using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Test.Endpoints;

/// <summary>
/// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests
/// </summary>
/// <typeparam name="TProgram"></typeparam>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
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
            .ConfigureServices(services =>
            {
                //Replace DbContext
                var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<TodoDbContextTrxn>));
                services.Remove(descriptor);
                services.AddDbContext<TodoDbContextTrxn>(options =>
                {
                    //use in memory db
                    options.UseInMemoryDatabase($"Test.Endpoints-{Guid.NewGuid()}");

                    //use a different sql db for test
                    //options.UseSqlServer(connectionString,
                    //    //retry strategy does not support user initiated transactions 
                    //    sqlServerOptionsAction: sqlOptions =>
                    //    {
                    //        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                    //        maxRetryDelay: TimeSpan.FromSeconds(30),
                    //        errorNumbersToAdd: null);
                    //    });

                }, ServiceLifetime.Singleton); //InMemoryDatabase - each TestServer request creates a new DbContext, so keep updates around

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;

                var db = scopedServices.GetRequiredService<TodoDbContextTrxn>();
                var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TProgram>>>();

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