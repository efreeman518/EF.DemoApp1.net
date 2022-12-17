using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Infrastructure.BackgroundServices;

//https://blog.elmah.io/async-processing-of-long-running-tasks-in-asp-net-core/amp/

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new();
    private readonly SemaphoreSlim _signal = new(0);

    public async Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _workItems.TryDequeue(out var workItem);
        return workItem;
    }

    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        _ = workItem ?? throw new ArgumentNullException(nameof(workItem));

        _workItems.Enqueue(workItem);
        _signal.Release();
    }
}

/// <summary>
/// Fire-And-Forget / long running background task
/// </summary>
public class BackgroundTaskService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<BackgroundTaskService> _logger;

    public BackgroundTaskService(IBackgroundTaskQueue taskQueue, ILogger<BackgroundTaskService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //keep checking the queue
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);
            if (workItem != null)
            {
                try
                {
                    _ = workItem(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ExecuteAsync Fail");
                }
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackgroundTaskService Hosted Service is stopping.");

        await base.StopAsync(cancellationToken);
    }
}

