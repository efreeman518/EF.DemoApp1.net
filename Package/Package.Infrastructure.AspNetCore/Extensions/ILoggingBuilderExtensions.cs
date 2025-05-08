using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Package.Infrastructure.AspNetCore.Extensions;
public static class OpenTelemetryLoggingExtensions
{
    public static ILoggingBuilder AddConfiguredOpenTelemetryLogging(
        this ILoggingBuilder loggingBuilder,
        IConfigurationSection logLevelSection,
        string azureMonitorConnectionString)
    {
        var logLevelConfig = logLevelSection
            .GetChildren()
            .ToDictionary(
                s => s.Key,
                s => Enum.TryParse<LogLevel>(s.Value, out var level) ? level : LogLevel.Information
            );

        loggingBuilder.ClearProviders();

        // Apply filtering to the logging system (applies to all providers including OpenTelemetry)
        loggingBuilder.AddFilter((category, level) =>
        {
            if (logLevelConfig.TryGetValue(category!, out var exactLevel))
                return level >= exactLevel;

            foreach (var kvp in logLevelConfig)
            {
                if (category!.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    return level >= kvp.Value;
            }

            if (logLevelConfig.TryGetValue("Default", out var defaultLevel))
                return level >= defaultLevel;

            return level >= LogLevel.Information;
        });

        loggingBuilder.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;

            options.AddAzureMonitorLogExporter(exporterOptions =>
            {
                exporterOptions.ConnectionString = azureMonitorConnectionString;
            });
        });

        return loggingBuilder;
    }
}