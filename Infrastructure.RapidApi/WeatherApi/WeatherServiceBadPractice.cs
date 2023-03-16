using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;
using System.Text.Json;

namespace Infrastructure.RapidApi.WeatherApi;

/// <summary>
/// https://rapidapi.com/weatherapi/api/weatherapi-com/
/// </summary>
public class WeatherServiceBadPractice : IWeatherService
{
    private readonly ILogger _logger;
    private readonly WeatherServiceSettings _settings;

    public WeatherServiceBadPractice(ILogger<WeatherServiceBadPractice> logger, IOptions<WeatherServiceSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
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
        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url),
            Headers =
            {
                { "X-RapidAPI-Key", _settings.Key },
                { "X-RapidAPI-Host", _settings.Host },
            }
        };

        _logger.LogInformation("GetWeatherAsync start: {Url}", request.RequestUri);

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<WeatherRoot>(body);

        _logger.LogInformation("GetWeatherAsync complete: {data}", data.SerializeToJson());
        return data;
    } 
}
