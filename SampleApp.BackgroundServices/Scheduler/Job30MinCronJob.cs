using Package.Infrastructure.BackgroundServices.Cron;

namespace SampleApp.BackgroundServices.Scheduler;

public class Job30MinCronJob : CronJobSettings
{
    public string? SomeUrl { get; set; }
}
