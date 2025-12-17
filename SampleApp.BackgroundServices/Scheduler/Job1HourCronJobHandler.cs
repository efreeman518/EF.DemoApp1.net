using Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundServices.Cron;

namespace SampleApp.BackgroundServices.Scheduler;

public class Job1HourCronJobHandler(IServiceScopeFactory serviceScopeFactory, ILogger<Job1HourCronJobHandler> logger)
    : ICronJobHandler<Job1HourCronJob>
{
    public async Task ExecuteAsync(string traceId, Job1HourCronJob cronJob, CancellationToken stoppingToken = default)
    {
        logger.LogInformation("{CronJob} (1hour) - Start with TraceId {TraceId}", cronJob.JobName, traceId);

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            //var todoService = scope.ServiceProvider.GetRequiredService<ITodoService>();

            // 1-hour job specific logic
            logger.LogInformation("Processing 1-hour job with Topic: {Topic}", cronJob.SomeTopicOrQueue);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{CronJob} - Failed during execution", cronJob.JobName);
        }

        logger.LogInformation("{CronJob} (1hour) - Complete", cronJob.JobName);
    }
}
