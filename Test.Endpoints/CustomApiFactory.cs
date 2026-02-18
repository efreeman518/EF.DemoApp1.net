using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Package.Infrastructure.BackgroundServices.Cron;
using Package.Infrastructure.BackgroundServices.Work;
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
                configuration.AddJsonFile(Utility.ResolveJsonConfigPath("appsettings-test.json"));
                config = configuration.Build();//get config for use here
            })
            .ConfigureTestServices(services =>
            {
                if (config.GetValue("TestSettings:DisableHostedServices", true))
                {
                    RemoveKnownHostedServices(services);
                }

                //swap the api database to the test database in Services collection
                string dbName = config.GetValue<string>("TestSettings:DBName") ?? "Test.Endpoints.TestDB";
                DbSupport.ConfigureServicesTestDB<TodoDbContextTrxn, TodoDbContextQuery>(services, dbConnectionString, dbName);
            });
    }

    private static void RemoveKnownHostedServices(IServiceCollection services)
    {
        var descriptorsToRemove = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType != null
                && IsKnownProblematicHostedService(descriptor.ImplementationType))
            .ToList();

        foreach (var descriptor in descriptorsToRemove)
        {
            services.Remove(descriptor);
        }
    }

    private static bool IsKnownProblematicHostedService(Type implementationType)
    {
        if (implementationType == typeof(ChannelBackgroundTaskService))
        {
            return true;
        }

        return implementationType.IsGenericType
            && implementationType.GetGenericTypeDefinition() == typeof(CronBackgroundService<>);
    }
}

public class CustomHttpHandler : DelegatingHandler
{
    public CustomHttpHandler()
    {
        // Use default server certificate validation for security
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual
            // Removed insecure ServerCertificateCustomValidationCallback override
        };

        InnerHandler = handler;
    }
}