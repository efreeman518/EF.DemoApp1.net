using Microsoft.ApplicationInsights.Channel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SampleApp.Bootstrapper;

namespace SampleApp.Api;

/// <summary>
/// Program class must be explicitly defined for WebApplicationFactory in Test.Endpoints
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
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
        builder.Logging.ClearProviders(); 
        builder.Logging.AddApplicationInsights();
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddDebug();
            builder.Logging.AddConsole();
        }

        var config = builder.Configuration;
        builder.Services
            //api services - controllers, versioning, swagger, telemetry
            .RegisterApiServices(config)
            //infrastructure - caches, DbContexts, repos, external service proxies
            .RegisterInfrastructureServices(config)
            //domain services
            .RegisterDomainServices(config)
            //app servives
            .RegisterApplicationServices(config)
            //background services, health checks, startup tasks
            .RegisterRuntimeServices(config);

        var app = builder.Build().ConfigurePipeline();
        await app.RunStartupTasks();
        await app.RunAsync();
    }
}
