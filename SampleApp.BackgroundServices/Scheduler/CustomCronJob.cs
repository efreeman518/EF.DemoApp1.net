using Package.Infrastructure.BackgroundServices.Cron;

namespace SampleApp.BackgroundServices.Scheduler;

public class CustomCronJob : CronJobSettings
{
    public string? SomeUrl { get; set; }
    public string? SomeTopicOrQueue { get; set; }
}
