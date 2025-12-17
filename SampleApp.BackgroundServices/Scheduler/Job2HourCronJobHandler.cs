using Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundServices.Cron;

namespace SampleApp.BackgroundServices.Scheduler;

public class Job2HourCronJobHandler(IServiceScopeFactory serviceScopeFactory, ILogger<Job2HourCronJobHandler> logger)
    : ICronJobHandler<Job2HourCronJob>
{
    public async Task ExecuteAsync(string traceId, Job2HourCronJob cronJob, CancellationToken stoppingToken = default)
    {
        logger.LogInformation("{CronJob} (2hour) - Start with TraceId {TraceId}", cronJob.JobName, traceId);

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            //var todoService = scope.ServiceProvider.GetRequiredService<ITodoService>();

            // 2-hour job specific logic
            logger.LogInformation("Processing 2-hour job with URL: {Url}", cronJob.SomeUrl);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{CronJob} - Failed during execution", cronJob.JobName);
        }

        logger.LogInformation("{CronJob} (2hour) - Complete", cronJob.JobName);
    }
}
