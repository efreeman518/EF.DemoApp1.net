using Refit;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

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
        catch (Exception ex) when (IsWasmStreamingError(ex))
        {
            var pd = CreateWasmStreamingErrorProblem(options.OperationName, ex);
            onFailure?.Invoke(pd);
            activity?.SetTag("success", false).SetTag("error.type", "wasm.streaming");
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
        catch (Exception ex) when (IsWasmStreamingError(ex))
        {
            var pd = CreateWasmStreamingErrorProblem(options.OperationName, ex);
            onFailure?.Invoke(pd);
            activity?.SetTag("success", false).SetTag("error.type", "wasm.streaming");
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
        return RefitCallHelperShared.MapApiException(
            ex,
            options.OperationName,
            EmptyResponseTitle,
            info => OnAuthError?.Invoke(info),
            CorrelationHeaderNames,
            captureRaw,
            MaxRawPreview,
            includeValidationErrorShaping: true);
    }

    private static ProblemDetails DeserializeProblemDetails(HttpStatusCode statusCode, string? content)
    {
        return RefitCallHelperShared.DeserializeProblemDetails(statusCode, content, EmptyResponseTitle, includeValidationErrorShaping: true);
    }

    private static string AggregateMessage(HttpRequestException ex) => RefitCallHelperShared.AggregateMessage(ex);

    private static ApiResult<T> TimeoutProblem<T>(string? op) =>
        ApiResult<T>.Failure(RefitCallHelperShared.CreateTimeoutProblem(op));

    private static ApiResult TimeoutProblem(string? op) =>
        ApiResult.Failure(RefitCallHelperShared.CreateTimeoutProblem(op));

    private static ProblemDetails NetworkProblem(string msg, string? op) => RefitCallHelperShared.CreateNetworkProblem(msg, op);

    private static ProblemDetails GenericProblem(string msg, string? op) => RefitCallHelperShared.CreateGenericProblem(msg, op);

    private static ApiCallMetadata BuildMeta(ProblemDetails? problem, DateTimeOffset started, DateTimeOffset ended, string? op) =>
        new(started, ended, ended - started,
            TimedOut: problem?.Status == (int)HttpStatusCode.GatewayTimeout,
            OperationName: op,
            WasNoOp: IsNoOp(problem));

    // Helper to check if exception is related to .NET 10 WASM streaming issue
    private static bool IsWasmStreamingError(Exception ex) => RefitCallHelperShared.IsWasmStreamingError(ex);

    // Helper to create WASM streaming error ProblemDetails
    private static ProblemDetails CreateWasmStreamingErrorProblem(string? operationName, Exception? ex = null) =>
        RefitCallHelperShared.CreateWasmStreamingErrorProblem(operationName, ex);
}