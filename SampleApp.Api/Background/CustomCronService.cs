using Package.Infrastructure.BackgroundService;

namespace SampleApp.Api.Background;

public class CustomCronService : CronServiceSettings
{
    public string? SomeUrl { get; set; }
    public string? SomeTopicOrQueue { get; set; }
}
