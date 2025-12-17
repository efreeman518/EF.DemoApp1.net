using Package.Infrastructure.BackgroundServices.Cron;

namespace SampleApp.BackgroundServices.Scheduler;

public class Job2HourCronJob : CronJobSettings
{
    public string? SomeUrl { get; set; }
}
