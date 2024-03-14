using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common;
using Package.Infrastructure.Data.Contracts;
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
    protected readonly static IConfigurationSection TestConfigSection = Config.GetSection("TestSettings");
    protected static IServiceProvider Services => _services;
    protected static IServiceScope ServiceScope => _serviceScope;
    protected static ILogger Logger => _logger;
    protected static TodoDbContextBase DbContext => _dbContext;

    private static IServiceProvider _services = null!;
    private static IServiceScope _serviceScope = null!;
    private static ILogger<DbIntegrationTestBase> _logger = null!;
    private static TodoDbContextBase _dbContext = null!;

    //https://testcontainers.com/guides/testing-an-aspnet-core-web-app/
    private static MsSqlContainer _dbContainer = null!;
    private static string _dbConnectionString = null!;

    //https://github.com/jbogard/Respawn
    private static Respawner _respawner = null!;
    private static DbConnection _dbConnection = null!;

    /// <summary>
    /// Configure the test class; runs once before any test class [MSTest:ClassInitialize], [BenchmarkDotNet:GlobalSetup]
    /// </summary>
    /// <returns></returns>
    protected static async Task ConfigureTestInstanceAsync(CancellationToken cancellationToken = default)
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
        var dbSource = TestConfigSection.GetValue<string?>("DBSource", null);
        if (dbSource == "TestContainer")
        {
            dbSource = _dbConnectionString;
        }
        _dbContext = DbSupport.ConfigureServicesTestDB<TodoDbContextTrxn>(_logger, services, dbSource);
        if (!_dbContext.Database.IsInMemory())
        {
            await _dbConnection.OpenAsync(cancellationToken);
            await InitializeRespawner();
        }

        //IRequestContext - replace the Bootstrapper registered non-http 'BackgroundService' registration; injected into repositories
        services.AddTransient<IRequestContext<string>>(provider =>
        {
            var correlationId = Guid.NewGuid().ToString();
            return new RequestContext<string>(correlationId, $"Test.Support.IntegrationTestBase-{correlationId}");
        });

        //build IServiceProvider for subsequent use finding/injecting services
        _services = services.BuildServiceProvider(validateScopes: true);
        _serviceScope = _services.CreateScope();
        _logger.Log(LogLevel.Information, "Test Initialized.");
    }

    /// <summary>
    /// Effective when using TestContainers
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected static async Task StartDbContainerAsync(CancellationToken cancellationToken = default)
    {
        _dbContainer = new MsSqlBuilder().Build();
        await _dbContainer.StartAsync(cancellationToken);
        _dbConnectionString = _dbContainer.GetConnectionString().Replace("master", Config.GetValue("TestSettings:DBName", "TestDB"));
        _dbConnection = new SqlConnection(_dbConnectionString); //respawner
    }

    /// <summary>
    /// https://github.com/jbogard/Respawn
    /// </summary>
    /// <returns></returns>
    private static async Task InitializeRespawner()
    {
        if (_dbConnection == null) return;

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["todo"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    protected static async Task ResetDatabaseAsync(bool respawn = false, List<Action>? seedFactories = null, List<string>? seedPaths = null,
        string seedSearchPattern = "*.sql", CancellationToken cancellationToken = default)
    {
        if (!DbContext.Database.IsInMemory() && respawn)
        {
            await _respawner.ResetAsync(_dbConnection);
        }
        await DbContext.ResetDatabaseAsync(Logger, seedFactories, seedPaths, seedSearchPattern, cancellationToken);
        await DbContext.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ClientName, cancellationToken: cancellationToken);
    }

    protected static async Task BaseClassCleanup(CancellationToken cancellationToken = default)
    {
        ServiceScope.Dispose();
        if (_dbConnection != null)
        {
            await _dbConnection.CloseAsync();
            await _dbConnection.DisposeAsync();
        }

        if (_dbContainer != null)
        {
            await _dbContainer.StopAsync(cancellationToken);
            await _dbContainer.DisposeAsync();
        }
    }
}
