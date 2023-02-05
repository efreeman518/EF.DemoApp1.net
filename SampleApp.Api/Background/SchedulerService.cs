using Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundService;
using System.Threading;

namespace SampleApp.Api.Background;

public class SchedulerService : ScheduledBackgroundService<CustomCronService>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SchedulerService(IServiceScopeFactory serviceScopeFactory, ILogger<SchedulerService> logger, IOptions<ScheduledBackgroundServiceSettings<CustomCronService>> settings)
        : base(logger, settings)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// Uncaught Exception will stop the service
    /// https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="TraceId"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteOnScheduleAsync(string TraceId, CustomCronService cronService, CancellationToken stoppingToken = default)
    {
        Logger.Log(LogLevel.Debug, "{ServiceName} - Start scheduled background work {Runtime}", cronService.ServiceName, DateTime.Now);

        try
        {
            _ = cronService.SomeUrl;
            _ = cronService.SomeTopicOrQueue;

            //create scope if needed for scoped services
            using var scope = _serviceScopeFactory.CreateScope();

            //do something - based on cronService properties
            //message to topic/queue
            //or
            //get a scoped service and call a method
            _ = scope.ServiceProvider.GetRequiredService<ITodoService>();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{ServiceName} - Failed during scheduled background work.", cronService.ServiceName);
        }

        Logger.Log(LogLevel.Debug, null, "{ServiceName} - Complete scheduled background work {Runtime} ", cronService.ServiceName, DateTime.Now);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        //graceful clean-up actions
        await Task.FromResult<object>(new object());
    }
}
