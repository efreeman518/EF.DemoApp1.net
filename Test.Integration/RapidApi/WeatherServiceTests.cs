using Infrastructure.RapidApi.WeatherApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        //act
        var weather = (await _svc.GetCurrentAsync("San Diego, CA")).Match(
            Succ: response => response,
            Fail: err => throw err);

        //assert 
        Assert.IsNotNull(weather);

        _logger.InfoLog($"GetCurrentAsync_pass - Complete: {weather.SerializeToJson()}");
    }

    [TestMethod]
    public async Task GetForecastAsync_pass()
    {
        _logger.InfoLog("GetForecastAsync_pass - Start");

        //act
        var weather = (await _svc.GetForecastAsync("San Diego, CA", 3)).Match(
            Succ: response => response,
            Fail: err => throw err);
        Assert.IsNotNull(weather);

        var forecast = (await _svc.GetCurrentAsync("Paris, France")).Match(
            Succ: response => response,
            Fail: err => throw err);

        //assert 
        Assert.IsNotNull(forecast);

        _logger.InfoLog($"GetForecastAsync_pass - Complete: {weather.SerializeToJson()}");
    }
}
