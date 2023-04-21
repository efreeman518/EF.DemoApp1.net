namespace Package.Infrastructure.BackgroundServices;

public abstract class CronJobSettings
{
    public string JobName { get; set; } = $"CronJob-{Guid.NewGuid()}";
    public string? Cron { get; set; }
    public int SleepIntervalSeconds { get; set; } = 600; //default every 10 min sleep
    public bool LockSingleInstance { get; set; } = true; //prevent multiple instances running if the workload takes longer than the cron interval
}
