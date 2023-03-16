using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;
using System.Collections.Specialized;

namespace Infrastructure.RapidApi.WeatherApi;

/// <summary>
/// https://rapidapi.com/weatherapi/api/weatherapi-com/
/// </summary>
public class WeatherServiceBetterPractice : IWeatherService
{
    private readonly ILogger _logger;
    private readonly WeatherServiceSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public WeatherServiceBetterPractice(ILogger<WeatherServiceBetterPractice> logger, IOptions<WeatherServiceSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WeatherRoot?> GetCurrentAsync(string location)
    {
        string url = $"{_settings.BaseUrl}/current.json?q={location}";
        return await GetWeatherAsync(url);
    }

    public async Task<WeatherRoot?> GetForecastAsync(string location, int? days = null, DateTime? date = null)
    {
        string url = $"{_settings.BaseUrl}/forecast.json?q={location}&days={days}&dt={date?.ToString("yyyy-MM-dd")}";
        return await GetWeatherAsync(url);
    }

    public async Task<WeatherRoot?> GetWeatherAsync(string url)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var headers = new NameValueCollection
        {
            { "X-RapidAPI-Key", _settings.Key },
            { "X-RapidAPI-Host", _settings.Host }
        };
        _logger.LogInformation("GetWeatherAsync start: {Url}", url);

        (HttpResponseMessage _, WeatherRoot? data) = await httpClient.HttpRequestAndResponseAsync<object, WeatherRoot?>(HttpMethod.Get, url, null, headers);

        _logger.LogInformation("GetWeatherAsync complete: {Url}", data?.SerializeToJson());
        return data;
    }
}
