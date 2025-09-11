using Refit;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Package.Infrastructure.Utility.UI;

public static class RefitCallHelper
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private const string EmptyResponseTitle = "Unexpected empty response";

    public sealed record ApiCallMetadata(
        DateTimeOffset Started,
        DateTimeOffset Ended,
        TimeSpan Duration,
        bool TimedOut,
        string? OperationName,
        bool WasNoOp);

    private const int NoOpStatusCode = 460;
    public static bool IsNoOp(ProblemDetails? pd) => pd?.Status == NoOpStatusCode;

    public static Task<ApiResult<T>> TryApiCallIfAsync<T>(
        bool condition,
        Func<Task<T>> apiCall,
        string? noOpReason = null,
        TimeSpan? timeout = null,
        string? operationName = null,
        bool treatNotFoundAsNone = false,
        CancellationToken cancellationToken = default)
    {
        if (!condition)
        {
            return Task.FromResult(ApiResult<T>.Failure(new ProblemDetails
            {
                Status = NoOpStatusCode,
                Title = "No Operation",
                Detail = noOpReason ?? "Precondition failed; API call was skipped."
            }));
        }
        return TryApiCallAsync(apiCall, failOnTimeout: false, timeout, operationName, treatNotFoundAsNone, cancellationToken);
    }

    public static Task<ApiResult> TryApiCallIfVoidAsync(
        bool condition,
        Func<Task> apiCall,
        string? noOpReason = null,
        TimeSpan? timeout = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        if (!condition)
        {
            return Task.FromResult(ApiResult.Failure(new ProblemDetails
            {
                Status = NoOpStatusCode,
                Title = "No Operation",
                Detail = noOpReason ?? "Precondition failed; API call was skipped."
            }));
        }
        return TryApiCallVoidAsync(apiCall, failOnTimeout: false, timeout, operationName, cancellationToken);
    }

    public static Task<ApiResult<T>> TryApiCallAsync<T>(
        Func<Task<T>> apiCall,
        bool failOnTimeout = false,
        CancellationToken cancellationToken = default)
        => TryApiCallAsync(apiCall, failOnTimeout, timeout: null, operationName: null, treatNotFoundAsNone: false, cancellationToken);

    public static Task<ApiResult> TryApiCallVoidAsync(
        Func<Task> apiCall,
        bool failOnTimeout = false,
        CancellationToken cancellationToken = default)
        => TryApiCallVoidAsync(apiCall, failOnTimeout, timeout: null, operationName: null, cancellationToken);

    public static Task<ApiResult<T>> TryApiCallAsync<T>(
        Func<CancellationToken, Task<T>> apiCall,
        bool failOnTimeout = false,
        TimeSpan? timeout = null,
        string? operationName = null,
        bool treatNotFoundAsNone = false,
        CancellationToken cancellationToken = default)
        => CoreTypedAsync(ct => apiCall(ct), failOnTimeout, timeout, operationName, treatNotFoundAsNone, cancellationToken);

    public static Task<ApiResult> TryApiCallVoidAsync(
        Func<CancellationToken, Task> apiCall,
        bool failOnTimeout = false,
        TimeSpan? timeout = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
        => CoreVoidAsync(ct => apiCall(ct), failOnTimeout, timeout, operationName, cancellationToken);

    public static async Task<(ApiResult<T> Result, ApiCallMetadata Meta)> TryApiCallWithMetaAsync<T>(
        Func<Task<T>> apiCall,
        bool failOnTimeout = false,
        TimeSpan? timeout = null,
        string? operationName = null,
        bool treatNotFoundAsNone = false,
        CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        var result = await TryApiCallAsync(apiCall, failOnTimeout, timeout, operationName, treatNotFoundAsNone, cancellationToken).ConfigureAwait(false);
        var ended = DateTimeOffset.UtcNow;
        return (result, BuildMeta(result.Problem, started, ended, operationName));
    }

    public static async Task<(ApiResult Result, ApiCallMetadata Meta)> TryApiCallVoidWithMetaAsync(
        Func<Task> apiCall,
        bool failOnTimeout = false,
        TimeSpan? timeout = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        var result = await TryApiCallVoidAsync(apiCall, failOnTimeout, timeout, operationName, cancellationToken).ConfigureAwait(false);
        var ended = DateTimeOffset.UtcNow;
        return (result, BuildMeta(result.Problem, started, ended, operationName));
    }

    private static ApiCallMetadata BuildMeta(ProblemDetails? problem, DateTimeOffset started, DateTimeOffset ended, string? op) =>
        new(started, ended, ended - started,
            TimedOut: problem?.Status == (int)HttpStatusCode.GatewayTimeout,
            OperationName: op,
            WasNoOp: IsNoOp(problem));

    private static Task<ApiResult<T>> TryApiCallAsync<T>(
        Func<Task<T>> apiCall,
        bool failOnTimeout,
        TimeSpan? timeout,
        string? operationName,
        bool treatNotFoundAsNone,
        CancellationToken cancellationToken)
        => CoreTypedAsync(_ => apiCall(), failOnTimeout, timeout, operationName, treatNotFoundAsNone, cancellationToken);

    private static Task<ApiResult> TryApiCallVoidAsync(
        Func<Task> apiCall,
        bool failOnTimeout,
        TimeSpan? timeout,
        string? operationName,
        CancellationToken cancellationToken)
        => CoreVoidAsync(_ => apiCall(), failOnTimeout, timeout, operationName, cancellationToken);

    private static async Task<ApiResult<T>> CoreTypedAsync<T>(
        Func<CancellationToken, Task<T>> apiCall,
        bool failOnTimeout,
        TimeSpan? timeout,
        string? operationName,
        bool treatNotFoundAsNone,
        CancellationToken externalCancellation)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellation);
        cts.CancelAfter(timeout ?? DefaultTimeout);

        try
        {
            var data = await apiCall(cts.Token).ConfigureAwait(false);
            return ApiResult<T>.Success(data);
        }
        catch (OperationCanceledException) when (!failOnTimeout && !externalCancellation.IsCancellationRequested)
        {
            return TimeoutProblem<T>(operationName);
        }
        catch (ApiException ex)
        {
            if (treatNotFoundAsNone && ex.StatusCode == HttpStatusCode.NotFound)
            {
                return ApiResult<T>.Success(default!);
            }
            return ApiResult<T>.Failure(MapApiException(ex, operationName));
        }
        catch (HttpRequestException httpEx)
        {
            return ApiResult<T>.Failure(NetworkProblem(HttpAggregateMessage(httpEx), operationName));
        }
        catch (SocketException sockEx)
        {
            return ApiResult<T>.Failure(NetworkProblem(sockEx.Message, operationName));
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure(GenericProblem(ex.Message, operationName));
        }
    }

    private static async Task<ApiResult> CoreVoidAsync(
        Func<CancellationToken, Task> apiCall,
        bool failOnTimeout,
        TimeSpan? timeout,
        string? operationName,
        CancellationToken externalCancellation)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellation);
        cts.CancelAfter(timeout ?? DefaultTimeout);

        try
        {
            await apiCall(cts.Token).ConfigureAwait(false);
            return ApiResult.Success();
        }
        catch (OperationCanceledException) when (!failOnTimeout && !externalCancellation.IsCancellationRequested)
        {
            return TimeoutProblem(operationName);
        }
        catch (ApiException ex)
        {
            return ApiResult.Failure(MapApiException(ex, operationName));
        }
        catch (HttpRequestException httpEx)
        {
            return ApiResult.Failure(NetworkProblem(HttpAggregateMessage(httpEx), operationName));
        }
        catch (SocketException sockEx)
        {
            return ApiResult.Failure(NetworkProblem(sockEx.Message, operationName));
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(GenericProblem(ex.Message, operationName));
        }
    }

    private static string HttpAggregateMessage(HttpRequestException ex)
        => ex.InnerException is null ? ex.Message : $"{ex.Message} (Inner: {ex.InnerException.Message})";

    private static ProblemDetails AttachOperation(ProblemDetails pd, string? op)
    {
        if (op is null) return pd;
        pd.Extensions ??= new Dictionary<string, object>();
        if (!pd.Extensions.ContainsKey("operation"))
            pd.Extensions["operation"] = op;
        return pd;
    }

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

    private static ProblemDetails MapApiException(ApiException ex, string? operationName)
    {
        var deserialized = DeserializeProblemDetails(ex.StatusCode, ex.Content);
        if (deserialized.Title != EmptyResponseTitle)
        {
            return AttachOperation(deserialized, operationName);
        }

        var pd = ex.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Not Authorized",
                Detail = "You do not have permission to perform this action."
            },
            HttpStatusCode.Forbidden => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Forbidden",
                Detail = "You do not have permission to perform this action."
            },
            HttpStatusCode.NotFound => new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Not Found",
                Detail = "Resource not found."
            },
            HttpStatusCode.MethodNotAllowed => new ProblemDetails
            {
                Status = (int)HttpStatusCode.MethodNotAllowed,
                Title = "Method Not Allowed",
                Detail = "Method not allowed for this endpoint."
            },
            (HttpStatusCode)429 => new ProblemDetails
            {
                Status = 429,
                Title = "Too Many Requests",
                Detail = "Rate limit exceeded. Please retry later."
            },
            _ => new ProblemDetails
            {
                Status = (int)ex.StatusCode,
                Title = "API Error",
                Detail = $"Unexpected API error ({(int)ex.StatusCode})."
            }
        };

        try
        {
            string? correlationId = null;
            if (ex.Headers is not null)
            {
                if (ex.Headers.TryGetValues("X-Correlation-Id", out var corrValues))
                    correlationId = corrValues.FirstOrDefault();
                else if (ex.Headers.TryGetValues("X-Correlation-ID", out var corrValuesAlt))
                    correlationId = corrValuesAlt.FirstOrDefault();
            }

            if (correlationId is not null)
            {
                pd.Extensions ??= new Dictionary<string, object>();
                pd.Extensions["correlationId"] = correlationId;
            }
        }
        catch
        {
            // Ignore header parsing issues
        }

        return AttachOperation(pd, operationName);
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
            var pd = JsonSerializer.Deserialize<ProblemDetails>(content, DefaultJsonSerializerOptions);
            if (pd is null)
            {
                return new ProblemDetails
                {
                    Status = (int)statusCode,
                    Title = "Unexpected error",
                    Detail = $"Failed to deserialize the error response. {content}"
                };
            }

            if (pd.Status == 0)
            {
                pd.Status = (int)statusCode;
            }

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
}