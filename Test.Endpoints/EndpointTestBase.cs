using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Package.Infrastructure.Data.Contracts;
using Respawn;
using Respawn.Graph;
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

    //protected static readonly IAppCache _appcache = new CachingService();

    //needed for tests to call the in-memory api; authenticated endpoints will need a bearer token
    //protected static HttpClient HttpClientApi = null!;

    protected static async Task<HttpClient> GetHttpClient()
    {
        var scopes = Config.GetSection("SampleApiRestClientSettings:Scopes").Get<string[]>();

        //handler takes care of auth
        var handler = new SampleRestApiAuthMessageHandler(scopes!);
        //var tokenResourceId = Config.GetValue<string?>("SampleApiRestClientSettings:ResourceId");
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

        //if we are going to have a dbContext for resetting data, we need to make sure its created now in order for respawner to open the connection
        //Environment.SetEnvironmentVariable("AKVCMKURL", "");
        //db.Database.Migrate(); //needs AKVCMKURL env var set
        //cannot run parallel tests - this throws
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken); //does not use migrations; uses DbContext to create tables

        if (!_dbContext.Database.IsInMemory())
        {
            //supports respawner
            _dbConnection = new SqlConnection(_dbConnectionString);
            await _dbConnection.OpenAsync(cancellationToken);
            await InitializeRespawner();
        }

        //create the http client for the test to hit endpoints; this creates the WAF, in memory api, replaces services, so need the db connection string
        //HttpClientApi = GetHttpClient(_dbConnectionString);
    }

    /// <summary>
    /// Effective when using TestContainers
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected static async Task StartDbContainerAsync(CancellationToken cancellationToken = default)
    {
        //create image from docker file - https://dotnet.testcontainers.org/api/create_docker_image/

        _dbContainer = new MsSqlBuilder().Build();
        await _dbContainer.StartAsync(cancellationToken);
        _dbConnectionString = _dbContainer.GetConnectionString().Replace("master", Config.GetValue("TestSettings:DBName", "TestDB"));
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
        string[] notAllowedTypes = ["UseInMemoryDatabase", "TestContainer"];
        if (notAllowedTypes.Contains(TestConfigSection.GetValue("DBSource", "UseInMemoryDatabase")) || _dbConnectionString == null)
        {
            throw new InvalidOperationException("Snapshots are only allowed for existing SQL DBs");
        }
        await DbSupport.CreateDbSnapshot(snapshotName, _dbConnection.Database, _dbConnectionString, cancellationToken);
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
        string[] notAllowedTypes = ["UseInMemoryDatabase", "TestContainer"];
        if (notAllowedTypes.Contains(TestConfigSection.GetValue("DBSource", "UseInMemoryDatabase")) || _dbConnectionString == null)
        {
            throw new InvalidOperationException("Snapshots are only allowed for existing SQL DBs");
        }
        await DbSupport.DeleteDbSnapshot(snapshotName, _dbConnectionString, cancellationToken);
    }

    /// <summary>
    /// https://github.com/jbogard/Respawn
    /// Effective when using real DB (not in-memory)
    /// </summary>
    /// <returns></returns>
    public static async Task InitializeRespawner()
    {
        if (_dbConnection == null) return;

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["todo"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
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
        if (!DbContext.Database.IsInMemory())
        {
            if (respawn) await _respawner.ResetAsync(_dbConnection);

            //Currently works only with existing database; not TestContainer or InMemoryDatabase
            if (!string.IsNullOrEmpty(dbSnapshotName))
            {
                var snapshotUtility = new SqlDatabaseSnapshotUtility(_dbConnectionString);
                var dbName = _dbConnection.Database;
                await snapshotUtility.RestoreSnapshotAsync(dbName, dbSnapshotName, cancellationToken);
            }
        }
        await DbContext.SeedDatabaseAsync(NullLogger.Instance, seedPaths, seedFactories, cancellationToken);
        await DbContext.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, _testContextName, cancellationToken: cancellationToken);
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
        return new TodoDbContextTrxn(optionsBuilder.Options);
    }

}
