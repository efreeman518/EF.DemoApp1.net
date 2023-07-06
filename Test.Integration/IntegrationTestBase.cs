using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common;
using SampleApp.Bootstrapper;
using System;

namespace Test.Integration;

/// <summary>
/// Testing Application and Domain services and logic; not http endpoints
/// </summary>
public abstract class IntegrationTestBase
{
    protected const string ClientName = "IntegrationTest";

    protected static IConfigurationRoot Config => Utility.Config;
    protected readonly IServiceProvider Services;
    protected readonly ILogger<IntegrationTestBase> Logger;

    protected IntegrationTestBase()
    {
        //DI
        ServiceCollection services = new();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().AddDebug().AddApplicationInsights();
        });
        services.AddSingleton(loggerFactory);

        //bootstrapper service registrations - infrastructure, domain, application 
        services
            .RegisterInfrastructureServices(Config)
            .RegisterBackgroundServices(Config)
            .RegisterDomainServices(Config)
            .RegisterApplicationServices(Config);

        //add logging for integration tests
        //services.AddApplicationInsightsTelemetryWorkerService(Config);
        services.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug().AddApplicationInsights());

        //IRequestContext - replace the Bootstrapper registered non-http 'BackgroundService' registration; injected into repositories
        services.AddTransient<IRequestContext>(provider =>
        {
            var correlationId = Guid.NewGuid().ToString();
            return new RequestContext(correlationId, $"Test.Integration-{correlationId}");
        });

        //build IServiceProvider for subsequent use finding/injecting services
        Services = services.BuildServiceProvider(validateScopes: true);

        //logging
        Logger = Services.GetRequiredService<ILogger<IntegrationTestBase>>();

        Logger.Log(LogLevel.Information, "Test Initialized.");
    }

    [AssemblyInitialize]
    public static void Initialize(TestContext ctx)
    {
        ctx.GetHashCode();
    }

}
