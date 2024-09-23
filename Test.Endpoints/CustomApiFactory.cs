using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Test.Support;

namespace Test.Endpoints;

/// <summary>
/// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests
/// </summary>
/// <typeparam name="TProgram"></typeparam>
public class CustomApiFactory<TProgram>(string? dbConnectionString = null) : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //The SUT's services (repos, DbContext, etc) are registered in its Startup.ConfigureServices method.
        //The test app's builder.ConfigureTestServices callback is executed after the app's Startup.ConfigureServices code is executed.
        //To use a different service for the tests, the app's service must be replaced here in builder.ConfigureServices
        //This methed enables replacing the endpoint project's registered services with test-purposed services

        IConfiguration config = null!;

        string env = builder.GetSetting("ASPNETCORE_ENVIRONMENT") ?? "Development";

        //var memorySettings = new Dictionary<string, string?>();
        //if (dbConnectionString != null)
        //{
        //    memorySettings.Add("ConnectionStrings:TodoDbContextTrxn", dbConnectionString);
        //    memorySettings.Add("ConnectionStrings:TodoDbContextQuery", dbConnectionString);
        //}

        builder
            .UseEnvironment(env)
            .ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                //configuration.AddInMemoryCollection(memorySettings);
                //override api settings with test settings
                configuration.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings-test.json"));
                config = configuration.Build();//get config for use here
            })
            .ConfigureServices(services =>
            {
                //remove unneeded services
                services.RemoveAll<IHostedService>();

                //swap the api database to the test database in Services collection
                string dbName = config.GetValue<string>("TestSettings:DBName") ?? "Test.Endpoints.TestDB";
                DbSupport.ConfigureServicesTestDB<TodoDbContextTrxn, TodoDbContextQuery>(services, dbConnectionString, dbName);
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