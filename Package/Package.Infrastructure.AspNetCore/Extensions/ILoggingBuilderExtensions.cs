using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace Package.Infrastructure.AspNetCore.Extensions;
public static class OpenTelemetryLoggingExtensions
{
    public static ILoggingBuilder AddOpenTelemetryWithConfig(
        this ILoggingBuilder loggingBuilder,
        IConfigurationSection loggingSection,
        string appInsightsConnectionString,
        Action<OpenTelemetryLoggerOptions>? configureOpenTelemetry = null)
    {
        // Clear existing providers if desired
        loggingBuilder.ClearProviders();

        // Apply log level filters from configuration first
        loggingBuilder.AddConfiguration(loggingSection);

        // Configure OpenTelemetry
        loggingBuilder.AddOpenTelemetry(options =>
        {
            // Default OpenTelemetry configuration
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;

            options.AddAzureMonitorLogExporter(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });

            // Allow additional customization
            configureOpenTelemetry?.Invoke(options);
        });

        return loggingBuilder;
    }
}