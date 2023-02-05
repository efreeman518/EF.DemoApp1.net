using Package.Infrastructure.BackgroundService;

namespace SampleApp.Api.Background;

public class CronServiceSettings : CronJobBackgroundServiceSettings<CustomCronJob>
{
    public const string ConfigSectionName = "CronServiceSettings";
}

