using Application.Contracts.Interfaces;
using Infrastructure.RapidApi.WeatherApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Extensions;

namespace Test.Integration.Application;

[Ignore("RapidApi credentials required in config settings.")]

[TestClass]
public class WeatherServiceTests : IntegrationTestBase
{
    public WeatherServiceTests() : base()
    { }

    [TestMethod]
    public async Task GetCurrentAsync_pass()
    {
        Logger.LogInformation("GetCurrentAsync_pass - Start");

        //arrange
        using IServiceScope serviceScope = Services.CreateScope(); //needed for injecting scoped services
        WeatherService svc = (WeatherService)serviceScope.ServiceProvider.GetRequiredService(typeof(IWeatherService));

        //act
        var weather = await svc.GetCurrentAsync("San Diego, CA");

        //assert 
        Assert.IsNotNull(weather);

        Logger.LogInformation("GetCurrentAsync_pass - Complete: {Weather}", weather.SerializeToJson());
    }

    [TestMethod]
    public async Task GetForecastAsync_pass()
    {
        Logger.LogInformation("GetForecastAsync_pass - Start");

        //arrange
        using IServiceScope serviceScope = Services.CreateScope(); //needed for injecting scoped services
        WeatherService svc = (WeatherService)serviceScope.ServiceProvider.GetRequiredService(typeof(IWeatherService));

        //act
        var weather = await svc.GetForecastAsync("San Diego, CA", 3);
        var weather2 = await svc.GetCurrentAsync("Paris, France");

        //assert 
        Assert.IsNotNull(weather);
        Assert.IsNotNull(weather2);

        Logger.LogInformation("GetForecastAsync_pass - Complete: {Weather}", weather.SerializeToJson());
    }
}
