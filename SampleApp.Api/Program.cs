using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SampleApp.Bootstrapper;

namespace SampleApp.Api;

/// <summary>
/// Program class must be explicitly defined for WebApplicationFactory in Test.Endpoints
/// </summary>
public class Program
{
    private const string SERVICE_NAME = "SampleApi";

    protected Program()
    {
    }

    public static async Task Main(string[] args)
    {
        //logging for initialization
        ILogger<Program> loggerStartup;
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
            builder.AddApplicationInsights();
        });
        loggerStartup = loggerFactory.CreateLogger<Program>();

        try
        {
            loggerStartup.LogInformation("{ServiceName} - Startup.", SERVICE_NAME);

            //CreateBuilder defaults:
            //- config gets 'ASPNETCORE_*' env vars, appsettings.json and appsettings.{Environment}.json
            //- logging gets Console
            var builder = WebApplication.CreateBuilder(args);

            //configuration
            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddUserSecrets<Program>();
            }

            //logging
            loggerStartup.LogInformation("{ServiceName} - Configure logging.", SERVICE_NAME);
            builder.Logging.ClearProviders();
            builder.Logging.AddApplicationInsights();
            if (builder.Environment.IsDevelopment())
            {
                builder.Logging.AddConsole();
                builder.Logging.AddDebug();
            }

            loggerStartup.LogInformation("{ServiceName} - Register services.", SERVICE_NAME);
            var config = builder.Configuration;
            builder.Services
                //api services - controllers, versioning, health checks, swagger, telemetry
                .RegisterApiServices(config)
                //infrastructure - caches, DbContexts, repos, external service proxies, startup tasks
                .RegisterInfrastructureServices(config)
                //domain services
                .RegisterDomainServices(config)
                //app servives
                .RegisterApplicationServices(config)
                //background services
                .RegisterBackgroundServices(config);

            var app = builder.Build().ConfigurePipeline();
            await app.RunStartupTasks();
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            loggerStartup.LogCritical(ex, "{ServiceName} - Host terminated unexpectedly.", SERVICE_NAME);
        }
        finally
        {
            loggerStartup.LogInformation("{ServiceName} - Ending application.", SERVICE_NAME);
        }
    }
}
