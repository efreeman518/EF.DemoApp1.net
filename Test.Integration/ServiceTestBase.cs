using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Test.Integration;

/// <summary>
/// Testing Application and Domain services and logic; not http endpoints
/// </summary>
public abstract class ServiceTestBase
{
    protected const string ClientName = "IntegrationTest";

    protected static IConfigurationRoot Config => Utility.Config;
    protected readonly IServiceProvider Services;
    protected readonly ILogger<ServiceTestBase> Logger;

    protected ServiceTestBase()
    {
        //DI
        ServiceCollection services = new();

        //register infrastructure and domain services (non-http)
        new SampleApp.Bootstrapper.Startup(Config).ConfigureServices(services);

        //add logging for integration tests
        services.AddApplicationInsightsTelemetryWorkerService(Config);
        services.AddLogging(configure => configure.AddConsole().AddDebug().AddApplicationInsights());

        //build IServiceProvider for subsequent use finding/injecting services
        Services = services.BuildServiceProvider(validateScopes: true);

        //logging
        Logger = Services.GetRequiredService<ILogger<ServiceTestBase>>();

        Logger.Log(LogLevel.Information, "Test Initialized.");
    }

    [AssemblyInitialize]
    public static void Initialize(TestContext ctx)
    {
        ctx.GetHashCode();
    }

}
