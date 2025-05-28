using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.BackgroundServices.Attributes;
using Package.Infrastructure.Common.Contracts;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Package.Infrastructure.BackgroundServices.InternalMessageBroker;

/// <summary>
/// Should be singleton
/// </summary>
public class InternalMessageBus(
    ILogger<InternalMessageBus> logger,
    IServiceProvider services,
    IBackgroundTaskQueue backgroundTaskQueue,
    IOptions<InternalMessageBusSettings> settings) : IInternalMessageBus
{
    private sealed record HandlerInfo(object Handler, bool RequiresScope);

    // Thread-safe handler registry for each event type and its bag of handlers
    private readonly ConcurrentDictionary<Type, ConcurrentBag<HandlerInfo>> _handlers = new();

    /// <summary>
    /// Call at startup to auto register all the handlers found in the loaded assemblies.
    /// </summary>
    public void AutoRegisterHandlers()
    {
        var handlerInterfaceType = typeof(IMessageHandler<>);

        // Scan all loaded types for closed IMessageHandler<T>
        var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
            })
            .Where(t => t is not null && !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t!.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                .Select(i => new { HandlerType = t, MessageType = i.GetGenericArguments()[0] }))
            .GroupBy(x => new { x.HandlerType, x.MessageType })
            .Select(g => g.First())
            .ToList();

        foreach (var ht in handlerTypes)
        {
            var closedHandlerType = handlerInterfaceType.MakeGenericType(ht.MessageType);

            // Create a scope for resolving scoped handlers
            using var scope = services.CreateScope();
            var handlers = scope.ServiceProvider.GetServices(closedHandlerType);

            foreach (var handler in handlers)
            {
                bool requiresScope = handler!.GetType().GetCustomAttribute<ScopedMessageHandlerAttribute>() != null;
                var info = new HandlerInfo(handler, requiresScope);
                var bag = _handlers.GetOrAdd(ht.MessageType, _ => []);
                // Prevent duplicate registration
                if (!bag.Any(h => h.Handler.GetType() == handler.GetType()))
                    bag.Add(info);
            }
        }
    }

    /// <summary>
    /// Register a message handler at runtime
    /// </summary>
    public void RegisterMessageHandler<T>(IMessageHandler<T> handler) where T : IMessage
    {
        var key = typeof(T);
        var requiresScope = handler.GetType().GetCustomAttribute<ScopedMessageHandlerAttribute>() != null;
        var info = new HandlerInfo(handler, requiresScope);
        var bag = _handlers.GetOrAdd(key, _ => []);
        bag.Add(info);
    }

    /// <summary>
    /// Unregister a message handler at runtime
    /// </summary>
    public void UnregisterMessageHandler<T>(IMessageHandler<T> handler) where T : IMessage
    {
        var key = typeof(T);
        if (_handlers.TryGetValue(key, out var bag))
        {
            // Remove by rebuilding the bag (ConcurrentBag does not support removal)
            var newBag = new ConcurrentBag<HandlerInfo>(bag.Where(h => !ReferenceEquals(h.Handler, handler)));
            _handlers[key] = newBag;
        }
    }

    /// <summary>
    /// Publish message(s) to registered handlers
    /// </summary>
    public void Publish<T>(InternalMessageBusProcessMode mode, ICollection<T> messages) where T : IMessage
    {
        logger.LogDebug("Publish Start {Mode} {MessageType}", mode, typeof(T));

        //get the handlers for the message type
        if (!_handlers.TryGetValue(typeof(T), out var bag) || bag.IsEmpty) return;

        var handlerInfos = bag.ToList();
        QueueMessageHandlerWork(mode, handlerInfos, messages);
    }

    private void QueueMessageHandlerWork<T>(InternalMessageBusProcessMode mode, List<HandlerInfo> handlerInfos, ICollection<T> messages) where T : IMessage
    {
        logger.LogDebug("QueueMessageHandlerWork Start");

        if (handlerInfos.Count > 0)
        {
            // If mode is Queue, only process with the first handler
            if (mode == InternalMessageBusProcessMode.Queue)
                handlerInfos = [handlerInfos[0]];

            foreach (var handlerInfo in handlerInfos)
            {
                if (handlerInfo.RequiresScope)
                {
                    backgroundTaskQueue.QueueScopedBackgroundWorkItem<IMessageHandler<T>>(async (scopedHandler, token) =>
                    {
                        foreach (var message in messages)
                        {
                            await HandleInternalAsync(scopedHandler, message, token);
                        }
                    });
                }
                else
                {
                    var handler = (IMessageHandler<T>)handlerInfo.Handler;
                    backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                    {
                        foreach (var message in messages)
                        {
                            await HandleInternalAsync(handler, message, token);
                        }
                    });
                }
            }
        }

        logger.LogDebug("QueueMessageHandlerWork Finish");
    }

    private async Task HandleInternalAsync<T>(IMessageHandler<T> handler, T message, CancellationToken token)
        where T : IMessage
    {
        var jsonMessage = settings.Value.LogMessageBody ? JsonSerializer.Serialize(message) : "No logging";
        try
        {
            logger.LogDebug("HandleInternalAsync Start - {Handler} {Message}", handler.ToString(), jsonMessage);
            await handler.HandleAsync(message, token);
        }
        catch (Exception ex)
        {
            logger.LogError(0, ex, "HandleInternalAsync Fail - {Handler} {Message}", handler.ToString(), jsonMessage);
        }
    }
}
