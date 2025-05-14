using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace Package.Infrastructure.AspNetCore.Extensions;

/// <summary>
/// aspire handles OpenTelemetry logging configuration for ASP.NET Core applications.
/// </summary>
public static class OpenTelemetryLoggingExtensions
{
    public static ILoggingBuilder AddOpenTelemetryWithConfig(
        this ILoggingBuilder loggingBuilder,
        IConfiguration config,
        Action<OpenTelemetryLoggerOptions>? configureOpenTelemetry = null)
    {
        // Clear existing providers if desired
        loggingBuilder.ClearProviders();

        // Apply log level filters from configuration first
        loggingBuilder.AddConfiguration(config.GetSection("Logging"));

        // Configure OpenTelemetry
        loggingBuilder.AddOpenTelemetry(options =>
        {
            // Default OpenTelemetry configuration
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;

            // Allow additional customization
            configureOpenTelemetry?.Invoke(options);
        });

        return loggingBuilder;
    }
}