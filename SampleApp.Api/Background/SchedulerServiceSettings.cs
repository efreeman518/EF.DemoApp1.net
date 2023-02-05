using Package.Infrastructure.BackgroundService;

namespace SampleApp.Api.Background;

public class SchedulerServiceSettings : ScheduledBackgroundServiceSettings<CustomCronService>
{
    public const string ConfigSectionName = "ScheduledServiceSettings";
}

