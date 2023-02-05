using Package.Infrastructure.BackgroundService;

namespace SampleApp.Api.Background;

public class CustomCronJob : CronJobSettings
{
    public string? SomeUrl { get; set; }
    public string? SomeTopicOrQueue { get; set; }
}
