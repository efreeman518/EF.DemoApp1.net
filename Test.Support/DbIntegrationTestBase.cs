using Infrastructure.Data;
using Microsoft.Data.SqlClient;
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
    private static readonly MsSqlContainer DbContainer = new MsSqlBuilder().Build();
    private static string _dbConnectionString = null!;


    //https://github.com/jbogard/Respawn
    private static Respawner _respawner = null!;
    private static DbConnection _dbConnection = null!;

    /// <summary>
    /// Configure the test class; runs once before any test class [MSTest:ClassInitialize], [BenchmarkDotNet:GlobalSetup]
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
        var dbSource = TestConfigSection.GetValue<string?>("DBSource", null);
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
        _serviceScope = _services.CreateScope();
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
        ServiceScope.Dispose();
        await _dbConnection.CloseAsync();
        await DbContainer.StopAsync();
    }

    /// <summary>
    /// https://github.com/jbogard/Respawn
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Optionally reset and reseed the database with data from the seed files and/or factories specified by the test, and/or from config
    /// </summary>
    /// <param name="respawn"></param>
    /// <param name="clearData"></param>
    /// <param name="seedFactories"></param>
    /// <param name="seedPaths"></param>
    /// <param name="seedSearchPattern"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected static async Task ResetDatabaseAsync(bool respawn = false, List<Action>? seedFactories = null,
        List<string>? seedPaths = null, string seedSearchPattern = "*.sql", CancellationToken cancellationToken = default)
    {
        //reset to blank db; clears data automatically
        if (respawn)
        {
            await _respawner.ResetAsync(_dbConnection);
        }

        //seed
        seedFactories ??= [];
        seedPaths ??= [];

        await _dbContext.SeedAsync(_logger, [.. seedPaths], seedSearchPattern, [.. seedFactories], cancellationToken);
        await _dbContext.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, cancellationToken);
    }
}
