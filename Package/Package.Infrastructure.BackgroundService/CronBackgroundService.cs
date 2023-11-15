using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static NCrontab.CrontabSchedule;

namespace Package.Infrastructure.BackgroundServices;

public abstract class CronBackgroundService<T> : Microsoft.Extensions.Hosting.BackgroundService where T : CronJobSettings
{
    //Inheriting class must implement
    protected abstract Task RunOnScheduleAsync(string TraceId, T cronService, CancellationToken stoppingToken = default);

    protected readonly ILogger<CronBackgroundService<T>> Logger;
    private readonly CronJobBackgroundServiceSettings<T> _settings;
    private readonly Guid _lifeTimeId = Guid.NewGuid();

    protected CronBackgroundService(ILogger<CronBackgroundService<T>> logger, IOptions<CronJobBackgroundServiceSettings<T>> settings) : base()
    {
        _settings = settings.Value;
        Logger = logger;
    }

    //https://blog.stephencleary.com/2020/05/backgroundservice-gotcha-silent-failure.html
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () =>
    {
        if (_settings.CronJobs == null) return;

        stoppingToken.Register(() => Logger.Log(LogLevel.Information, "CronBackgroundService is stopping. Lifetime {LifetimeId}", _lifeTimeId));

        List<Task> tasks = [];
        foreach (var cronJob in _settings.CronJobs)
        {
            tasks.Add(Task.Run(async () =>
            {
                await ExecuteTask1(cronJob, stoppingToken);
            }));
        }
        await Task.WhenAll(tasks);
    }, stoppingToken);

    private async Task ExecuteTask1(T cronJob, CancellationToken stoppingToken = default)
    {
        try
        {
            Logger.Log(LogLevel.Information, "{CronJob} is starting. Lifetime {LifetimeId}", cronJob.JobName, _lifeTimeId);
            ParseOptions o = new()
            {
                IncludingSeconds = true
            };
            var schedule = TryParse(cronJob.Cron, o);
            if (schedule == null)
            {
                Logger.Log(LogLevel.Warning, "'{Cron}' is not a valid CRON expression; scheduled service will not run.", cronJob.Cron);
                return;
            }
            var nextRun = schedule.GetNextOccurrence(DateTime.Now);
            DateTime now;

            SemaphoreSlim semaphore = new(1);

            while (!stoppingToken.IsCancellationRequested)
            {
                now = DateTime.Now;
                if (now > nextRun)
                {
                    try
                    {
                        if (cronJob.LockSingleInstance)
                        {
                            await semaphore.WaitAsync(stoppingToken);
                        }
                        await RunOnScheduleAsync(Guid.NewGuid().ToString(), cronJob, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, ex, "{CronJob} exception. Lifetime {LifetimeId}", cronJob.JobName, _lifeTimeId);
                    }
                    finally
                    {
                        if (cronJob.LockSingleInstance)
                        {
                            semaphore.Release();
                        }
                    }

                    nextRun = schedule.GetNextOccurrence(DateTime.Now);
                }
                await Task.Delay(cronJob.SleepIntervalSeconds, stoppingToken); //async delay between schedule check
            }


            Logger.Log(LogLevel.Information, "{CronJob} is stopping. Lifetime {LifetimeId}", cronJob.JobName, _lifeTimeId);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Critical, ex, "{CronJob} fatal error. Lifetime {LifetimeId}", cronJob.JobName, _lifeTimeId);
            throw;
        }
    }
}
