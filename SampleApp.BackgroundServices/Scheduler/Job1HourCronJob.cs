using Package.Infrastructure.BackgroundServices.Cron;

namespace SampleApp.BackgroundServices.Scheduler;

public class Job1HourCronJob : CronJobSettings
{
    public string? SomeTopicOrQueue { get; set; }
}
