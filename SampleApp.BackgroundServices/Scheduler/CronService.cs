using Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundServices;

namespace SampleApp.BackgroundServices.Scheduler;

public class CronService(IServiceScopeFactory serviceScopeFactory, ILogger<CronService> logger,
    IOptions<CronJobBackgroundServiceSettings<CustomCronJob>> settings) : CronBackgroundService<CustomCronJob>(logger, settings)
{

    /// <summary>
    /// Uncaught Exception will stop the service
    /// https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling
    /// </summary>
    /// <param name="TraceId"></param>
    /// <param name="cronJob"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task RunOnScheduleAsync(string TraceId, CustomCronJob cronJob, CancellationToken stoppingToken = default)
    {
        logger.Log(LogLevel.Information, "{CronJob} - Start scheduled background work {Runtime}", cronJob.JobName, DateTime.Now);

        try
        {
            _ = cronJob.SomeUrl;
            _ = cronJob.SomeTopicOrQueue;

            //create scope if needed for scoped services
            using var scope = serviceScopeFactory.CreateScope();

            //do something - get a scoped service and call a method
            _ = scope.ServiceProvider.GetRequiredService<ITodoService>();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{CronJob} - Failed during scheduled background work.", cronJob.JobName);
        }

        logger.Log(LogLevel.Information, null, "{CronJob} - Complete scheduled background work {Runtime} ", cronJob.JobName, DateTime.Now);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        //graceful clean-up actions
        await Task.FromResult<object>(new object());
    }
}
