namespace Package.Infrastructure.BackgroundService;
public abstract class CronJobSettings
{
    public string JobName { get; set; } = $"CronJob-{Guid.NewGuid()}";
    public string? Cron { get; set; }
    public int SleepIntervalSeconds { get; set; } = 600; //default every 10 min sleep
}
