using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Respawn;
using Respawn.Graph;
using System.Data.Common;
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

    private DbConnection _dbConnection = null!;
    private string _dbConnectionString = null!;
    private Respawner _respawner = null!;

    public async Task StartDbContainer()
    {
        await _dbContainer.StartAsync();
    }

    /// <summary>
    /// https://github.com/jbogard/Respawn
    /// </summary>
    /// <returns></returns>
    public async Task InitializeRespawner()
    {
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["todo"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    public async Task StopDbContainer()
    {
        await _dbContainer.StopAsync();
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
                var testConfigSection = config.GetSection("TestSettings");

                //Database
                _dbConnectionString = _dbContainer.GetConnectionString().Replace("master", testConfigSection.GetValue("DBName", "TestDB"));
                _dbConnection = new SqlConnection(_dbConnectionString);
                Support.Utility.ConfigureTestDB(logger, services, testConfigSection, _dbConnectionString);

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