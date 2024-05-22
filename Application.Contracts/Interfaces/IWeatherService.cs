using Application.Contracts.Model;
using LanguageExt.Common;

namespace Application.Contracts.Interfaces;

public interface IWeatherService
{
    Task<Result<WeatherRoot?>> GetCurrentAsync(string location);
    Task<Result<WeatherRoot?>> GetForecastAsync(string location, int? days = null, DateTime? date = null);
}
