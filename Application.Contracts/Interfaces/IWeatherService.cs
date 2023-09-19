using Application.Contracts.Model;

namespace Application.Contracts.Interfaces;

public interface IWeatherService
{
    Task<WeatherRoot?> GetCurrentAsync(string location);
    Task<WeatherRoot?> GetForecastAsync(string location, int? days = null, DateTime? date = null);
}
