using Application.Contracts.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace SampleApp.Bootstrapper.HealthChecks;

public class WeatherServiceHealthCheck(ILogger<WeatherServiceHealthCheck> logger, IWeatherService weatherService) : IHealthCheck
{
    private readonly ILogger<WeatherServiceHealthCheck> _logger = logger;
    private readonly IWeatherService _weatherService = weatherService;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WeatherServiceHealthCheck - Start");

        HealthStatus status;
        Exception? exHealth = null;
        try
        {
            var result = await _weatherService.GetCurrentAsync("San Diego, CA");
            if (result.IsSuccess)
            {
                status = result.Value != null ? HealthStatus.Healthy : HealthStatus.Unhealthy;
            }
            else
            {
                exHealth = new Exception(result.Error);
                status = HealthStatus.Unhealthy;
            }

            _logger.LogInformation("WeatherServiceHealthCheck - Complete");
        }
        catch (Exception ex)
        {
            exHealth = ex;
            status = HealthStatus.Unhealthy;
            _logger.LogError(ex, "WeatherServiceHealthCheck - Error");
        }

        return new HealthCheckResult(status,
            description: $"WeatherService is {status}.",
            exception: exHealth,
            data: null);
    }
}
