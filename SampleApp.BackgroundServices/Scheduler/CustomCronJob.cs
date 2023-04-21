using Package.Infrastructure.BackgroundServices;

namespace SampleApp.BackgroundServices.Scheduler;

public class CustomCronJob : CronJobSettings
{
    public string? SomeUrl { get; set; }
    public string? SomeTopicOrQueue { get; set; }
}
