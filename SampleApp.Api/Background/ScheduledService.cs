using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundService;
using System.Threading;

namespace SampleApp.Api.Background;


public class CustomCronService : CronServiceSettings
{
    public string? SomeUrl { get; set; }
    public string? SomeTopicOrQueue { get; set; }
}

public class ScheduledServiceSettings : ScheduledBackgroundServiceSettings<CustomCronService>
{
    public const string ConfigSectionName = "ScheduledServiceSettings";
}

public class ScheduledService : ScheduledBackgroundService<CustomCronService>
{
    public ScheduledService(IServiceScopeFactory serviceScopeFactory, ILogger<ScheduledService> logger, IOptions<ScheduledBackgroundServiceSettings<CustomCronService>> settings)
        : base(serviceScopeFactory, settings, logger)
    {
    }

    /// <summary>
    /// Uncaught Exception will stop the service
    /// https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="TraceId"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteInScopeAsync(IServiceProvider serviceProvider, string TraceId, CustomCronService cronService, CancellationToken stoppingToken = default)
    {
        Logger?.Log(LogLevel.Debug, "{ServiceName} - Start scheduled background work {Runtime}", cronService.ServiceName, DateTime.Now);

        try
        {
            _ = cronService.SomeUrl;
            _ = cronService.SomeTopicOrQueue;

            //do something - based on cronService properties
            //message to topic/queue
            //or
            //get a scoped service and call a method
            //_ = serviceProvider.GetRequiredService<ITodoService>();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "{ServiceName} - Failed during scheduled background work.", cronService.ServiceName);
        }

        Logger?.Log(LogLevel.Debug, null, "{ServiceName} - Complete scheduled background work {Runtime} ", cronService.ServiceName, DateTime.Now);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        //graceful clean-up actions
        await Task.FromResult<object>(new object());
    }
}
