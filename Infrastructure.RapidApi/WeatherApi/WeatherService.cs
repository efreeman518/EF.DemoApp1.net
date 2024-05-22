using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;

namespace Infrastructure.RapidApi.WeatherApi;

/// <summary>
/// https://rapidapi.com/weatherapi/api/weatherapi-com/
/// </summary>
public class WeatherService(ILogger<WeatherService> logger, IOptions<WeatherServiceSettings> settings, HttpClient httpClient) : IWeatherService
{
    public async Task<Result<WeatherRoot?>> GetCurrentAsync(string location)
    {
        _ = settings.GetHashCode();
        string url = $"/current.json?q={location}";
        return await GetWeatherAsync(url);
    }

    public async Task<Result<WeatherRoot?>> GetForecastAsync(string location, int? days = null, DateTime? date = null)
    {
        string url = $"/forecast.json?q={location}&days={days}&dt={date?.ToString("yyyy-MM-dd")}";
        return await GetWeatherAsync(url);
    }

    public async Task<Result<WeatherRoot?>> GetWeatherAsync(string url)
    {
        logger.LogInformation("GetWeatherAsync: {Url}", url);

        (HttpResponseMessage _, Result<WeatherRoot?> result) = await httpClient.HttpRequestAndResponseResultAsync<WeatherRoot?>(HttpMethod.Get, url, null);
        return result;
    }
}
