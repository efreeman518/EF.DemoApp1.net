using Infrastructure.RapidApi.WeatherApi;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp.Bootstrapper.HealthChecks;

public class ExternalServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<ExternalServiceHealthCheck> _logger;
    private readonly IWeatherService _weatherService;

    public ExternalServiceHealthCheck(ILogger<ExternalServiceHealthCheck> logger, IWeatherService weatherService)
    {
        _logger = logger;
        _weatherService = weatherService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ExternalServiceHealthCheck - Start");

        var status = HealthStatus.Healthy;
        try
        {
            var response = _weatherService.GetCurrentAsync("San Diego, CA");
            if (response == null) status = HealthStatus.Unhealthy;
            _logger.LogInformation("ExternalServiceHealthCheck - Complete");
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
