using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static NCrontab.CrontabSchedule;

namespace Package.Infrastructure.BackgroundServices.Cron;

public sealed class CronBackgroundService<T> : Microsoft.Extensions.Hosting.BackgroundService where T : CronJobSettings
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CronBackgroundService<T>> _logger;
    private volatile T _settings;
    private readonly Guid _lifeTimeId = Guid.NewGuid();

    private readonly Lock _settingsLock = new();
    private TaskCompletionSource<object?> _settingsChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenSource? _currentJobCts;

    public CronBackgroundService(ILogger<CronBackgroundService<T>> logger,
        IOptionsMonitor<T> settingsMonitor,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _settings = settingsMonitor.CurrentValue ?? throw new InvalidOperationException($"No configuration found for {typeof(T).Name}");

        settingsMonitor.OnChange(OnSettingsChanged);
    }

    private void OnSettingsChanged(T newSettings)
    {
        lock (_settingsLock)
        {
            _settings = newSettings ?? throw new InvalidOperationException($"Invalid configuration for {typeof(T).Name}");

            _logger.LogInformation("CronBackgroundService<{JobType}> settings changed. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);

            try
            {
                _currentJobCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // CTS may already be disposed during shutdown - safe to ignore
            }

            var tcs = _settingsChanged;
            tcs.TrySetResult(null);
            _settingsChanged = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () =>
    {
        stoppingToken.Register(() => _logger.LogInformation("CronBackgroundService<{JobType}> is stopping. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId));

        while (!stoppingToken.IsCancellationRequested)
        {
            var settingsSnapshot = _settings;

            if (string.IsNullOrWhiteSpace(settingsSnapshot?.Cron))
            {
                _logger.LogWarning("CronBackgroundService<{JobType}> waiting for configuration. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);

                try
                {
                    await Task.WhenAny(_settingsChanged.Task, Task.Delay(Timeout.Infinite, stoppingToken));
                }
                catch (OperationCanceledException)
                {
                    // Service stopping - exit loop gracefully
                    break;
                }
                continue;
            }

            using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            lock (_settingsLock)
            {
                _currentJobCts = jobCts;
            }

            var jobTask = Task.Run(async () => await ExecuteJobLoop(settingsSnapshot, jobCts.Token), jobCts.Token);

            try
            {
                await Task.WhenAny(jobTask, _settingsChanged.Task, Task.Delay(Timeout.Infinite, stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown or cancellation
            }

            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("CronBackgroundService<{JobType}> cancelling job for shutdown. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);

                try
                {
                    jobCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // CTS already disposed - safe to ignore
                }

                try
                {
                    await jobTask;
                }
                catch
                {
                    // Suppress all exceptions during shutdown - already logged in ExecuteJobLoop
                }
                break;
            }

            Task settingsChangedTask;
            lock (_settingsLock)
            {
                settingsChangedTask = _settingsChanged.Task;
            }

            if (settingsChangedTask.IsCompleted)
            {
                _logger.LogInformation("CronBackgroundService<{JobType}> restarting job due to settings change. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);

                try
                {
                    jobCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // CTS already disposed - safe to ignore
                }

                try
                {
                    await jobTask;
                }
                catch
                {
                    // Suppress all exceptions during restart - already logged in ExecuteJobLoop
                }

                // continue to pick up the new settings
                continue;
            }

            if (jobTask.IsCompleted)
            {
                _logger.LogWarning("CronBackgroundService<{JobType}> job completed unexpectedly. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);
            }
        }

        _logger.LogInformation("CronBackgroundService<{JobType}> stopped. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);
    }, CancellationToken.None);

    private async Task ExecuteJobLoop(T cronJob, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("{CronJob} is starting. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);

            ParseOptions o = new() { IncludingSeconds = true };

            if (string.IsNullOrWhiteSpace(cronJob.Cron))
            {
                _logger.LogWarning("{CronJob} has null or empty CRON expression; scheduled service will not run. Lifetime {LifetimeId}",
                    typeof(T).Name, _lifeTimeId);
                return;
            }

            var schedule = TryParse(cronJob.Cron, o);
            if (schedule is null)
            {
                _logger.LogWarning("{CronJob} CRON expression '{Cron}' is not valid; scheduled service will not run. Lifetime {LifetimeId}",
                    typeof(T).Name, cronJob.Cron, _lifeTimeId);
                return;
            }

            var nextRun = schedule.GetNextOccurrence(DateTime.Now);
            using var semaphore = new SemaphoreSlim(1, 1);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                if (now >= nextRun)
                {
                    try
                    {
                        if (cronJob.LockSingleInstance)
                        {
                            await semaphore.WaitAsync(stoppingToken);
                        }

                        try
                        {
                            var traceId = Guid.NewGuid().ToString();
                            _logger.LogDebug("{CronJob} executing with TraceId {TraceId}. Lifetime {LifetimeId}",
                                typeof(T).Name, traceId, _lifeTimeId);

                            using var scope = _serviceProvider.CreateScope();
                            var handler = scope.ServiceProvider.GetService<ICronJobHandler<T>>();

                            if (handler == null)
                            {
                                _logger.LogError("No handler found for cron job type {JobType}. Job will not run. Lifetime {LifetimeId}",
                                    typeof(T).Name, _lifeTimeId);
                                return;
                            }

                            await handler.ExecuteAsync(traceId, cronJob, stoppingToken);
                        }
                        finally
                        {
                            if (cronJob.LockSingleInstance)
                            {
                                semaphore.Release();
                            }
                        }
                    }
                    catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation(ex, "{CronJob} cancelled. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{CronJob} exception during execution. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);
                        // Continue - job will run again on next schedule
                    }

                    nextRun = schedule.GetNextOccurrence(DateTime.Now);
                    _logger.LogDebug("{CronJob} next run scheduled for {NextRun}. Lifetime {LifetimeId}",
                        typeof(T).Name, nextRun, _lifeTimeId);
                }

                var delaySeconds = Math.Max(1, cronJob.SleepIntervalSeconds);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                }
                catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation(ex, "{CronJob} delay cancelled. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);
                    break;
                }
            }

            _logger.LogInformation("{CronJob} is stopping. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);
        }
        catch (Exception ex)
        {
            // Fatal error - log but don't rethrow to prevent infinite restart loop
            _logger.LogCritical(ex, "{CronJob} fatal error. Job will remain stopped. Lifetime {LifetimeId}", typeof(T).Name, _lifeTimeId);
        }
    }

    public override void Dispose()
    {
        lock (_settingsLock)
        {
            try
            {
                _currentJobCts?.Cancel();
                _currentJobCts?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed during shutdown - safe to ignore
            }
        }

        base.Dispose();
    }
}
