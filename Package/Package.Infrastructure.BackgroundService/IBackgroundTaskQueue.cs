namespace Package.Infrastructure.BackgroundServices;

public interface IBackgroundTaskQueue
{
    int QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, bool throwOnNullWorkitem = false);

    Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken);
}
