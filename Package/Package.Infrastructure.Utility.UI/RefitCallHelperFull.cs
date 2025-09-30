using Refit;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Package.Infrastructure.Utility.UI;

/// <summary>
/// Full-featured variant:
/// - ActivitySource instrumentation (toggle via EnableActivities)
/// - onSuccess / onFailure callbacks
/// - Correlation Id header extraction (configurable names)
/// - Optional raw error content preview
/// - treatNotFoundAsNone support
/// - Metadata
/// </summary>
public static class RefitCallHelperFull
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private const string EmptyResponseTitle = "Unexpected empty response";
    private const int NoOpStatusCode = 460;
    private const int MaxRawPreview = 1024;

    public static bool EnableActivities { get; set; } = true;
    public static readonly ActivitySource ActivitySource = new("RefitCallHelper");

    //Client code can subscribe to this event to get notified of auth errors (401, 403)
    public static event Action<AuthErrorInfo>? OnAuthError;

    public static string[] CorrelationHeaderNames { get; set; } = ["X-Correlation-Id", "X-Correlation-ID"];

    public sealed record ApiCallMetadata(
        DateTimeOffset Started,
        DateTimeOffset Ended,
        TimeSpan Duration,
        bool TimedOut,
        string? OperationName,
        bool WasNoOp);

    public sealed record CallOptions(
        TimeSpan? Timeout = null,
        bool FailOnTimeout = false,
        string? OperationName = null,
        bool TreatNotFoundAsNone = false,
        bool CaptureRawError = false);

    public static bool IsNoOp(ProblemDetails? pd) => pd?.Status == NoOpStatusCode;

    // ------------ Public (typed) ------------
    public static Task<ApiResult<T>> TryApiCallIfAsync<T>(
        bool condition,
        Func<Task<T>> apiCall,
        string? noOpReason = null,
        CallOptions? options = null,
        Action<T>? onSuccess = null,
        Action<ProblemDetails>? onFailure = null,
        CancellationToken cancellationToken = default)
    {
        if (!condition)
        {
            var pd = new ProblemDetails
            {
                Status = NoOpStatusCode,
                Title = "No Operation",
                Detail = noOpReason ?? "Precondition failed; API call was skipped."
            };
            onFailure?.Invoke(pd);
            return Task.FromResult(ApiResult<T>.Failure(pd));
        }
        return TryApiCallAsync(apiCall, options, onSuccess, onFailure, cancellationToken);
    }

    public static Task<ApiResult<T>> TryApiCallAsync<T>(
        Func<Task<T>> apiCall,
        CallOptions? options = null,
        Action<T>? onSuccess = null,
        Action<ProblemDetails>? onFailure = null,
        CancellationToken cancellationToken = default)
        => CoreTypedAsync(_ => apiCall(), options ?? new(), onSuccess, onFailure, cancellationToken);

    public static Task<ApiResult<T>> TryApiCallAsync<T>(
        Func<CancellationToken, Task<T>> apiCall,
        CallOptions? options = null,
        Action<T>? onSuccess = null,
        Action<ProblemDetails>? onFailure = null,
        CancellationToken cancellationToken = default)
        => CoreTypedAsync(apiCall, options ?? new(), onSuccess, onFailure, cancellationToken);

    // ------------ Public (void) ------------
    public static Task<ApiResult> TryApiCallIfVoidAsync(
        bool condition,
        Func<Task> apiCall,
        string? noOpReason = null,
        CallOptions? options = null,
        Action? onSuccess = null,
        Action<ProblemDetails>? onFailure = null,
        CancellationToken cancellationToken = default)
    {
        if (!condition)
        {
            var pd = new ProblemDetails
            {
                Status = NoOpStatusCode,
                Title = "No Operation",
                Detail = noOpReason ?? "Precondition failed; API call was skipped."
            };
            onFailure?.Invoke(pd);
            return Task.FromResult(ApiResult.Failure(pd));
        }
        return TryApiCallVoidAsync(apiCall, options, onSuccess, onFailure, cancellationToken);
    }

    public static Task<ApiResult> TryApiCallVoidAsync(
        Func<Task> apiCall,
        CallOptions? options = null,
        Action? onSuccess = null,
        Action<ProblemDetails>? onFailure = null,
        CancellationToken cancellationToken = default)
        => CoreVoidAsync(_ => apiCall(), options ?? new(), onSuccess, onFailure, cancellationToken);

    public static Task<ApiResult> TryApiCallVoidAsync(
        Func<CancellationToken, Task> apiCall,
        CallOptions? options = null,
        Action? onSuccess = null,
        Action<ProblemDetails>? onFailure = null,
        CancellationToken cancellationToken = default)
        => CoreVoidAsync(apiCall, options ?? new(), onSuccess, onFailure, cancellationToken);

    // ------------ With metadata ------------
    public static async Task<(ApiResult<T> Result, ApiCallMetadata Meta)> TryApiCallWithMetaAsync<T>(
        Func<Task<T>> apiCall,
        CallOptions? options = null,
        Action<T>? onSuccess = null,
        Action<ProblemDetails>? onFailure = null,
        CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        var result = await TryApiCallAsync(apiCall, options, onSuccess, onFailure, cancellationToken);
        var ended = DateTimeOffset.UtcNow;
        return (result, BuildMeta(result.Problem, started, ended, options?.OperationName));
    }

    public static async Task<(ApiResult Result, ApiCallMetadata Meta)> TryApiCallVoidWithMetaAsync(
        Func<Task> apiCall,
        CallOptions? options = null,
        Action? onSuccess = null,
        Action<ProblemDetails>? onFailure = null,
        CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        var result = await TryApiCallVoidAsync(apiCall, options, onSuccess, onFailure, cancellationToken);
        var ended = DateTimeOffset.UtcNow;
        return (result, BuildMeta(result.Problem, started, ended, options?.OperationName));
    }

    // ------------ Core (typed) ------------
    private static async Task<ApiResult<T>> CoreTypedAsync<T>(
        Func<CancellationToken, Task<T>> apiCall,
        CallOptions options,
        Action<T>? onSuccess,
        Action<ProblemDetails>? onFailure,
        CancellationToken externalCancellation)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellation);
        cts.CancelAfter(options.Timeout ?? DefaultTimeout);

        var activity = StartActivity(options.OperationName);
        try
        {
            var data = await apiCall(cts.Token).ConfigureAwait(false);
            activity?.SetTag("success", true);
            onSuccess?.Invoke(data);
            return ApiResult<T>.Success(data);
        }
        catch (OperationCanceledException) when (!options.FailOnTimeout && !externalCancellation.IsCancellationRequested)
        {
            var timeoutResult = TimeoutProblem<T>(options.OperationName);
            onFailure?.Invoke(timeoutResult.Problem!);
            activity?.SetTag("timeout", true).SetTag("success", false);
            return timeoutResult;
        }
        catch (ApiException ex)
        {
            if (options.TreatNotFoundAsNone && ex.StatusCode == HttpStatusCode.NotFound)
            {
                activity?.SetTag("notFoundAsNone", true).SetTag("success", true);
                onSuccess?.Invoke(default!);
                return ApiResult<T>.Success(default!);
            }
            var pd = MapApiException(ex, options, captureRaw: options.CaptureRawError);
            onFailure?.Invoke(pd);
            activity?.SetTag("success", false).SetTag("http.status_code", pd.Status);
            return ApiResult<T>.Failure(pd);
        }
        catch (HttpRequestException httpEx)
        {
            var pd = NetworkProblem(AggregateMessage(httpEx), options.OperationName);
            onFailure?.Invoke(pd);
            activity?.SetTag("success", false).SetTag("transport", "http");
            return ApiResult<T>.Failure(pd);
        }
        catch (SocketException sockEx)
        {
            var pd = NetworkProblem(sockEx.Message, options.OperationName);
            onFailure?.Invoke(pd);
            activity?.SetTag("success", false).SetTag("transport", "socket");
            return ApiResult<T>.Failure(pd);
        }
        catch (Exception ex)
        {
            var pd = GenericProblem(ex.Message, options.OperationName);
            onFailure?.Invoke(pd);
            activity?.SetTag("success", false).SetTag("error.type", ex.GetType().FullName);
            return ApiResult<T>.Failure(pd);
        }
        finally
        {
            activity?.Dispose();
        }
    }

    // ------------ Core (void) ------------
    private static async Task<ApiResult> CoreVoidAsync(
        Func<CancellationToken, Task> apiCall,
        CallOptions options,
        Action? onSuccess,
        Action<ProblemDetails>? onFailure,
        CancellationToken externalCancellation)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellation);
        cts.CancelAfter(options.Timeout ?? DefaultTimeout);

        var activity = StartActivity(options.OperationName);
        try
        {
            await apiCall(cts.Token).ConfigureAwait(false);
            activity?.SetTag("success", true);
            onSuccess?.Invoke();
            return ApiResult.Success();
        }
        catch (OperationCanceledException) when (!options.FailOnTimeout && !externalCancellation.IsCancellationRequested)
        {
            var timeoutResult = TimeoutProblem(options.OperationName);
            onFailure?.Invoke(timeoutResult.Problem!);
            activity?.SetTag("timeout", true).SetTag("success", false);
            return timeoutResult;
        }
        catch (ApiException ex)
        {
            var pd = MapApiException(ex, options, captureRaw: options.CaptureRawError);
            onFailure?.Invoke(pd);
            activity?.SetTag("success", false).SetTag("http.status_code", pd.Status);
            return ApiResult.Failure(pd);
        }
        catch (HttpRequestException httpEx)
        {
            var pd = NetworkProblem(AggregateMessage(httpEx), options.OperationName);
            onFailure?.Invoke(pd);
            activity?.SetTag("success", false).SetTag("transport", "http");
            return ApiResult.Failure(pd);
        }
        catch (SocketException sockEx)
        {
            var pd = NetworkProblem(sockEx.Message, options.OperationName);
            onFailure?.Invoke(pd);
            activity?.SetTag("success", false).SetTag("transport", "socket");
            return ApiResult.Failure(pd);
        }
        catch (Exception ex)
        {
            var pd = GenericProblem(ex.Message, options.OperationName);
            onFailure?.Invoke(pd);
            activity?.SetTag("success", false).SetTag("error.type", ex.GetType().FullName);
            return ApiResult.Failure(pd);
        }
        finally
        {
            activity?.Dispose();
        }
    }

    // ------------ Mapping / utilities ------------
    private static Activity? StartActivity(string? op)
    {
        if (!EnableActivities) return null;
        return ActivitySource.StartActivity(op is null ? "refit.call" : $"refit.call:{op}", ActivityKind.Client);
    }

    private static ProblemDetails MapApiException(ApiException ex, CallOptions options, bool captureRaw)
    {
        var deserializedPD = DeserializeProblemDetails(ex.StatusCode, ex.Content);
        ProblemDetails pd;

        // Detect AADSTS50173 or invalid_grant
        if (IsAuthTokenRevoked(ex.Content))
        {
            OnAuthError?.Invoke(new AuthErrorInfo(
                Error: GetJsonField(ex.Content, "error") ?? "error_not_identified",
                ErrorDescription: GetJsonField(ex.Content, "error_description"),
                ErrorCode: GetJsonIntField(ex.Content, "error_codes"),
                SubError: GetJsonField(ex.Content, "suberror"),
                Problem: deserializedPD
            ));
        }

        if (deserializedPD.Title != EmptyResponseTitle)
        {
            pd = deserializedPD;
        }
        else
        {
            pd = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new ProblemDetails { Status = 401, Title = "Not Authorized", Detail = "You do not have permission to perform this action." },
                HttpStatusCode.Forbidden => new ProblemDetails { Status = 403, Title = "Forbidden", Detail = "You do not have permission to perform this action." },
                HttpStatusCode.NotFound => new ProblemDetails { Status = 404, Title = "Not Found", Detail = "Resource not found." },
                HttpStatusCode.MethodNotAllowed => new ProblemDetails { Status = 405, Title = "Method Not Allowed", Detail = "Method not allowed for this endpoint." },
                (HttpStatusCode)429 => new ProblemDetails { Status = 429, Title = "Too Many Requests", Detail = "Rate limit exceeded. Please retry later." },
                _ => new ProblemDetails { Status = (int)ex.StatusCode, Title = "API Error", Detail = $"Unexpected API error ({(int)ex.StatusCode})." }
            };
        }

        // Correlation header(s)
        try
        {
            if (ex.Headers is not null)
            {
                foreach (var headerName in CorrelationHeaderNames)
                {
                    if (ex.Headers.TryGetValues(headerName, out var vals))
                    {
                        var val = vals.FirstOrDefault();
                        if (!string.IsNullOrEmpty(val))
                        {
                            pd.Extensions ??= new Dictionary<string, object>();
                            pd.Extensions["correlationId"] = val;
                            break;
                        }
                    }
                }
            }
        }
        catch { /* swallow */ }

        if (captureRaw && !string.IsNullOrEmpty(ex.Content))
        {
            var preview = ex.Content.Length > MaxRawPreview
                ? ex.Content[..MaxRawPreview] + "...(truncated)"
                : ex.Content;
            pd.Extensions ??= new Dictionary<string, object>();
            if (!pd.Extensions.ContainsKey("raw"))
                pd.Extensions["raw"] = preview;
        }

        return AttachOperation(pd, options.OperationName);
    }

    private static ProblemDetails DeserializeProblemDetails(HttpStatusCode statusCode, string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return new ProblemDetails
            {
                Status = (int)statusCode,
                Title = EmptyResponseTitle,
                Detail = "Response was empty or null."
            };
        }

        try
        {
            var pd = JsonSerializer.Deserialize<ProblemDetails>(content, JsonOptions);
            if (pd is null)
            {
                return new ProblemDetails
                {
                    Status = (int)statusCode,
                    Title = "Unexpected error",
                    Detail = $"Failed to deserialize the error response. {content}"
                };
            }
            if (pd.Status == 0) pd.Status = (int)statusCode;
            return pd;
        }
        catch
        {
            return new ProblemDetails
            {
                Status = (int)statusCode,
                Title = "Response deserialization error",
                Detail = $"Failed to deserialize the error response: {content}"
            };
        }
    }

    private static string AggregateMessage(HttpRequestException ex) =>
        ex.InnerException is null ? ex.Message : $"{ex.Message} (Inner: {ex.InnerException.Message})";

    private static ApiResult<T> TimeoutProblem<T>(string? op) =>
        ApiResult<T>.Failure(AttachOperation(new ProblemDetails
        {
            Status = (int)HttpStatusCode.GatewayTimeout,
            Title = op is null ? "Request Timeout" : $"{op} Timeout",
            Detail = "The request took too long to complete."
        }, op));

    private static ApiResult TimeoutProblem(string? op) =>
        ApiResult.Failure(AttachOperation(new ProblemDetails
        {
            Status = (int)HttpStatusCode.GatewayTimeout,
            Title = op is null ? "Request Timeout" : $"{op} Timeout",
            Detail = "The request took too long to complete."
        }, op));

    private static ProblemDetails NetworkProblem(string msg, string? op) =>
        AttachOperation(new ProblemDetails
        {
            Status = 503,
            Title = op is null ? "API Unreachable" : $"{op} Unreachable",
            Detail = $"The API may be offline or unreachable. {msg}"
        }, op);

    private static ProblemDetails GenericProblem(string msg, string? op) =>
        AttachOperation(new ProblemDetails
        {
            Status = 500,
            Title = op is null ? "Unexpected Error" : $"{op} Failed",
            Detail = msg
        }, op);

    private static ProblemDetails AttachOperation(ProblemDetails pd, string? op)
    {
        if (op is null) return pd;
        pd.Extensions ??= new Dictionary<string, object>();
        if (!pd.Extensions.ContainsKey("operation"))
            pd.Extensions["operation"] = op;
        return pd;
    }

    private static ApiCallMetadata BuildMeta(ProblemDetails? problem, DateTimeOffset started, DateTimeOffset ended, string? op) =>
        new(started, ended, ended - started,
            TimedOut: problem?.Status == (int)HttpStatusCode.GatewayTimeout,
            OperationName: op,
            WasNoOp: IsNoOp(problem));

    private static bool IsAuthTokenRevoked(string? content)
    {
        // Check for AADSTS50173, invalid_grant, or suberror: bad_token
        if (content is null) return false;
        return content.Contains("AADSTS50173") ||
               content.Contains("\"error\":\"invalid_grant\"") ||
               content.Contains("\"suberror\":\"bad_token\"") ||
               content.Contains("\"error_codes\":[50173]");
    }

    // Simple JSON field extractors (for error info)
    private static string? GetJsonField(string? json, string field)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(field, out var el))
                return el.GetString();
        }
        catch
        {
            // handle
        }
        return null;
    }

    private static int? GetJsonIntField(string? json, string field)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.Array && el.GetArrayLength() > 0)
                return el[0].GetInt32();
        }
        catch
        {
            //handle
        }
        return null;
    }
}