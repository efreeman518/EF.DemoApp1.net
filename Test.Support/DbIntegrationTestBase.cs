using DotNet.Testcontainers.Images;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Data.Contracts;
using Respawn;
using System.Data.Common;
using Testcontainers.MsSql;

namespace Test.Support;

/// <summary>
/// Testing Domain, Application, and Infrastructure services/logic; not http endpoints
/// MSTest Constructor (if defined) runs before each test
/// </summary>
public abstract class DbIntegrationTestBase : IntegrationTestBase
{
    protected static string _testContextName = null!;
    protected static TodoDbContextBase DbContext => _dbContext;
    private static TodoDbContextBase _dbContext = null!;

    //https://testcontainers.com/guides/testing-an-aspnet-core-web-app/
    private static MsSqlContainer _dbContainer = null!;
    private static string _dbConnectionString = null!;

    //https://github.com/jbogard/Respawn
    private static Respawner _respawner = null!;
    private static DbConnection _dbConnection = null!;

    /// <summary>
    /// Runs before each test
    /// </summary>
    protected DbIntegrationTestBase()
    {

    }

    /// <summary>
    /// Configure the test class; runs once before any test class [MSTest:ClassInitialize], [BenchmarkDotNet:GlobalSetup]
    /// </summary>
    /// <param name="testContextName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected static async Task ConfigureTestInstanceAsync(string testContextName, CancellationToken cancellationToken = default)
    {
        _testContextName = $"IntegrationTest-{testContextName}";

        _dbConnectionString = TestConfigSection.GetValue("DBSource", "UseInMemoryDatabase")!;

        if (_dbConnectionString == "TestContainer")
        {
            _dbConnectionString = await StartDbContainerAsync(cancellationToken);
        }

        //Services for DI
        ConfigureServices(_testContextName);

        //modify the services collection - swap registered for test db
        string dbName = TestConfigSection.GetValue<string>("TestSettings:DBName") ?? "Test.Integration.TestDB";
        DbSupport.ConfigureServicesTestDB<TodoDbContextTrxn, TodoDbContextQuery>(ServicesCollection, _dbConnectionString, dbName);

        //rebuild service collection and grab the DbContext
        Services = ServicesCollection.BuildServiceProvider();
        _dbContext = Services.GetRequiredService<TodoDbContextTrxn>();

        bool skipAlwaysEncryptedSetup = TestConfigSection.GetValue("DisableAlwaysEncryptedSetup", true);
        await DbTestLifecycle.EnsureInitializedAsync(_dbContext, skipAlwaysEncryptedSetup, cancellationToken);

        if (!_dbContext.Database.IsInMemory())
        {
            (_dbConnection, _respawner) = await DbTestLifecycle.OpenRespawnerAsync(_dbConnectionString, cancellationToken);
        }

        //build IServiceProvider for subsequent use finding/injecting services
        ServiceScope = Services.CreateScope();
        Logger.Log(LogLevel.Information, "{TestContextName} ConfigureTestInstanceAsync (DB swap) complete.", testContextName);
    }

    /// <summary>
    /// Effective when using TestContainers
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>DB connection string from the container</returns>
    private static async Task<string> StartDbContainerAsync(CancellationToken cancellationToken = default)
    {
        string dbName = Config.GetValue("TestSettings:DBName", "TestDB");
        (_dbContainer, string dbConnectionString) = await DbTestLifecycle.StartDbContainerAsync(dbName, cancellationToken: cancellationToken);
        return dbConnectionString;
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
    /// Configure the database for the test; runs before each test [MSTest:TestInitialize], [BenchmarkDotNet:IterationSetup]
    /// </summary>
    /// <param name="respawn">based on Respawner configuration, clear all data to schema only</param>
    /// <param name="dbSnapshotName">Currently works only with existing database; not TestContainer or InMemoryDatabase; Name of the snapshot file</param>
    /// <param name="seedPaths">Paths to seed script files</param>
    /// <param name="seedFactories">Methods that will run against DbContext to create data</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected static async Task ResetDatabaseAsync(bool respawn = false, string? dbSnapshotName = null,
        List<string>? seedPaths = null, List<Action>? seedFactories = null, CancellationToken cancellationToken = default)
    {
        await DbTestLifecycle.ResetDatabaseAsync(
            DbContext,
            Logger,
            _dbConnectionString,
            _respawner,
            _dbConnection,
            respawn,
            dbSnapshotName,
            seedPaths,
            seedFactories,
            cancellationToken);
    }

    protected static async Task BaseClassCleanup()
    {
        ServiceScope.Dispose();
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
}
