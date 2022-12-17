using Infrastructure.BackgroundServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Test.Integration;

[TestClass]
public abstract class IntegrationTestBase
{
    protected readonly IServiceProvider Services;
    protected readonly ILoggerFactory LoggerFactory;

    //[AssemblyInitialize]
    //public static void Initialize(TestContext ctx) //ctx required for [AssemblyInitialize] run 
    protected IntegrationTestBase()
    {
        //DI
        ServiceCollection services = new();

        //queued background service - fire and forget 
        services.AddHostedService<BackgroundTaskService>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        services.AddLogging(configure => configure.AddConsole().AddDebug());

        //build IServiceProvider for subsequent use finding/injecting services
        Services = services.BuildServiceProvider();

        LoggerFactory = Services.GetRequiredService<ILoggerFactory>();
    }

}
