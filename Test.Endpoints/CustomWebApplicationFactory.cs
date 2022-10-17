using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;

namespace Test.Endpoints;

/// <summary>
/// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0
/// </summary>
/// <typeparam name="TStartup"></typeparam>
public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
    where TStartup : class
{
    //Set the ASPNETCORE_ENVIRONMENT environment variable (for example, Staging, Production, or other custom value, such as Testing).
    //Override CreateHostBuilder in the test app to read environment variables prefixed with ASPNETCORE
    protected override IHostBuilder CreateHostBuilder() =>
        base.CreateHostBuilder()!
            .ConfigureHostConfiguration(config => config.AddEnvironmentVariables("ASPNETCORE"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //The SUT's services (repos, DbContext, etc) are registered in its Startup.ConfigureServices method.
        //The test app's builder.ConfigureServices callback is executed after the app's Startup.ConfigureServices code is executed.
        //To use a different service for the tests, the app's service must be replaced here in builder.ConfigureServices
        //This methed enables replacing the endpoint project's registered services with test-purposed services

        string env = Utility.GetConfiguration().GetValue<string>("Environment", "development");
        builder
            .UseEnvironment(env)
            .ConfigureServices(services =>
            {
                //Replace DbContext
                var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<TodoContext>));
                services.Remove(descriptor);
                services.AddDbContext<TodoContext>(options =>
                {
                    //use in memory db
                    options.UseInMemoryDatabase($"InMemoryDbForTesting-{Guid.NewGuid()}");

                    //////use a different test sql db
                    ////options.UseSqlServer(connectionString,
                    ////    //retry strategy does not support user initiated transactions 
                    ////    sqlServerOptionsAction: sqlOptions =>
                    ////    {
                    ////        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                    ////        maxRetryDelay: TimeSpan.FromSeconds(30),
                    ////        errorNumbersToAdd: null);
                    ////    });

                }, ServiceLifetime.Singleton); //InMemoryDatabase - each TestServer request creates a new DbContext, so keep updates around

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;

                var db = scopedServices.GetRequiredService<TodoContext>();
                var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                db.Database.EnsureCreated();

                //Seed DbContext replacement
                try
                {
                    Utility.SeedInMemoryDB(db);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the database with test messages. Error: {Message}", ex.Message);
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