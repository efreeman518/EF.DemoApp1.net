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

        Logger.LogInformation("GetCurrentAsync_pass - Complete: {0}", weather.SerializeToJson());
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

        Logger.LogInformation("GetForecastAsync_pass - Complete: {0}", weather.SerializeToJson());
    }
}
