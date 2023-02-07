using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
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
        _logger.LogInformation("Start ExternalServiceHealthCheck");

        //external resource/dependency health check
        string sResult = "".GetHashCode().ToString();

        //parse the result to determine if external resource/dependency is healthy
        if (string.IsNullOrEmpty(sResult)) return Task.FromResult(HealthCheckResult.Unhealthy($"Resource/Dependency is unhealthy: {sResult}"));
        return Task.FromResult(HealthCheckResult.Healthy("Resource/Dependency is healthy."));
    }
}
