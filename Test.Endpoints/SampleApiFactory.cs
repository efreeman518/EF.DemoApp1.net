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
                configuration.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(),"appsettings-test.json"));
                config = configuration.Build();//get config for use here
            })
            .ConfigureTestServices(services =>
            {
                var dbSource = config.GetValue<string?>("DBSource", null);

                //if dbSource is null, use the api defined DbContext/DB
                if (!string.IsNullOrEmpty(dbSource))
                {
                    services.RemoveAll(typeof(DbContextOptions<TodoDbContextTrxn>));
                    services.RemoveAll(typeof(TodoDbContextTrxn));
                    services.AddDbContext<TodoDbContextTrxn>(options =>
                    {

                        if (dbSource == "TestContainer")
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
                        }
                        else if (dbSource == "UseInMemoryDatabase")
                        {
                            options.UseInMemoryDatabase($"Test.Endpoints-{Guid.NewGuid()}");
                        }
                        else
                        {
                            options.UseSqlServer(dbSource,
                                //retry strategy does not support user initiated transactions 
                                sqlServerOptionsAction: sqlOptions =>
                                {
                                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                                    maxRetryDelay: TimeSpan.FromSeconds(30),
                                    errorNumbersToAdd: null);
                                });
                        }

                    }, ServiceLifetime.Singleton); //enables injection; otherwise AddDbContext is scoped and there is no scope
                }
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;

                var db = scopedServices.GetRequiredService<TodoDbContextTrxn>();
                var logger = scopedServices.GetRequiredService<ILogger<SampleApiFactory<TProgram>>>();


                db.Database.EnsureCreated(); //does not use migrations

                //Environment.SetEnvironmentVariable("AKVCMKURL", "");
                //db.Database.Migrate(); //needs AKVCMKURL env var set

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