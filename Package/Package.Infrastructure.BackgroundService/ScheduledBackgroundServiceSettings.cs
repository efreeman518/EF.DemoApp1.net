namespace Package.Infrastructure.BackgroundService;
public class ScheduledBackgroundServiceSettings<T> where T : CronServiceSettings
{
    public List<T> CronServices { get; set; } = new List<T>();
}

