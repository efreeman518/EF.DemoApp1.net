//using Polly;
//using Polly.CircuitBreaker;
//using Polly.Contrib.WaitAndRetry;
//using Polly.Extensions.Http;
//using Polly.Retry;
//using System.Net;

//namespace Package.Infrastructure.Common;

////https://stackoverflow.com/questions/73037947/can-we-use-polly-retry-instead-of-exponentialbackoffretry-in-service-bus-topic-t

//public class RetrySettings
//{
//    public bool IncludeDefaultTransientHttpErrors { get; set; }
//    public int MaxAttempts { get; set; } = 5;
//    public double MedianFirstRetryDelaySeconds { get; set; } = 2;
//}

//public class CircuitBreakerSettings
//{
//    //circuit breaker
//    public double FailureThreshold { get; set; } = 0.5;
//    public int SamplingDurationSeconds { get; set; } = 10;
//    public int MinThroughput { get; set; } = 20;
//    public int BreakDurationSeconds { get; set; } = 30;
//}

//public static class PollyRetry
//{
//    /// <summary>
//    /// Typical Http Retry - Network Failures, 5XX, 408 (known transient errors)
//    /// </summary>
//    /// <param name="settings"></param>
//    /// <param name="httpStatusCodes">other status codes to consider - 404, etc.</param>
//    /// <returns></returns>
//    public static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy(RetrySettings? settings = null, List<HttpStatusCode>? httpStatusCodes = null)
//    {
//        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(settings?.MedianFirstRetryDelaySeconds ?? 1), retryCount: settings?.MaxAttempts ?? 5);
//        var policyBuilder = HttpPolicyExtensions.HandleTransientHttpError(); //known transient errors
//        if (httpStatusCodes != null)
//            policyBuilder.OrResult(msg => httpStatusCodes.Contains(msg.StatusCode));
//        return policyBuilder.WaitAndRetryAsync(delay);
//    }

//    /// <summary>
//    /// Typical Http Circuit Breaker - Network Failures, 5XX, 408 (known transient errors)
//    /// </summary>
//    /// <param name="settings"></param>
//    /// <param name="httpStatusCodes">other status codes to consider - 404, etc.</param></param>
//    /// <returns></returns>
//    public static IAsyncPolicy<HttpResponseMessage> GetHttpCircuitBreakerPolicy(CircuitBreakerSettings? settings = null, List<HttpStatusCode>? httpStatusCodes = null)
//    {
//        var policyBuilder = HttpPolicyExtensions.HandleTransientHttpError(); //known transient errors
//        if (httpStatusCodes != null)
//            policyBuilder.OrResult(msg => httpStatusCodes.Contains(msg.StatusCode));
//        return policyBuilder.AdvancedCircuitBreakerAsync(settings?.FailureThreshold ?? .5,
//                TimeSpan.FromSeconds(settings?.SamplingDurationSeconds ?? 10),
//                settings?.MinThroughput ?? 20,
//                TimeSpan.FromSeconds(settings?.BreakDurationSeconds ?? 30));
//    }

//    /// <summary>
//    /// Generic Retry, return T
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    /// <param name="settings"></param>
//    /// <param name="retryExceptions"></param>
//    /// <param name="includeInner"></param>
//    /// <param name="exceptionCallback"></param>
//    /// <param name="returnCallback"></param>
//    /// <returns></returns>
//    public static IAsyncPolicy<T> GetWaitAndRetryAsyncPolicy<T>(RetrySettings settings, ICollection<Type>? retryExceptions = null,
//        bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null)
//    {
//        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(settings.MedianFirstRetryDelaySeconds), retryCount: settings.MaxAttempts);
//        return GetPolicyBuilder(retryExceptions, includeInner, exceptionCallback, returnCallback)
//            .WaitAndRetryAsync(delay);
//    }

//    /// <summary>
//    /// Generic circuit breaker, return T
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    /// <param name="settings"></param>
//    /// <param name="retryExceptions"></param>
//    /// <param name="includeInner"></param>
//    /// <param name="exceptionCallback"></param>
//    /// <param name="returnCallback"></param>
//    /// <returns></returns>
//    public static IAsyncPolicy<T> GetCiruitBreakerAsyncPolicy<T>(CircuitBreakerSettings settings, ICollection<Type>? retryExceptions = null,
//        bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null) =>
//        GetPolicyBuilder(retryExceptions, includeInner, exceptionCallback, returnCallback)
//        .AdvancedCircuitBreakerAsync(settings.FailureThreshold,
//                TimeSpan.FromSeconds(settings.SamplingDurationSeconds),
//                settings.MinThroughput,
//                TimeSpan.FromSeconds(settings.BreakDurationSeconds));

//    /// <summary>
//    /// Wrap the retry here and return HttpResponseMessage
//    /// </summary>
//    /// <param name="factory"></param>
//    /// <param name="retrySettings"></param>
//    /// <param name="circuitBreakerSettings"></param>
//    /// <param name="retryHttpStatusCodes"></param>
//    /// <returns></returns>
//    public async static Task<HttpResponseMessage> RetryHttpAsync(Func<Task<HttpResponseMessage>> factory,
//        RetrySettings? retrySettings = null, CircuitBreakerSettings? circuitBreakerSettings = null,
//        HttpStatusCode[]? retryHttpStatusCodes = null)
//    {
//        retryHttpStatusCodes ??= [
//                HttpStatusCode.RequestTimeout, // 408
//                                               //HttpStatusCode.InternalServerError, // 500 //subsequent retry will violate replay attack check and throw unauthorized
//            HttpStatusCode.BadGateway, // 502
//            HttpStatusCode.ServiceUnavailable, // 503
//            HttpStatusCode.GatewayTimeout // 504
//            ];
//        bool returnCallback(HttpResponseMessage r) => retryHttpStatusCodes.Contains(r.StatusCode);

//        List<Type> retryExceptions = [typeof(HttpRequestException)];
//        return await RetryAsync(factory, retrySettings ?? new RetrySettings(), circuitBreakerSettings ?? new CircuitBreakerSettings(),
//            retryExceptions, false, null, returnCallback).ConfigureAwait(ConfigureAwaitOptions.None);
//    }

//    /// <summary>
//    /// Wrap the retry and return nothing
//    /// </summary>
//    /// <param name="factory"></param>
//    /// <param name="retrySettings"></param>
//    /// <param name="circuitBreakerSettings"></param>
//    /// <param name="retryHttpStatusCodes"></param>
//    /// <returns></returns>
//    public async static Task RetryHttpNoReturnAsync(Func<Task<HttpResponseMessage>> factory,
//        RetrySettings? retrySettings = null, CircuitBreakerSettings? circuitBreakerSettings = null,
//        HttpStatusCode[]? retryHttpStatusCodes = null)
//    {
//        retryHttpStatusCodes ??= [
//                HttpStatusCode.RequestTimeout, // 408
//                                               //HttpStatusCode.InternalServerError, // 500 //subsequent retry will violate replay attack check and throw unauthorized
//            HttpStatusCode.BadGateway, // 502
//            HttpStatusCode.ServiceUnavailable, // 503
//            HttpStatusCode.GatewayTimeout // 504
//            ];
//        bool returnCallback(HttpResponseMessage r) => retryHttpStatusCodes.Contains(r.StatusCode);

//        List<Type> retryExceptions = [typeof(HttpRequestException)];
//        await RetryAsync(factory, retrySettings ?? new RetrySettings(), circuitBreakerSettings ?? new CircuitBreakerSettings(),
//            retryExceptions, false, null, returnCallback).ConfigureAwait(ConfigureAwaitOptions.None);
//    }

//    /// <summary>
//    /// Wrap the retry and return T
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    /// <param name="factory"></param>
//    /// <param name="retrySettings"></param>
//    /// <param name="circuitBreakerSettings"></param>
//    /// <param name="retryExceptions"></param>
//    /// <param name="includeInner"></param>
//    /// <param name="exceptionCallback"></param>
//    /// <param name="returnCallback"></param>
//    /// <returns></returns>
//    public static async Task<T> RetryAsync<T>(Func<Task<T>> factory, RetrySettings retrySettings, CircuitBreakerSettings circuitBreakerSettings, List<Type>? retryExceptions = null, bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null)
//    {
//        var retry = WaitAndRetryAsyncPolicy(retrySettings, retryExceptions, includeInner, exceptionCallback, returnCallback);
//        var circuitBreaker = CiruitBreakerAsyncPolicy<T>(circuitBreakerSettings, retryExceptions, includeInner, exceptionCallback, returnCallback);
//        var result = await Policy.WrapAsync(retry, circuitBreaker).ExecuteAndCaptureAsync(factory).ConfigureAwait(ConfigureAwaitOptions.None);
//        return result.Result;
//    }

//    public static AsyncRetryPolicy<T> WaitAndRetryAsyncPolicy<T>(RetrySettings settings, ICollection<Type>? retryExceptions = null, bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null)
//    {
//        PolicyBuilder<T> policyBuilder = GetPolicyBuilder(retryExceptions, includeInner, exceptionCallback, returnCallback);
//        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(settings.MedianFirstRetryDelaySeconds), retryCount: settings.MaxAttempts);
//        return policyBuilder.WaitAndRetryAsync(delay);
//    }


//    public static AsyncCircuitBreakerPolicy<T> CiruitBreakerAsyncPolicy<T>(CircuitBreakerSettings settings, ICollection<Type>? retryExceptions = null, bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null)
//    {
//        PolicyBuilder<T> policyBuilder = GetPolicyBuilder(retryExceptions, includeInner, exceptionCallback, returnCallback);
//        return policyBuilder.AdvancedCircuitBreakerAsync(settings.FailureThreshold,
//                TimeSpan.FromSeconds(settings.SamplingDurationSeconds),
//                settings.MinThroughput,
//                TimeSpan.FromSeconds(settings.BreakDurationSeconds));
//    }


//    /// <summary>
//    /// Builds the retry policy
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    /// <param name="retryExceptions">Collection of retryable exceptions</param>
//    /// <param name="includeInner">Exceptions may be at any inner level</param>
//    /// <param name="exceptionCallback">Callback method to investigate the exception to determine retryability (SqlException.ErrorCode)</param>
//    /// <param name="returnCallback">Callback method to investigate the factory method return value to determine retryability (HttpResponseMessage.StatusCode)</param>
//    /// <returns></returns>
//    private static PolicyBuilder<T> GetPolicyBuilder<T>(ICollection<Type>? retryExceptions = null, bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null)
//    {
//        if (retryExceptions == null && returnCallback == null) throw new InvalidOperationException("GetPolicyBuilder must have defined retryExceptions and/or returnCallback.");

//        PolicyBuilder<T> policyBuilder = null!; // = HttpPolicyExtensions.HandleTransientHttpError();
//        if (retryExceptions != null)
//        {
//            policyBuilder = includeInner
//                ? Policy<T>.HandleInner<Exception>(ex => retryExceptions.Contains(ex.GetType()) && (exceptionCallback == null || exceptionCallback(ex)))
//                : Policy<T>.Handle<Exception>(ex => retryExceptions.Contains(ex.GetType()) && (exceptionCallback == null || exceptionCallback(ex)));

//            if (returnCallback != null)
//                policyBuilder = policyBuilder.OrResult(r => returnCallback(r)); //T could be HttpResponseMessage which retryCallback takes to check StatusCode property for retryable values
//        }
//        else if (returnCallback != null)
//            policyBuilder = Policy<T>.HandleResult(r => returnCallback(r)); //T could be HttpResponseMessage which retryCallback takes to check StatusCode property for retryable values

//        return policyBuilder;
//    }
//}
