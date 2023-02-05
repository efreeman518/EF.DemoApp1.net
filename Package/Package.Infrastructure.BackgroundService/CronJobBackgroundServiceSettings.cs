namespace Package.Infrastructure.BackgroundService;
public class CronJobBackgroundServiceSettings<T> where T : CronJobSettings
{
    public List<T> CronJobs { get; set; } = new List<T>();
}

