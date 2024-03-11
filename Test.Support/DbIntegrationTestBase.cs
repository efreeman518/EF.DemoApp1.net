using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common;
using Respawn;
using Respawn.Graph;
using SampleApp.Bootstrapper;
using System.Data;
using System.Data.Common;
using Testcontainers.MsSql;

namespace Test.Support;

/// <summary>
/// Testing Domain, Application, and Infrastructure services/logic; not http endpoints
/// MSTest Constructor (if defined) runs before each test
/// </summary>
public abstract class DbIntegrationTestBase
{
    protected const string ClientName = "IntegrationTest";
    protected readonly static IConfigurationRoot Config = Utility.BuildConfiguration().AddUserSecrets<DbIntegrationTestBase>().Build();
    private readonly static IConfigurationSection _testConfigSection = Config.GetSection("TestSettings");
    protected static IServiceProvider Services => _services;
    protected static ILogger Logger => _logger;

    private static IServiceProvider _services = null!;
    private static ILogger<DbIntegrationTestBase> _logger = null!;

    //https://testcontainers.com/guides/testing-an-aspnet-core-web-app/
    private static readonly MsSqlContainer DbContainer = new MsSqlBuilder().Build();
    private static string _dbConnectionString = null!;
    private static TodoDbContextBase _dbContext = null!;

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
            builder.ClearProviders().AddConsole().AddDebug().AddApplicationInsights();
        });
        services.AddSingleton(loggerFactory);

        //bootstrapper service registrations - infrastructure, domain, application 
        services
            .RegisterInfrastructureServices(Config)
            .RegisterBackgroundServices(Config)
            .RegisterDomainServices(Config)
            .RegisterApplicationServices(Config);

        services.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug().AddApplicationInsights());
        _logger = services.BuildServiceProvider().GetRequiredService<ILogger<DbIntegrationTestBase>>();

        //database
        var dbSource = _testConfigSection.GetValue<string?>("DBSource", null);
        _dbContext = DbSupport.ConfigureTestDB<TodoDbContextTrxn>(_logger, services, dbSource, _dbConnectionString);
        await InitializeRespawner();

        //IRequestContext - replace the Bootstrapper registered non-http 'BackgroundService' registration; injected into repositories
        services.AddTransient<IRequestContext<string>>(provider =>
        {
            var correlationId = Guid.NewGuid().ToString();
            return new RequestContext<string>(correlationId, $"Test.Support.IntegrationTestBase-{correlationId}");
        });

        //build IServiceProvider for subsequent use finding/injecting services
        _services = services.BuildServiceProvider(validateScopes: true);
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

    public static async Task ResetDatabaseAsync(bool reseed = true)
    {
        await _respawner.ResetAsync(_dbConnection);
        if (reseed && _testConfigSection.GetValue("SeedData", false))
        {
            var seedPaths = _testConfigSection.GetSection("SeedFiles:Paths").Get<string[]>();
            if (seedPaths != null && seedPaths.Length > 0)
            {
                _dbContext.SeedRawSqlFiles(_logger, [.. seedPaths], _testConfigSection.GetValue("SeedFiles:SearchPattern", "*.sql")!);
            }

            try
            {
                _logger.LogInformation($"Seeding default entity data.");
                _dbContext.SeedDefaultEntityData(false);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred seeding the database with test data. Error: {Message}", ex.Message);
            }
        }
    }
}
