using Infrastructure.Data;
using LazyCache;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Data.Contracts;
using Package.Infrastructure.Http.Tokens;
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
[TestClass]
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

    protected readonly IAppCache _appcache;
    protected readonly IOAuth2TokenProvider _tokenProvider;

    //needed for tests to call the in-memory api
    protected HttpClient ApiHttpClient = null!;

    protected EndpointTestBase()
    {
        _appcache = new CachingService();
        var options = new AzureADOptions
        {
            TenantId = Config.GetValue<Guid>("Auth:TenantId")!,
            ClientId = Config.GetValue<string>("Auth:ClientId")!,
            ClientSecret = Config.GetValue<string>("Auth:ClientSecret")!
        };
        _ = options.GetHashCode();

        //no auth - not currently used
        //_tokenProvider = new AzureAdTokenProvider(Options.Create(options), _appcache);
        _tokenProvider = null!;

        ApiHttpClient = ApiFactoryManager.GetClient<Program>(_testContextName);

        //Authentication
        //await ApplyBearerAuthHeader(ApiHttpClient);
    }

    //no auth - not currently used
    protected async Task ApplyBearerAuthHeader(HttpClient client)
    {
        //scopes = new string[] { _azureAdOptions.Resource + ".default" };
        var scopes = Config.GetValue("Auth:Scopes", new string[] { string.Empty })!;
        var token = await _tokenProvider.GetAccessTokenAsync(scopes);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task ConfigureTestInstanceAsync(string testContextName, CancellationToken cancellationToken = default)
    {
        _testContextName = $"EndpointTest-{testContextName}";
        var dbSource = TestConfigSection.GetValue<string?>("DBSource", null);

        if (dbSource == "TestContainer")
        {
            dbSource = _dbConnectionString;
        }
        //create the DbContext so that tests can arrange data prior to running
        _dbContext = NewTodoDbContextTrxn(dbSource!);

        //if we are going to have a dbContext for resetting data, we need to make sure its created now in order for respawner to open the connection
        //Environment.SetEnvironmentVariable("AKVCMKURL", "");
        //db.Database.Migrate(); //needs AKVCMKURL env var set
        _dbContext.Database.EnsureCreated(); //does not use migrations; uses DbContext to create tables

        if (!_dbContext.Database.IsInMemory())
        {
            await _dbConnection.OpenAsync(cancellationToken);
            await InitializeRespawner();
        }
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

    protected static async Task ResetDatabaseAsync(bool respawn = false, List<Action>? seedFactories = null, List<string>? seedPaths = null,
        string seedSearchPattern = "*.sql", CancellationToken cancellationToken = default)
    {
        if (!DbContext.Database.IsInMemory() && respawn)
        {
            await _respawner.ResetAsync(_dbConnection);
        }
        await DbContext.ResetDatabaseAsync(NullLogger.Instance, seedFactories, seedPaths, seedSearchPattern, cancellationToken);
        await DbContext.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, _testContextName, cancellationToken: cancellationToken);
    }

    public static async Task BaseClassCleanup(CancellationToken cancellationToken = default)
    {
        ApiFactoryManager.Cleanup<Program>(_testContextName);

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
