using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common.Contracts;
using SampleApp.Bootstrapper;

namespace Test.Support;

/// <summary>
/// Testing Domain, Application, and Infrastructure services/logic; not http endpoints
/// MSTest Constructor (if defined) runs before each test
/// </summary>
public abstract class IntegrationTestBase
{
    protected readonly static IConfigurationRoot Config = Utility.BuildConfiguration().AddUserSecrets<IntegrationTestBase>().Build();
    protected readonly static IConfigurationSection TestConfigSection = Config.GetSection("TestSettings");
    //protected static IServiceProvider Services => _services;
    //protected static IServiceScope ServiceScope => _serviceScope;
    //protected static ILogger Logger => _logger;

    //MSTest requires static ClassInitialize/ClassCleanup methods which are used to initialize the DB
#pragma warning disable S2223 // Non-constant static fields should not be visible; 
#pragma warning disable CA2211 // Non-constant fields should not be visible
    protected static IServiceProvider Services = null!;
    protected static IServiceScope ServiceScope = null!;
    protected static ILogger<IntegrationTestBase> Logger = null!;
    protected static ServiceCollection ServicesCollection = [];
#pragma warning restore CA2211 // Non-constant fields should not be visible
#pragma warning restore S2223 // Non-constant static fields should not be visible

    /// <summary>
    /// Runs before each test
    /// </summary>
    protected IntegrationTestBase()
    {

    }

    /// <summary>
    /// Configure the test class; runs once before any test class [MSTest:ClassInitialize], [BenchmarkDotNet:GlobalSetup]
    /// </summary>
    /// <param name="testContextName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected static void ConfigureServices(string testContextName)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders().AddConsole().AddDebug().AddApplicationInsights();
        });
        ServicesCollection.AddSingleton(loggerFactory);

        //bootstrapper service registrations - infrastructure, domain, application 
        ServicesCollection
            .RegisterInfrastructureServices(Config)
            .RegisterBackgroundServices(Config)
            .RegisterDomainServices(Config)
            .RegisterApplicationServices(Config);

        //register services for testing that are not already registered in the bootstraper

        ServicesCollection.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug().AddApplicationInsights());
        Logger = ServicesCollection.BuildServiceProvider().GetRequiredService<ILogger<IntegrationTestBase>>();

        //IRequestContext - replace the Bootstrapper registered non-http 'BackgroundService' registration; injected into repositories
        ServicesCollection.AddTransient<IRequestContext<string>>(provider =>
        {
            var correlationId = Guid.NewGuid().ToString();
            return new RequestContext<string>(correlationId, $"Test.Support.IntegrationTestBase-{correlationId}");
        });

        //build IServiceProvider for subsequent use finding/injecting services
        Services = ServicesCollection.BuildServiceProvider(validateScopes: true);
        ServiceScope = Services.CreateScope();
        Logger.Log(LogLevel.Information, "{TestContextName} Base ConfigureServices complete.", testContextName);
    }
}
