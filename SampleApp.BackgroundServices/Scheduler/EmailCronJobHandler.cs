using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundServices.Cron;

namespace SampleApp.BackgroundServices.Scheduler;

/// <summary>
/// Handler for email cron jobs
/// </summary>
public class EmailCronJobHandler(ILogger<EmailCronJobHandler> logger, IServiceScopeFactory serviceScopeFactory)
    : ICronJobHandler<EmailCronJob>
{
    public async Task ExecuteAsync(string traceId, EmailCronJob cronJob, CancellationToken stoppingToken = default)
    {
        logger.LogInformation("{CronJob} - Start email processing job {Runtime}", cronJob.JobName, DateTime.Now);

        try
        {
            using var scope = serviceScopeFactory.CreateScope();

            // Email-specific processing logic here
            logger.LogInformation("Processing emails with SMTP: {SmtpServer}, Template: {EmailTemplate}, Batch Size: {BatchSize}",
                cronJob.SmtpServer, cronJob.EmailTemplate, cronJob.BatchSize);

            // Simulate email processing work
            await Task.Delay(1000, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{CronJob} - Failed during email processing", cronJob.JobName);
            throw; // Re-throw to let the base service handle it
        }

        logger.LogInformation("{CronJob} - Complete email processing job {Runtime}", cronJob.JobName, DateTime.Now);
    }
}