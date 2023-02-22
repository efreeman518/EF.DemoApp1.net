using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp.Bootstrapper.HealthChecks;

public class ExternalServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<ExternalServiceHealthCheck> _logger;

    public ExternalServiceHealthCheck(ILogger<ExternalServiceHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ExternalServiceHealthCheck - Start");

        var status = HealthStatus.Healthy;
        try
        {
            //external resource/dependency health check
#pragma warning disable CS0162 // Unreachable code detected
            if (false) status = HealthStatus.Unhealthy;
#pragma warning restore CS0162 // Unreachable code detected

        }
        catch (Exception ex)
        {
            status = HealthStatus.Unhealthy;
            _logger.LogError(ex, "ExternalServiceHealthCheck - Error");
        }

        return Task.FromResult(new HealthCheckResult(status,
            description: $"ExternalService is {status}.",
            exception: null,
            data: null));
    }
}
