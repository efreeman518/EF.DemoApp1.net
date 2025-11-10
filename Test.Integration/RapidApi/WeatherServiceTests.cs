using Infrastructure.RapidApi.WeatherApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;

namespace Test.Integration.RapidApi;

[Ignore("RapidApi (external weather service) credentials required in config settings.")]

[TestClass]
public class WeatherServiceTests
{
    protected readonly ILogger<WeatherServiceTests> _logger;
    private readonly WeatherService _svc;

    public WeatherServiceTests()
    {
        IConfigurationRoot config = Support.Utility.BuildConfiguration().Build();

        //logger
        var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().AddDebug().AddApplicationInsights();
            });
        _logger = loggerFactory.CreateLogger<WeatherServiceTests>();

        //settings
        WeatherServiceSettings settings = new();
        config.GetSection(WeatherServiceSettings.ConfigSectionName).Bind(settings);
        var oSettings = Options.Create(settings);

        //httpclient
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.GetValue<string>("WeatherServiceSettings:BaseUrl")!)
        };
        httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", config.GetValue<string>("WeatherServiceSettings:Key")!);
        httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", config.GetValue<string>("WeatherServiceSettings:Host")!);

        _svc = new WeatherService(loggerFactory.CreateLogger<WeatherService>(), oSettings, httpClient);
    }

    [TestMethod]
    public async Task GetCurrentAsync_pass()
    {
        _logger.InfoLog("GetCurrentAsync_pass - Start");

        var result = await _svc.GetCurrentAsync("San Diego, CA");

        if (result.IsSuccess)
        {
            var weather = result.Value;
            Assert.IsNotNull(weather);
            _logger.InfoLog($"GetCurrentAsync_pass - Complete: {weather.SerializeToJson()}");
        }
        else
        {
            throw new InvalidOperationException(string.Join(",", result.Errors));
        }
    }

    [TestMethod]
    public async Task GetForecastAsync_pass()
    {
        _logger.InfoLog("GetForecastAsync_pass - Start");

        var forecastResult = await _svc.GetForecastAsync("San Diego, CA", 3);

        if (forecastResult.IsSuccess)
        {
            var weather = forecastResult.Value;
            Assert.IsNotNull(weather);
            _logger.InfoLog($"GetForecastAsync_pass - Complete: {weather.SerializeToJson()}");
        }
        else
        {
            throw new InvalidOperationException(string.Join(",", forecastResult.Errors));
        }

        var currentResult = await _svc.GetCurrentAsync("Paris, France");

        if (currentResult.IsSuccess)
        {
            var forecast = currentResult.Value;
            Assert.IsNotNull(forecast);
        }
        else
        {
            throw new InvalidOperationException(string.Join(",", currentResult.Errors));
        }
    }
}
