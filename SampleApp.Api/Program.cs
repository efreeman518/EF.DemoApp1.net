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
    public static async Task Main(string[] args)
    {
        //defaults: config gets 'ASPNETCORE_*' env vars, appsettings.json and appsettings.{Environment}.json
        var builder = WebApplication.CreateBuilder(args);

        //configuration
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddUserSecrets<Program>();
        }

        //logging
        builder.Logging.ClearProviders(); //console default
        builder.Logging.AddApplicationInsights();
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddDebug();
            builder.Logging.AddConsole();
        }

        builder.RegisterApiServices();

        var config = builder.Configuration;
        builder.Services
            .RegisterDomainServices(config)
            .RegisterApplicationServices(config)
            .RegisterInfrastructureServices(config)
            .RegisterRuntimeServices(config);

        var app = builder.Build().ConfigurePipeline();
        await app.RunStartupTasks();
        await app.RunAsync();

    }
}

//namespace SampleApp.Api;

//public class Program
//{
//    protected Program() { }

//    public static async Task Main(string[] args)
//    {
//        IHost host = CreateHostBuilder(args).Build();
//        await host.RunStartupTasks();
//        await host.RunAsync();
//    }

//    public static IHostBuilder CreateHostBuilder(string[] args) =>
//        Host.CreateDefaultBuilder(args)
//            .ConfigureWebHostDefaults(webBuilder =>
//            {
//                webBuilder.ConfigureLogging(ConfigureLogger);
//                webBuilder.UseStartup<Startup>();
//            });

//    static void ConfigureLogger(WebHostBuilderContext hostingContext, ILoggingBuilder logging)
//    {
//        logging.AddApplicationInsights();

//        if (hostingContext.HostingEnvironment.IsDevelopment())
//        {
//            logging.AddDebug();
//            logging.AddConsole();
//        }
//    }
//}
