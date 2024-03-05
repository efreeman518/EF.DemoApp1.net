using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common;
using SampleApp.Bootstrapper;
using Testcontainers.MsSql;

namespace Test.Integration;

/// <summary>
/// Testing Domain, Application, and Infrastructure services and logic; not http endpoints
/// </summary>
public abstract class IntegrationTestBase
{
    protected const string ClientName = "IntegrationTest";

    protected readonly static IConfigurationRoot Config = Support.Utility.BuildConfiguration().AddUserSecrets<IntegrationTestBase>().Build();

    protected readonly IServiceProvider Services;
    protected readonly ILogger<IntegrationTestBase> Logger;

    //https://testcontainers.com/guides/testing-an-aspnet-core-web-app/
    public static readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

    protected IntegrationTestBase()
    {
        //Services for DI
        ServiceCollection services = [];

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

        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILogger<IntegrationTestBase>>();
        var testConfigSection = Config.GetSection("TestSettings");
        var dbConnectionString = _dbContainer.GetConnectionString().Replace("master", testConfigSection.GetValue("DBName", "TestDB"));
        Support.Utility.ConfigureTestDB(logger, services, testConfigSection, dbConnectionString);

        //IRequestContext - replace the Bootstrapper registered non-http 'BackgroundService' registration; injected into repositories
        services.AddTransient<IRequestContext<string>>(provider =>
        {
            var correlationId = Guid.NewGuid().ToString();
            return new RequestContext<string>(correlationId, $"Test.Integration-{correlationId}");
        });

        //build IServiceProvider for subsequent use finding/injecting services
        Services = services.BuildServiceProvider(validateScopes: true);

        //add logging for integration tests
        services.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug().AddApplicationInsights());
        Logger = Services.GetRequiredService<ILogger<IntegrationTestBase>>();

        Logger.Log(LogLevel.Information, "Test Initialized.");
    }

    [AssemblyInitialize]
    public static void Initialize(TestContext ctx)
    {
        ctx.GetHashCode();
    }
}
