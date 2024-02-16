using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;

namespace Test.Endpoints;

/// <summary>
/// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests
/// </summary>
/// <typeparam name="TProgram"></typeparam>
public class SampleApiFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    //https://testcontainers.com/guides/testing-an-aspnet-core-web-app/
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

    public async Task StartDbContainer()
    {
        await _dbContainer.StartAsync();
    }
    public async Task StopDbContainer()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //The SUT's services (repos, DbContext, etc) are registered in its Startup.ConfigureServices method.
        //The test app's builder.ConfigureServices callback is executed after the app's Startup.ConfigureServices code is executed.
        //To use a different service for the tests, the app's service must be replaced here in builder.ConfigureServices
        //This methed enables replacing the endpoint project's registered services with test-purposed services

        builder.ConfigureAppConfiguration(config =>
        {
            // Add custom configuration sources
            config.AddJsonFile("appsettings.json", optional: false);
            config.AddEnvironmentVariables(prefix: "ASPNETCORE_");
        });

        string env = Utility.GetConfiguration().GetValue<string>("Environment", "Development")!;
        builder
            .UseEnvironment(env)
            .ConfigureTestServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<TodoDbContextTrxn>));
                services.AddDbContext<TodoDbContextTrxn>(options =>
                {
                    //use sql server test container
                    options.UseSqlServer(_dbContainer.GetConnectionString(),
                        //retry strategy does not support user initiated transactions 
                        sqlServerOptionsAction: sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        });

                    //use in memory db
                    //options.UseInMemoryDatabase($"Test.Endpoints-{Guid.NewGuid()}");

                    //use a different sql db for test
                    //options.UseSqlServer(connectionString,
                    //    //retry strategy does not support user initiated transactions 
                    //    sqlServerOptionsAction: sqlOptions =>
                    //    {
                    //        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                    //        maxRetryDelay: TimeSpan.FromSeconds(30),
                    //        errorNumbersToAdd: null);
                    //    });

                }, ServiceLifetime.Singleton); //enables injection; otherwise AddDbContext is scoped and there is no scope

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;

                var db = scopedServices.GetRequiredService<TodoDbContextTrxn>();
                var logger = scopedServices.GetRequiredService<ILogger<SampleApiFactory<TProgram>>>();

                db.Database.EnsureCreated();

                //Seed DbContext replacement
                try
                {
                    Support.Utility.SeedDefaultEntityData(db);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the database with test data. Error: {Message}", ex.Message);
                }
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