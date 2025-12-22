using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundServices.Cron;

namespace SampleApp.BackgroundServices.Scheduler;

public class Job30MinCronJobHandler(IServiceScopeFactory serviceScopeFactory, ILogger<Job30MinCronJobHandler> logger)
    : ICronJobHandler<Job30MinCronJob>
{
    public async Task ExecuteAsync(string traceId, Job30MinCronJob cronJob, CancellationToken stoppingToken = default)
    {
        logger.LogInformation("{CronJob} (30min) - Start with TraceId {TraceId}", cronJob.JobName, traceId);

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            //var todoService = scope.ServiceProvider.GetRequiredService<ITodoService>();

            // 30-minute job specific logic
            logger.LogInformation("Processing 30-min job with URL: {Url}", cronJob.SomeUrl);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{CronJob} - Failed during execution", cronJob.JobName);
        }

        logger.LogInformation("{CronJob} (30min) - Complete", cronJob.JobName);
    }
}
