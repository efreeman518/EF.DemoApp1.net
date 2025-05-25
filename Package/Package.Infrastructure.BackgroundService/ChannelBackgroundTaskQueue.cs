using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Package.Infrastructure.BackgroundServices;

/// <summary>
/// Channel-based implementation of IBackgroundTaskQueue
/// </summary>
public sealed class ChannelBackgroundTaskQueue : IBackgroundTaskQueue, IDisposable
{
    private readonly Channel<Func<CancellationToken, Task>> _channel;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ChannelBackgroundTaskQueue>? _logger;
    private bool _disposed;

    public ChannelBackgroundTaskQueue(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ChannelBackgroundTaskQueue>? logger = null,
        int? boundedCapacity = null,
        BoundedChannelFullMode boundedChannelFullMode = BoundedChannelFullMode.Wait)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger;

        // Create bounded or unbounded channel based on capacity
        _channel = boundedCapacity.HasValue
            ? Channel.CreateBounded<Func<CancellationToken, Task>>(new BoundedChannelOptions(boundedCapacity.Value)
            {
                FullMode = boundedChannelFullMode,
                SingleReader = false, // Allow multiple consumers
                SingleWriter = false  // Allow multiple producers
            })
            : Channel.CreateUnbounded<Func<CancellationToken, Task>>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });
    }

    /// <summary>
    /// Gets the channel reader for direct consumption of work items
    /// </summary>
    public ChannelReader<Func<CancellationToken, Task>> Reader => _channel.Reader;

    /// <summary>
    /// Adds an async Func to the queue for later processing
    /// </summary>
    /// <param name="workItem">async Func taking a CancellationToken and returning a Task</param>
    /// <param name="throwOnNullWorkitem">throws if true and workItem is null</param>
    /// <returns>The number of tasks in the queue after adding this one (approximation), or -1 if workItem is null</returns>
    /// <exception cref="ArgumentNullException">If workItem is null and throwOnNullWorkitem is true</exception>
    public int QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, bool throwOnNullWorkitem = false)
    {
        if (workItem == null)
        {
            if (throwOnNullWorkitem)
                throw new ArgumentNullException(nameof(workItem));
            return -1;
        }

        if (_disposed)
        {
            _logger?.LogWarning("Cannot queue work item - channel is completed");
            return -1;
        }

        try
        {
            if (_channel.Writer.TryWrite(workItem))
            {
                return _channel.Reader.Count; // Returns approximate count after enqueue
            }

            _logger?.LogWarning("Failed to write work item to channel");
            return -1;
        }
        catch (ChannelClosedException exCCE)
        {
            _logger?.LogWarning(exCCE, "Cannot queue work item - channel is completed");
            return -1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error queuing background work item");
            throw new InvalidOperationException("Error queuing background work item", ex);
        }
    }

    /// <summary>
    /// Adds a scoped async Func to the queue for later processing
    /// </summary>
    /// <typeparam name="TScoped">Type of scoped service needed</typeparam>
    /// <param name="workItem">The work item function</param>
    /// <param name="throwOnNullWorkitem">Whether to throw on null work item</param>
    /// <param name="cancellationToken">Cancellation token (not used in this implementation but kept for interface compatibility)</param>
    /// <returns>The number of tasks in the queue after adding this one (approximation), or -1 if workItem is null</returns>
    public int QueueScopedBackgroundWorkItem<TScoped>(
        Func<TScoped, CancellationToken, Task> workItem,
        bool throwOnNullWorkitem = false,
        CancellationToken cancellationToken = default)
    {
        if (workItem == null)
        {
            if (throwOnNullWorkitem)
                throw new ArgumentNullException(nameof(workItem));
            return -1;
        }

        // Create a wrapper function that will create a scope and resolve TScoped service
        return QueueBackgroundWorkItem(async token =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedService = scope.ServiceProvider.GetService<TScoped>()
                    ?? throw new ArgumentException($"Scoped background work depends on scoped service {typeof(TScoped).Name} but it is not registered.");

                await workItem(scopedService!, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Expected when cancellation is requested
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing scoped background task for service {ServiceType}", typeof(TScoped).Name);
            }
        }, throwOnNullWorkitem);
    }

    /// <summary>
    /// Waits for and removes the first item in the queue
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The work item to process, or null if channel is completed</returns>
    public async Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
            return null;

        try
        {
            if (await _channel.Reader.WaitToReadAsync(cancellationToken) && _channel.Reader.TryRead(out var item))
            {
                return item;
            }

            return null;
        }
        catch (ChannelClosedException)
        {
            // Channel is completed and empty
            return null;
        }
        catch (OperationCanceledException)
        {
            // Propagate cancellation
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error dequeuing work item");
            throw;
        }
    }

    /// <summary>
    /// Gets an IAsyncEnumerable that yields work items as they become available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>IAsyncEnumerable of work items</returns>
    public IAsyncEnumerable<Func<CancellationToken, Task>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the approximate number of items in the queue
    /// </summary>
    public int Count => _channel.Reader.Count;

    /// <summary>
    /// Returns whether the channel has been marked as completed
    /// </summary>
    public bool IsCompleted => _channel.Reader.Completion.IsCompleted;

    /// <summary>
    /// Completes the channel, preventing additional items from being enqueued
    /// Existing items can still be processed
    /// </summary>
    public void Complete()
    {
        if (!_disposed)
        {
            _channel.Writer.Complete();
        }
    }

    /// <summary>
    /// Disposes the channel queue
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Complete();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}