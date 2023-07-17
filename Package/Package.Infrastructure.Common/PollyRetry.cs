using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Package.Infrastructure.Common;

//https://stackoverflow.com/questions/73037947/can-we-use-polly-retry-instead-of-exponentialbackoffretry-in-service-bus-topic-t

public class RetrySettings
{
    public bool IncludeDefaultTransientHttpErrors { get; set; }
    public int RetryMaxAttempts { get; set; } = 5;
    public double MedianFirstRetryDelaySeconds { get; set; } = 2;
}

public class CircuitBreakerSettings
{
    //circuit breaker
    public double CircuitBreakerFailureThreshold { get; set; } = 0.5;
    public int CircuitBreakerSamplingDurationSeconds { get; set; } = 10;
    public int CircuitBreakerMinThroughput { get; set; } = 20;
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;
}

public static class PollyRetry
{
    //http - return T
    public async static Task<HttpResponseMessage> RetryHttpAsync(Func<Task<HttpResponseMessage>> factory,
        RetrySettings? retrySettings = null, CircuitBreakerSettings? circuitBreakerSettings = null,
        HttpStatusCode[]? retryHttpStatusCodes = null)
    {
        retryHttpStatusCodes ??= new HttpStatusCode[] {
                HttpStatusCode.RequestTimeout, // 408
                //HttpStatusCode.InternalServerError, // 500 //subsequent retry will violate replay attack check and throw unauthorized
                HttpStatusCode.BadGateway, // 502
                HttpStatusCode.ServiceUnavailable, // 503
                HttpStatusCode.GatewayTimeout // 504
            };
        bool returnCallback(HttpResponseMessage r) => retryHttpStatusCodes.Contains(r.StatusCode);

        List<Type> retryExceptions = new() { typeof(HttpRequestException) };
        return await RetryAsync(factory, retrySettings ?? new RetrySettings(), circuitBreakerSettings ?? new CircuitBreakerSettings(), retryExceptions, false, null, returnCallback);
    }

    //http - no return
    public async static Task RetryHttpNoReturnAsync(Func<Task<HttpResponseMessage>> factory,
        RetrySettings? retrySettings = null, CircuitBreakerSettings? circuitBreakerSettings = null,
        HttpStatusCode[]? retryHttpStatusCodes = null)
    {
        retryHttpStatusCodes ??= new HttpStatusCode[] {
                HttpStatusCode.RequestTimeout, // 408
                //HttpStatusCode.InternalServerError, // 500 //subsequent retry will violate replay attack check and throw unauthorized
                HttpStatusCode.BadGateway, // 502
                HttpStatusCode.ServiceUnavailable, // 503
                HttpStatusCode.GatewayTimeout // 504
            };
        bool returnCallback(HttpResponseMessage r) => retryHttpStatusCodes.Contains(r.StatusCode);

        List<Type> retryExceptions = new() { typeof(HttpRequestException) };
        await RetryAsync(factory, retrySettings ?? new RetrySettings(), circuitBreakerSettings ?? new CircuitBreakerSettings(), retryExceptions, false, null, returnCallback);
    }

    public static async Task<T> RetryAsync<T>(Func<Task<T>> factory, RetrySettings retrySettings, CircuitBreakerSettings circuitBreakerSettings, List<Type>? retryExceptions = null, bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null)
    {
        var retry = WaitAndRetryAsyncPolicy(retrySettings, retryExceptions, includeInner, exceptionCallback, returnCallback);
        var circuitBreaker = CiruitBreakerAsyncPolicy<T>(circuitBreakerSettings, retryExceptions, includeInner, exceptionCallback, returnCallback);
        var result = await Policy.WrapAsync(retry, circuitBreaker).ExecuteAndCaptureAsync(factory);
        return result.Result;
    }

    public static AsyncRetryPolicy<T> WaitAndRetryAsyncPolicy<T>(RetrySettings settings, ICollection<Type>? retryExceptions = null, bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null)
    {
        PolicyBuilder<T> policyBuilder = GetPolicyBuilder(retryExceptions, includeInner, exceptionCallback, returnCallback);
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: settings.RetryMaxAttempts);
        return policyBuilder.WaitAndRetryAsync(delay);
    }

    public static IAsyncPolicy<T> GetWaitAndRetryAsyncPolicy<T>(RetrySettings settings, ICollection<Type>? retryExceptions = null,
        bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null)
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(RetrySettings.MedianFirstRetryDelaySeconds), retryCount: settings.RetryMaxAttempts);
        return GetPolicyBuilder(retryExceptions, includeInner, exceptionCallback, returnCallback)
            .WaitAndRetryAsync(delay);
    }

    public static AsyncCircuitBreakerPolicy<T> CiruitBreakerAsyncPolicy<T>(CircuitBreakerSettings settings, ICollection<Type>? retryExceptions = null, bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null)
    {
        PolicyBuilder<T> policyBuilder = GetPolicyBuilder(retryExceptions, includeInner, exceptionCallback, returnCallback);
        return policyBuilder.AdvancedCircuitBreakerAsync(settings.CircuitBreakerFailureThreshold,
                TimeSpan.FromSeconds(settings.CircuitBreakerSamplingDurationSeconds),
                settings.CircuitBreakerMinThroughput,
                TimeSpan.FromSeconds(settings.CircuitBreakerBreakDurationSeconds));
    }

    public static IAsyncPolicy<T> GetCiruitBreakerAsyncPolicy<T>(CircuitBreakerSettings settings, ICollection<Type>? retryExceptions = null, 
        bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null) => 
        GetPolicyBuilder(retryExceptions, includeInner, exceptionCallback, returnCallback)
        .AdvancedCircuitBreakerAsync(settings.CircuitBreakerFailureThreshold,
                TimeSpan.FromSeconds(settings.CircuitBreakerSamplingDurationSeconds),
                settings.CircuitBreakerMinThroughput,
                TimeSpan.FromSeconds(settings.CircuitBreakerBreakDurationSeconds));

    /// <summary>
    /// Builds the retry policy
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="retryExceptions">Collection of retryable exceptions</param>
    /// <param name="includeInner">Exceptions may be at any inner level</param>
    /// <param name="exceptionCallback">Callback method to investigate the exception to determine retryability (SqlException.ErrorCode)</param>
    /// <param name="returnCallback">Callback method to investigate the factory method return value to determine retryability (HttpResponseMessage.StatusCode)</param>
    /// <returns></returns>
    private static PolicyBuilder<T> GetPolicyBuilder<T>(ICollection<Type>? retryExceptions = null, bool includeInner = false, Func<Exception, bool>? exceptionCallback = null, Func<T, bool>? returnCallback = null)
    {
        if (retryExceptions == null && returnCallback == null) throw new InvalidOperationException("GetPolicyBuilder must have defined retryExceptions and/or returnCallback.");

        PolicyBuilder<T> policyBuilder = null!; // = HttpPolicyExtensions.HandleTransientHttpError();
        if (retryExceptions != null)
        {
            policyBuilder = includeInner
                ? Policy<T>.HandleInner<Exception>(ex => retryExceptions.Contains(ex.GetType()) && (exceptionCallback == null || exceptionCallback(ex)))
                : Policy<T>.Handle<Exception>(ex => retryExceptions.Contains(ex.GetType()) && (exceptionCallback == null || exceptionCallback(ex)));

            if (returnCallback != null)
                policyBuilder = policyBuilder.OrResult(r => returnCallback(r)); //T could be HttpResponseMessage which retryCallback takes to check StatusCode property for retryable values
        }
        else if (returnCallback != null)
            policyBuilder = Policy<T>.HandleResult(r => returnCallback(r)); //T could be HttpResponseMessage which retryCallback takes to check StatusCode property for retryable values

        return policyBuilder;
    }
}
