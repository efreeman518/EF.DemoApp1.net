using Package.Infrastructure.BackgroundServices;

namespace SampleApp.BackgroundServices.Scheduler;

public class CronServiceSettings : CronJobBackgroundServiceSettings<CustomCronJob>
{
    public const string ConfigSectionName = "CronServiceSettings";
}

