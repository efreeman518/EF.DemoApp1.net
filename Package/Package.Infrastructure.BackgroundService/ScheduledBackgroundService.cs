using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static NCrontab.CrontabSchedule;

namespace Package.Infrastructure.BackgroundService;

public abstract class CronServiceSettings
{
    public string ServiceName { get; set; } = $"ScheduledService-{Guid.NewGuid()}";
    public string? Schedule { get; set; } //Cron
    public int SleepIntervalSeconds { get; set; } = 600; //default every 10 min sleep
}

public abstract class ScheduledBackgroundService<T> : Microsoft.Extensions.Hosting.BackgroundService where T : CronServiceSettings
{
    //Inheriting class must implement
    protected abstract Task ExecuteInScopeAsync(IServiceProvider serviceProvider, string TraceId, T cronService, CancellationToken stoppingToken = default);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ScheduledBackgroundServiceSettings<T> _settings;
    private readonly Guid _lifeTimeId = Guid.NewGuid();
    protected readonly ILogger<ScheduledBackgroundService<T>>? Logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceScopeFactory"></param>
    /// <param name="settings"></param>
    /// <param name="logger"></param>
    /// <param name="loggingSettings"></param>
    protected ScheduledBackgroundService(IServiceScopeFactory serviceScopeFactory, IOptions<ScheduledBackgroundServiceSettings<T>> settings,
        ILogger<ScheduledBackgroundService<T>>? logger = null) : base()
    {
        _serviceScopeFactory = serviceScopeFactory;
        _settings = settings.Value;
        Logger = logger;
    }


    //https://blog.stephencleary.com/2020/05/backgroundservice-gotcha-silent-failure.html
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () =>
    {
        if (_settings.CronServices == null) return;

        List<Task> tasks = new();
        foreach (var cron in _settings.CronServices)
        {
            tasks.Add(Task.Run(async () =>
            {
                stoppingToken.Register(() => Logger?.Log(LogLevel.Information, "{ServiceName} is stopping. Lifetime {LifetimeId}", cron.ServiceName, _lifeTimeId));
                await ExecuteTask1(cron, stoppingToken);
            }));
        }
        await Task.WhenAll(tasks);
    }, stoppingToken);

    private async Task ExecuteTask1(T cron, CancellationToken stoppingToken = default)
    {
        try
        {
            Logger?.Log(LogLevel.Information, "{ServiceName} is starting. Lifetime {LifetimeId}", cron.ServiceName, _lifeTimeId);
            ParseOptions o = new()
            {
                IncludingSeconds = true
            };
            var schedule = TryParse(cron.Schedule, o);
            if (schedule == null)
            {
                Logger?.Log(LogLevel.Warning, "'{Schedule}' is not a valid CRON expression; scheduled service will not run.", cron.Schedule);
                return;
            }
            var nextRun = schedule.GetNextOccurrence(DateTime.Now);

            do
            {
                var now = DateTime.Now;
                if (now > nextRun)
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        await ExecuteInScopeAsync(scope.ServiceProvider, Guid.NewGuid().ToString(), cron, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Logger?.Log(LogLevel.Error, ex, "{ServiceName} exception. Lifetime {LifetimeId}", cron.ServiceName, _lifeTimeId);
                    }
                    nextRun = schedule.GetNextOccurrence(DateTime.Now);
                }
                await Task.Delay(cron.SleepIntervalSeconds, stoppingToken); //async delay between executes
            }
            while (!stoppingToken.IsCancellationRequested);

            Logger?.Log(LogLevel.Information, "{ServiceName} is stopping. Lifetime {LifetimeId}", cron.ServiceName, _lifeTimeId);
        }
        catch (Exception ex)
        {
            Logger?.Log(LogLevel.Critical, ex, "{ServiceName} fatal error. Lifetime {LifetimeId}", cron.ServiceName, _lifeTimeId);
            throw;
        }
    }
}
