using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Data.Contracts;
using Respawn;
using Respawn.Graph;
using System.Data.Common;
using Test.Support;
using Testcontainers.MsSql;

namespace Test.Endpoints;

/// <summary>
/// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests
/// </summary>
/// <typeparam name="TProgram"></typeparam>
public class CustomApiFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    //https://testcontainers.com/guides/testing-an-aspnet-core-web-app/
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

    private static ILogger<CustomApiFactory<TProgram>> _logger = null!;
    private IConfigurationSection _testConfigSection = null!;
    private TodoDbContextBase _dbContext = null!;
    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;

    public async Task StartDbContainer(CancellationToken cancellationToken = default)
    {
        await _dbContainer.StartAsync(cancellationToken);
    }

    /// <summary>
    /// https://github.com/jbogard/Respawn
    /// </summary>
    /// <returns></returns>
    public async Task InitializeRespawner(CancellationToken cancellationToken = default)
    {
        await _dbConnection.OpenAsync(cancellationToken);
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["todo"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    /// <summary>
    /// Reseed the database with data from the seed files and/or factories specified by the test, and/or from config
    /// </summary>
    /// <param name="seedFromConfig"></param>
    /// <param name="seedFactories"></param>
    /// <param name="seedPaths"></param>
    /// <param name="seedSearchPattern"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ResetDatabaseAsync(bool respawn = true, bool seedFromConfig = true, List<Action>? seedFactories = null,
        List<string>? seedPaths = null, string seedSearchPattern = "*.sql", CancellationToken cancellationToken = default)
    {
        if (respawn)
        {
            //reset to blank db
            await _respawner.ResetAsync(_dbConnection);
        }

        //seed
        seedFactories ??= [];
        seedPaths ??= [];
        if (seedFromConfig)
        {
            if (_testConfigSection.GetValue("SeedEntityData", false))
            {
                seedFactories.Add(() => _dbContext.SeedEntityData());
            }
            seedPaths.AddRange(_testConfigSection.GetSection("SeedFiles:Paths").Get<string[]>() ?? []);
        }
        await _dbContext.SeedAsync(_logger, [.. seedPaths], seedSearchPattern, [.. seedFactories], cancellationToken);
        await _dbContext.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, cancellationToken);
    }

    public async Task StopDbContainer(CancellationToken cancellationToken = default)
    {
        await _dbContainer.StopAsync(cancellationToken);
        await _dbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //The SUT's services (repos, DbContext, etc) are registered in its Startup.ConfigureServices method.
        //The test app's builder.ConfigureTestServices callback is executed after the app's Startup.ConfigureServices code is executed.
        //To use a different service for the tests, the app's service must be replaced here in builder.ConfigureServices
        //This methed enables replacing the endpoint project's registered services with test-purposed services

        IConfiguration config = null!;

        string env = builder.GetSetting("ASPNETCORE_ENVIRONMENT") ?? "Development"; // Utility.GetConfiguration().GetValue<string>("Environment", "Development")!;
        builder
            .UseEnvironment(env)
            .ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                //override api settings with test settings
                configuration.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings-test.json"));
                config = configuration.Build();//get config for use here
            })
            .ConfigureTestServices(services =>
            {
                //remove unneeded services
                services.RemoveAll<IHostedService>();

                var sp = services.BuildServiceProvider();
                var logger = sp.GetRequiredService<ILogger<CustomApiFactory<TProgram>>>();
                _testConfigSection = config.GetSection("TestSettings");

                //Database
                var dbConnectionString = _dbContainer.GetConnectionString().Replace("master", _testConfigSection.GetValue("DBName", "TestDB"));
                _dbConnection = new SqlConnection(dbConnectionString);
                var dbSource = _testConfigSection.GetValue<string?>("DBSource", null);
                _dbContext = DbSupport.ConfigureTestDB<TodoDbContextTrxn>(logger, services, dbSource, dbConnectionString);
                _logger = services.BuildServiceProvider().GetRequiredService<ILogger<CustomApiFactory<TProgram>>>();
            });
    }
}

public class CustomHttpHandler : DelegatingHandler
{
    public CustomHttpHandler()
    {
        //ignore cert validation
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (HttpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;
            }
        };

        InnerHandler = handler;
    }
}