using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;

namespace Infrastructure.RapidApi.WeatherApi;

/// <summary>
/// https://rapidapi.com/weatherapi/api/weatherapi-com/
/// </summary>
public class WeatherService(ILogger<WeatherService> logger, IOptions<WeatherServiceSettings> settings, HttpClient httpClient) : IWeatherService
{
    public async Task<WeatherRoot?> GetCurrentAsync(string location)
    {
        _ = settings.GetHashCode();
        string url = $"/current.json?q={location}";
        return await GetWeatherAsync(url);
    }

    public async Task<WeatherRoot?> GetForecastAsync(string location, int? days = null, DateTime? date = null)
    {
        string url = $"/forecast.json?q={location}&days={days}&dt={date?.ToString("yyyy-MM-dd")}";
        return await GetWeatherAsync(url);
    }

    public async Task<WeatherRoot?> GetWeatherAsync(string url)
    {
        logger.LogInformation("GetWeatherAsync start: {Url}", url);

        (HttpResponseMessage _, WeatherRoot? data) = await httpClient.HttpRequestAndResponseAsync<WeatherRoot?>(HttpMethod.Get, url, null);

        logger.LogInformation("GetWeatherAsync complete: {Url}", data?.SerializeToJson());
        return data;
    }
}
