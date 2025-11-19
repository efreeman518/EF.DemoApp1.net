using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Net;

namespace Package.Infrastructure.Utility.UI;

public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a customized resilience handler that implements all standard resilience strategies 
    /// but excludes specified HTTP status codes from retry attempts.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="pipelineName">Name of the resilience pipeline.</param>
    /// <param name="excludedStatusCodes">HTTP status codes that should not trigger retries.</param>
    /// <param name="maxRetryAttempts"></param>
    /// <param name="atteptTimeoutInSeconds"></param>
    /// <param name="totalTimeoutSeconds"></param>
    /// <param name="configureOptions">Optional callback to configure additional resilience options.</param>
    /// <returns>The HTTP client builder with resilience configured.</returns>
    public static IHttpClientBuilder AddCustomResilience(
        this IHttpClientBuilder builder,
        string pipelineName,
        List<int> excludedStatusCodes,
        int maxRetryAttempts = 3,
        int atteptTimeoutInSeconds = 20,
        int totalTimeoutSeconds = 60,
        Action<HttpStandardResilienceOptions>? configureOptions = null)
    {
        builder.AddResilienceHandler(pipelineName, options =>
        {
            // Configure standard options first
            var standardOptions = new HttpStandardResilienceOptions
            {
                // Standard retry settings
                Retry =
                    {
                        MaxRetryAttempts = maxRetryAttempts,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                    },

                // Standard circuit breaker settings
                CircuitBreaker =
                    {
                        SamplingDuration = TimeSpan.FromSeconds(30),
                        MinimumThroughput = 10,
                        FailureRatio = 0.5,
                        BreakDuration = TimeSpan.FromSeconds(15),
                    },

                // Standard timeout settings
                AttemptTimeout = { Timeout = TimeSpan.FromSeconds(atteptTimeoutInSeconds) },

                TotalRequestTimeout = { Timeout = TimeSpan.FromSeconds(totalTimeoutSeconds) }
            };

            // Apply custom configuration if provided
            configureOptions?.Invoke(standardOptions);

            // Customize the retry behavior to exclude specific status codes
            standardOptions.Retry.ShouldHandle = args =>
            {
                // Check HTTP status codes to exclude
                if (args.Outcome.Result?.StatusCode is HttpStatusCode statusCode)
                {
                    int statusCodeValue = (int)statusCode;

                    // If the status code is in our exclude list, don't retry
                    if (excludedStatusCodes.Contains(statusCodeValue))
                    {
                        return ValueTask.FromResult(false);
                    }

                    // Use standard transient status code checks (like 408, 429, etc.)
                    // but exclude any specifically excluded codes

                    return ValueTask.FromResult(
                            statusCode == HttpStatusCode.RequestTimeout ||
                            statusCode == HttpStatusCode.TooManyRequests ||
                            (statusCode >= HttpStatusCode.InternalServerError && !excludedStatusCodes.Contains(statusCodeValue))
                        );
                }

                // Don't retry on timeouts (typically these would be OperationCanceledException)
                if (args.Outcome.Exception is OperationCanceledException)
                {
                    return ValueTask.FromResult(false);
                }

                // Retry on network-related exceptions but not timeouts
                return ValueTask.FromResult(args.Outcome.Exception is HttpRequestException);
            };

            // Apply the options to the resilience pipeline builder
            // Ensure the correct method or extension method is used
            options.AddRetry(standardOptions.Retry);
            options.AddCircuitBreaker(standardOptions.CircuitBreaker);
            options.AddTimeout(standardOptions.AttemptTimeout);
        });

        // Return the original builder to maintain the method's return type
        return builder;
    }
}