using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Package.Infrastructure.AspNetCore.HealthChecks;
public sealed class HealthLoggingPublisher(ILogger<HealthLoggingPublisher> logger) : IHealthCheckPublisher
{
    // int.MinValue = "uninitialized"
    private int _lastStatus = int.MinValue;

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        var current = (int)report.Status;

        // Atomically swap and log only on change
        var previous = Interlocked.Exchange(ref _lastStatus, current);
        if (previous == current)
            return Task.CompletedTask;

        var failing = report.Entries
            .Where(e => e.Value.Status != HealthStatus.Healthy)
            .Select(e => $"{e.Key}:{e.Value.Status}")
            .ToArray();

        switch (report.Status)
        {
            case HealthStatus.Healthy:
                logger.LogInformation("Health summary: Healthy");
                break;
            case HealthStatus.Degraded:
                logger.LogWarning("Health summary: Degraded; failing={Failing}", string.Join(",", failing));
                break;
            case HealthStatus.Unhealthy:
                logger.LogError("Health summary: Unhealthy; failing={Failing}", string.Join(",", failing));
                break;
        }

        return Task.CompletedTask;
    }
}
