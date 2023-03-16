using Infrastructure.RapidApi.WeatherApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Extensions;
using System.Threading.Tasks;

namespace Test.Integration.Application;

[TestClass]
public class WeatherServiceTests : ServiceTestBase
{
    public WeatherServiceTests() : base()
    { }

    #region Bad Practice

    [TestMethod]
    public async Task BadPractice_GetCurrentAsync_pass()
    {
        Logger.LogInformation("BadPractice_GetCurrentAsync_pass - Start");

        //arrange
        using IServiceScope serviceScope = Services.CreateScope(); //needed for injecting scoped services
        WeatherServiceBadPractice svc = (WeatherServiceBadPractice)serviceScope.ServiceProvider.GetRequiredService(typeof(IWeatherService));

        //act
        var weather = await svc.GetCurrentAsync("San Diego, CA");

        //assert 
        Assert.IsNotNull(weather);

        Logger.LogInformation("BadPractice_GetCurrentAsync_pass - Complete: {Weather}", weather);
    }

    [TestMethod]
    public async Task BadPractice_GetForecastAsync_pass()
    {
        Logger.LogInformation("BadPractice_GetForecastAsync_pass - Start");

        //arrange
        using IServiceScope serviceScope = Services.CreateScope(); //needed for injecting scoped services
        WeatherServiceBadPractice svc = (WeatherServiceBadPractice)serviceScope.ServiceProvider.GetRequiredService(typeof(IWeatherService));

        //act
        var weather = await svc.GetForecastAsync("San Diego, CA", 3);

        //assert 
        Assert.IsNotNull(weather);

        Logger.LogInformation("BadPractice_GetForecastAsync_pass - Complete: {Weather}", weather);
    }

    #endregion

    #region Better Practice

    [TestMethod]
    public async Task BetterPractice_GetCurrentAsync_pass()
    {
        Logger.LogInformation("BetterPractice_GetCurrentAsync_pass - Start");

        //arrange
        using IServiceScope serviceScope = Services.CreateScope(); //needed for injecting scoped services
        WeatherServiceBadPractice svc = (WeatherServiceBadPractice)serviceScope.ServiceProvider.GetRequiredService(typeof(IWeatherService));

        //act
        var weather = await svc.GetCurrentAsync("San Diego, CA");

        //assert 
        Assert.IsNotNull(weather);

        Logger.LogInformation("BetterPractice_GetCurrentAsync_pass - Complete: {Weather}", weather.SerializeToJson());
    }

    [TestMethod]
    public async Task BetterPractice_GetForecastAsync_pass()
    {
        Logger.LogInformation("BetterPractice_GetForecastAsync_pass - Start");

        //arrange
        using IServiceScope serviceScope = Services.CreateScope(); //needed for injecting scoped services
        WeatherServiceBadPractice svc = (WeatherServiceBadPractice)serviceScope.ServiceProvider.GetRequiredService(typeof(IWeatherService));

        //act
        var weather = await svc.GetForecastAsync("San Diego, CA", 3);

        //assert 
        Assert.IsNotNull(weather);

        Logger.LogInformation("BetterPractice_GetForecastAsync_pass - Complete: {Weather}", weather.SerializeToJson());
    }

#endregion
}
