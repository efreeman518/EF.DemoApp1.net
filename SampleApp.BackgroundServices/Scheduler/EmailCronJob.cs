using Package.Infrastructure.BackgroundServices.Cron;

namespace SampleApp.BackgroundServices.Scheduler;

/// <summary>
/// Example of another cron job type for email processing
/// </summary>
public class EmailCronJob : CronJobSettings
{
    public string? SmtpServer { get; set; }
    public string? EmailTemplate { get; set; }
    public int BatchSize { get; set; } = 100;
}