using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common;
using Respawn;
using Respawn.Graph;
using SampleApp.Bootstrapper;
using System.Data;
using System.Data.Common;
using Testcontainers.MsSql;

namespace Test.Integration;

/// <summary>
/// Testing Domain, Application, and Infrastructure services and logic; not http endpoints
/// Constructor (if defined) runs before each test
/// </summary>
public abstract class IntegrationTestBase
{
    protected const string ClientName = "IntegrationTest";
    protected readonly static IConfigurationRoot Config = Support.Utility.BuildConfiguration().AddUserSecrets<IntegrationTestBase>().Build();
    protected static IServiceProvider Services => _services;
    protected static ILogger Logger => _logger;

    private static IServiceProvider _services = null!;
    private static ILogger<IntegrationTestBase> _logger = null!;

    //https://testcontainers.com/guides/testing-an-aspnet-core-web-app/
    private static readonly MsSqlContainer DbContainer = new MsSqlBuilder().Build();
    private static string _dbConnectionString = null!;

    //https://github.com/jbogard/Respawn
    private static Respawner _respawner = null!;
    private static DbConnection _dbConnection = null!;

    /// <summary>
    /// Configure the test class; runs once before any test class at [ClassInitialize]
    /// </summary>
    /// <returns></returns>
    protected static async Task ConfigureTestInstanceAsync()
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

        //database
        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<IntegrationTestBase>>();
        var testConfigSection = Config.GetSection("TestSettings");
        Support.Utility.ConfigureTestDB(logger, services, testConfigSection, _dbConnectionString);
        await InitializeRespawner();

        //IRequestContext - replace the Bootstrapper registered non-http 'BackgroundService' registration; injected into repositories
        services.AddTransient<IRequestContext<string>>(provider =>
        {
            var correlationId = Guid.NewGuid().ToString();
            return new RequestContext<string>(correlationId, $"Test.Integration-{correlationId}");
        });

        //build IServiceProvider for subsequent use finding/injecting services
        _services = services.BuildServiceProvider(validateScopes: true);

        //add logging for integration tests
        services.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug().AddApplicationInsights());
        _logger = _services.GetRequiredService<ILogger<IntegrationTestBase>>();

        _logger.Log(LogLevel.Information, "Test Initialized.");
    }

    protected static async Task StartContainerAsync()
    {
        await DbContainer.StartAsync();
        _dbConnectionString = DbContainer.GetConnectionString().Replace("master", Config.GetValue("TestSettings:DBName", "TestDB"));
        _dbConnection = new SqlConnection(_dbConnectionString);
    }
    protected static async Task StopContainerAsync()
    {
        await _dbConnection.CloseAsync();
        await DbContainer.StopAsync();
    }

    private static async Task InitializeRespawner()
    {
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["todo"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    public static async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    [AssemblyInitialize]
    public static void Initialize(TestContext ctx)
    {
        ctx.GetHashCode();
    }
}
