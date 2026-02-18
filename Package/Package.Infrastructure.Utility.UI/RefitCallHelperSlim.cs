using Refit;
using System.Net;
using System.Net.Sockets;

namespace Package.Infrastructure.Utility.UI;

/// <summary>
/// Slim variant intended for Blazor WebAssembly:
/// - Minimal dependencies
/// - No telemetry / correlation headers
/// - Optional treatNotFoundAsNone
/// - Metadata support
/// </summary>
public static class RefitCallHelperSlim
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private const string EmptyResponseTitle = "Unexpected empty response";
    private const int NoOpStatusCode = 460;

    //Client code can subscribe to this event to get notified of auth errors (401, 403)
    public static event Action<AuthErrorInfo>? OnAuthError;

    public sealed record ApiCallMetadata(
        DateTimeOffset Started,
        DateTimeOffset Ended,
        TimeSpan Duration,
        bool TimedOut,
        string? OperationName,
        bool WasNoOp);

    public static bool IsNoOp(ProblemDetails? pd) => pd?.Status == NoOpStatusCode;

    // -------- Public (typed) --------
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

    public static Task<ApiResult<T>> TryApiCallAsync<T>(
        Func<Task<T>> apiCall,
        bool failOnTimeout = false,
        TimeSpan? timeout = null,
        string? operationName = null,
        bool treatNotFoundAsNone = false,
        CancellationToken cancellationToken = default)
        => CoreTypedAsync(_ => apiCall(), failOnTimeout, timeout, operationName, treatNotFoundAsNone, cancellationToken);

    public static Task<ApiResult<T>> TryApiCallAsync<T>(
        Func<CancellationToken, Task<T>> apiCall,
        bool failOnTimeout = false,
        TimeSpan? timeout = null,
        string? operationName = null,
        bool treatNotFoundAsNone = false,
        CancellationToken cancellationToken = default)
        => CoreTypedAsync(apiCall, failOnTimeout, timeout, operationName, treatNotFoundAsNone, cancellationToken);

    // -------- Public (void) --------
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

    public static Task<ApiResult> TryApiCallVoidAsync(
        Func<Task> apiCall,
        bool failOnTimeout = false,
        TimeSpan? timeout = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
        => CoreVoidAsync(_ => apiCall(), failOnTimeout, timeout, operationName, cancellationToken);

    public static Task<ApiResult> TryApiCallVoidAsync(
        Func<CancellationToken, Task> apiCall,
        bool failOnTimeout = false,
        TimeSpan? timeout = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
        => CoreVoidAsync(apiCall, failOnTimeout, timeout, operationName, cancellationToken);

    // -------- Metadata variants --------
    public static async Task<(ApiResult<T> Result, ApiCallMetadata Meta)> TryApiCallWithMetaAsync<T>(
        Func<Task<T>> apiCall,
        bool failOnTimeout = false,
        TimeSpan? timeout = null,
        string? operationName = null,
        bool treatNotFoundAsNone = false,
        CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        var result = await TryApiCallAsync(apiCall, failOnTimeout, timeout, operationName, treatNotFoundAsNone, cancellationToken);
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
        var result = await TryApiCallVoidAsync(apiCall, failOnTimeout, timeout, operationName, cancellationToken);
        var ended = DateTimeOffset.UtcNow;
        return (result, BuildMeta(result.Problem, started, ended, operationName));
    }

    // -------- Core (typed) --------
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
                return ApiResult<T>.Success(default!);
            return ApiResult<T>.Failure(MapApiException(ex, operationName));
        }
        catch (HttpRequestException httpEx)
        {
            return ApiResult<T>.Failure(NetworkProblem(AggregateMessage(httpEx), operationName));
        }
        catch (SocketException sockEx)
        {
            return ApiResult<T>.Failure(NetworkProblem(sockEx.Message, operationName));
        }
        catch (Exception ex) when (IsWasmStreamingError(ex))
        {
            return ApiResult<T>.Failure(CreateWasmStreamingErrorProblem(operationName, ex));
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure(GenericProblem(ex.Message, operationName));
        }
    }

    // ADD THIS: Same exception handling for void calls
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
            return ApiResult.Failure(NetworkProblem(AggregateMessage(httpEx), operationName));
        }
        catch (SocketException sockEx)
        {
            return ApiResult.Failure(NetworkProblem(sockEx.Message, operationName));
        }
        catch (Exception ex) when (IsWasmStreamingError(ex))
        {
            return ApiResult.Failure(CreateWasmStreamingErrorProblem(operationName, ex));
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(GenericProblem(ex.Message, operationName));
        }
    }

    // Helper to check if exception is related to .NET 10 WASM streaming issue
    private static bool IsWasmStreamingError(Exception ex) => RefitCallHelperShared.IsWasmStreamingError(ex);

    // Helper to create WASM streaming error ProblemDetails
    private static ProblemDetails CreateWasmStreamingErrorProblem(string? operationName, Exception? ex = null) =>
        RefitCallHelperShared.CreateWasmStreamingErrorProblem(operationName, ex);

    // -------- Mapping / helpers --------
    private static ProblemDetails MapApiException(ApiException ex, string? op)
    {
        return RefitCallHelperShared.MapApiException(
            ex,
            op,
            EmptyResponseTitle,
            info => OnAuthError?.Invoke(info),
            correlationHeaderNames: null,
            captureRawError: false,
            includeValidationErrorShaping: true);
    }

    private static ProblemDetails DeserializeProblemDetails(HttpStatusCode statusCode, string? content)
    {
        return RefitCallHelperShared.DeserializeProblemDetails(statusCode, content, EmptyResponseTitle, includeValidationErrorShaping: true);
    }

    private static ApiCallMetadata BuildMeta(ProblemDetails? problem, DateTimeOffset started, DateTimeOffset ended, string? op) =>
        new(started, ended, ended - started,
            TimedOut: problem?.Status == (int)HttpStatusCode.GatewayTimeout,
            OperationName: op,
            WasNoOp: IsNoOp(problem));

    private static string AggregateMessage(HttpRequestException ex) => RefitCallHelperShared.AggregateMessage(ex);

    private static ApiResult<T> TimeoutProblem<T>(string? op) =>
        ApiResult<T>.Failure(RefitCallHelperShared.CreateTimeoutProblem(op));

    private static ApiResult TimeoutProblem(string? op) =>
        ApiResult.Failure(RefitCallHelperShared.CreateTimeoutProblem(op));

    private static ProblemDetails NetworkProblem(string msg, string? op) => RefitCallHelperShared.CreateNetworkProblem(msg, op);

    private static ProblemDetails GenericProblem(string msg, string? op) => RefitCallHelperShared.CreateGenericProblem(msg, op);
}