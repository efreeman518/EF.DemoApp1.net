namespace Package.Infrastructure.BackgroundServices;

public interface IBackgroundTaskQueue
{
    int QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, bool throwOnNullWorkitem = false);
    int QueueScopedBackgroundWorkItem<TScoped>(Func<TScoped, CancellationToken, Task> workItem, bool throwOnNullWorkitem = false, CancellationToken cancellationToken = default);

    Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken);
}
