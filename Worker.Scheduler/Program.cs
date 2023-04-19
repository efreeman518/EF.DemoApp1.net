using SampleApp.Bootstrapper;

const string SERVICE_NAME = "Worker.Scheduler";

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

    var host = Host.CreateDefaultBuilder(args)
        .ConfigureLogging((hostContext, logging) =>
        {
            loggerStartup.LogInformation("{ServiceName} - Configure logging.", SERVICE_NAME);

            logging.ClearProviders();
            logging.AddApplicationInsights();

            if (hostContext.HostingEnvironment.IsDevelopment())
            {
                logging.AddConsole();
                logging.AddDebug();
            }
        })
        .ConfigureServices((hostContext, services) =>
        {
            loggerStartup.LogInformation("{ServiceName} - Register services.", SERVICE_NAME);

            var config = hostContext.Configuration;

            services
                //app insights telemetry logging for non-http service
                .AddApplicationInsightsTelemetryWorkerService(config)
                //infrastructure - caches, DbContexts, repos, external service proxies, startup tasks
                .RegisterInfrastructureServices(config)
                //domain services
                .RegisterDomainServices(config)
                //app servives
                .RegisterApplicationServices(config)
                //background services
                .RegisterBackgroundServices(config);
        })
        .Build();

    await host.RunStartupTasks();
    await host.RunAsync();

}
catch (Exception ex)
{
    loggerStartup.LogCritical(ex, "{ServiceName} - Host terminated unexpectedly.", SERVICE_NAME);
}
finally
{
    loggerStartup.LogInformation("{ServiceName} - Ending application.", SERVICE_NAME);
}
