using Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundServices.Cron;

namespace SampleApp.BackgroundServices.Scheduler;

public class CustomCronJobHandler(IServiceScopeFactory serviceScopeFactory, ILogger<CustomCronJobHandler> logger)
    : ICronJobHandler<CustomCronJob>
{
    /// <summary>
    /// Execute the custom cron job logic
    /// </summary>
    /// <param name="traceId">Trace ID for correlation</param>
    /// <param name="cronJob">The custom cron job configuration</param>
    /// <param name="stoppingToken">Cancellation token</param>
    public async Task ExecuteAsync(string traceId, CustomCronJob cronJob, CancellationToken stoppingToken = default)
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
            throw; // Re-throw to let the base service handle it
        }

        logger.Log(LogLevel.Information, "{CronJob} - Complete scheduled background work {Runtime}", cronJob.JobName, DateTime.Now);
    }
}