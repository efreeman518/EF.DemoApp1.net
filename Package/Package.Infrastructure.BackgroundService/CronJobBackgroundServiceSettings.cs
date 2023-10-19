namespace Package.Infrastructure.BackgroundServices;

public class CronJobBackgroundServiceSettings<T> where T : CronJobSettings
{
    public List<T> CronJobs { get; set; } = [];
}

