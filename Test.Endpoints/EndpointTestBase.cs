using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Package.Infrastructure.Data.Contracts;
using Respawn;
using System.Data.Common;
using Test.Support;
using Testcontainers.MsSql;

namespace Test.Endpoints;

/// <summary>
/// Testing http endpoints (MVC controllers, razor pages)
/// Get the DB connection string & DbContext so data can be reset between tests 
/// </summary>
public abstract class EndpointTestBase
{
    private static string _testContextName = null!;
    private static MsSqlContainer _dbContainer = null!;
    private static string _dbConnectionString = null!;
    private static DbConnection _dbConnection = null!;
    private static Respawner _respawner = null!;
    private static TodoDbContextBase _dbContext = null!;

    protected static TodoDbContextBase DbContext => _dbContext;
    protected readonly static IConfigurationRoot Config = Utility.BuildConfiguration("appsettings-test.json").AddUserSecrets<Program>().Build();
    protected readonly static IConfigurationSection TestConfigSection = Config.GetSection("TestSettings");

    protected static async Task<HttpClient> GetHttpClient()
    {
        //net10 can expose the service on the network - this could be used for UI testing 
        //UseKestrel();
        //StartServer();

        var scopes = Config.GetSection("SampleApiRestClientSettings:Scopes").Get<string[]>();

        //handler takes care of auth
        var handler = new SampleRestApiAuthMessageHandler(scopes!);
        var httpClient = await ApiFactoryManager.GetClientAsync<Program>(_testContextName, dbConnectionString: _dbConnectionString, handlers: handler);
        return httpClient;
    }

    public static async Task ConfigureTestInstanceAsync(string testContextName, CancellationToken cancellationToken = default)
    {
        _testContextName = $"EndpointTest-{testContextName}";

        _dbConnectionString = TestConfigSection.GetValue("DBSource", "UseInMemoryDatabase")!;
        if (_dbConnectionString == "TestContainer")
        {
            await StartDbContainerAsync(cancellationToken); //sets _dbConnectionString
        }
        _dbContext = NewTodoDbContextTrxn(_dbConnectionString);

        bool skipAlwaysEncryptedSetup = TestConfigSection.GetValue("DisableAlwaysEncryptedSetup", true);
        await DbTestLifecycle.EnsureInitializedAsync(_dbContext, skipAlwaysEncryptedSetup, cancellationToken);

        if (!_dbContext.Database.IsInMemory())
        {
            (_dbConnection, _respawner) = await DbTestLifecycle.OpenRespawnerAsync(_dbConnectionString, cancellationToken);
        }
    }

    /// <summary>
    /// Effective when using TestContainers
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected static async Task StartDbContainerAsync(CancellationToken cancellationToken = default)
    {
        string dbName = Config.GetValue("TestSettings:DBName", "TestDB");
        (_dbContainer, _dbConnectionString) = await DbTestLifecycle.StartDbContainerAsync(dbName, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Currently works only with existing database; not TestContainer or InMemoryDatabase
    /// Create a snapshot of the database; run before each test [MSTest:TestInitialize], [BenchmarkDotNet:IterationSetup]
    /// Then at the beginning of appropriate tests, restore the database to the snapshot
    /// </summary>
    /// <param name="snapshotName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected static async Task CreateDbSnapshot(string snapshotName, CancellationToken cancellationToken = default)
    {
        await DbTestLifecycle.CreateDbSnapshotAsync(
            TestConfigSection.GetValue("DBSource", "UseInMemoryDatabase"),
            _dbConnectionString,
            _dbConnection,
            snapshotName,
            cancellationToken);
    }

    /// <summary>
    /// Currently works only with existing database; not TestContainer or InMemoryDatabase
    /// </summary>
    /// <param name="snapshotName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected static async Task DeleteDbSnapshot(string snapshotName, CancellationToken cancellationToken = default)
    {
        await DbTestLifecycle.DeleteDbSnapshotAsync(
            TestConfigSection.GetValue("DBSource", "UseInMemoryDatabase"),
            _dbConnectionString,
            snapshotName,
            cancellationToken);
    }

    /// <summary>
    /// https://github.com/jbogard/Respawn
    /// Effective when using real DB (not in-memory)
    /// </summary>
    /// <returns></returns>
    public static async Task InitializeRespawner()
    {
        if (_dbConnectionString == null || _dbConnection != null)
        {
            return;
        }

        (_dbConnection, _respawner) = await DbTestLifecycle.OpenRespawnerAsync(_dbConnectionString);
    }

    /// <summary>
    /// Configure the database for the test; runs before each test [MSTest:TestInitialize], [BenchmarkDotNet:IterationSetup]
    /// </summary>
    /// <param name="respawn">based on Respawner configuration, clear all data to schema only</param>
    /// <param name="dbSnapshotName">Currently works only with existing database; not TestContainer or InMemoryDatabase; Name of the snapshot file</param>
    /// <param name="seedPaths">Paths to seed script files</param>
    /// <param name="seedSearchPattern">Pattern for seed script files</param>
    /// <param name="seedFactories">Methods that will run against DbContext to create data</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected static async Task ResetDatabaseAsync(bool respawn = false, string? dbSnapshotName = null,
        List<string>? seedPaths = null, List<Action>? seedFactories = null, CancellationToken cancellationToken = default)
    {
        await DbTestLifecycle.ResetDatabaseAsync(
            DbContext,
            NullLogger.Instance,
            _dbConnectionString,
            _respawner,
            _dbConnection,
            respawn,
            dbSnapshotName,
            seedPaths,
            seedFactories,
            cancellationToken);
    }

    public static async Task BaseClassCleanup()
    {
        ApiFactoryManager.Cleanup<Program>(_testContextName);

        if (_dbConnection != null)
        {
            await _dbConnection.CloseAsync();
            await _dbConnection.DisposeAsync();
        }

        if (_dbContainer != null)
        {
            await _dbContainer.DisposeAsync();
        }
    }

    private static TodoDbContextTrxn NewTodoDbContextTrxn(string dbSource, string? dbName = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TodoDbContextTrxn>();
        if (dbSource == "UseInMemoryDatabase")
        {
            optionsBuilder.UseInMemoryDatabase(dbName ?? "InMemoryDatabase");
        }
        else
        {
            optionsBuilder.UseSqlServer(dbSource);
        }
        return new TodoDbContextTrxn(optionsBuilder.Options) { AuditId = "EndpointTests" };
    }

}
