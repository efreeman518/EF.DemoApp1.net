using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Package.Infrastructure.BackgroundServices;

//https://blog.elmah.io/async-processing-of-long-running-tasks-in-asp-net-core/amp/
//https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=net-7.0

public class BackgroundTaskQueue(IServiceScopeFactory serviceScopeFactory) : IBackgroundTaskQueue
{
    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new();
    private readonly SemaphoreSlim _semaphore = new(0); //no workItems initially, so 0 threads allowed in the semaphore that attempts to Dequeue

    /// <summary>
    /// Waits for and removes the first item in the queue - entering the semaphore (-1)
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken); //enters if semaphore > 0 (workItem was added to the queue)
        _workItems.TryDequeue(out var workItem);
        return workItem;
    }

    /// <summary>
    /// Adds an async Func to the queue for later processing, semaphore.Release adds +1, allowing another thread to enter the semaphore (-1)
    /// If work includes scoped services, workItem must create the scope, otherwise scoped services may fall out of scope 
    /// and be disposed before the work is done in the background
    /// </summary>
    /// <param name="workItem">async Func taking a CancellationToken and returning a Task</param>
    /// <param name="throwOnNullWorkitem">throws if true and workItem is null</param>
    /// <returns>the number of workItems previously queued (previous count of the semaphore), otherwise -1</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public int QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, bool throwOnNullWorkitem = false)
    {
        if (workItem != null)
        {
            _workItems.Enqueue(workItem);
            return _semaphore.Release(); //semaphore +1
        }
        else if (throwOnNullWorkitem)
            throw new ArgumentNullException(nameof(workItem));

        return -1;
    }

    public int QueueScopedBackgroundWorkItem<TScoped>(Func<TScoped, CancellationToken, Task> workItem, bool throwOnNullWorkitem = false, CancellationToken cancellationToken = default)
    {
        if (workItem != null)
        {
            _workItems.Enqueue(async cancellationToken =>
                {
                    try
                    {
                        using var scope = serviceScopeFactory.CreateScope();
                        var scopedService = scope.ServiceProvider.GetService<TScoped>()
                            ?? throw new ArgumentException($"Scoped background work depends on scoped service but it is not registered.");
                        await workItem(scopedService!, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });

            return _semaphore.Release(); //semaphore +1
        }
        else if (throwOnNullWorkitem)
            throw new ArgumentNullException(nameof(workItem));

        return -1;
    }
}

/// <summary>
/// Fire-And-Forget / long running background task
/// </summary>
public class BackgroundTaskService(ILogger<BackgroundTaskService> logger, IBackgroundTaskQueue taskQueue) : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue = taskQueue;
    private readonly ILogger<BackgroundTaskService> _logger = logger;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //keep checking the queue
        while (!stoppingToken.IsCancellationRequested)
        {
            //throttle if not awaiting workItem
            var workItem = await _taskQueue.DequeueAsync(stoppingToken); //waits for a task on the queue (semaphore > 0)
            if (workItem != null)
            {
                try
                {
                    //await or not await (eliding) - workItems that are not thread safe could have multiple threads
                    //https://blog.stephencleary.com/2016/12/eliding-async-await.html
                    await workItem(stoppingToken);
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

