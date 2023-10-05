using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Package.Infrastructure.BackgroundServices.InternalMessageBroker;

/// <summary>
/// Should be singleton
/// </summary>
public class InternalBroker(ILogger<InternalBroker> logger, IServiceProvider services, IBackgroundTaskQueue backgroundTaskQueue,
    IOptions<InternalBrokerSettings> settings) : IInternalBroker
{
    //register handlers at startup instead of creating scope and looking for handlers in service collection at runtime
    private readonly Dictionary<Type, object> _handlers = [];

    /// <summary>
    /// Register internal message handlers at startup
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="handler"></param>
    public void RegisterMessageHandler<T>(IMessageHandler<T> handler) where T : IMessage
    {
        Type key = typeof(T);
        //check if _handlers contains an entry (List) for handling T
        List<IMessageHandler<T>>? handlers;
        if (!_handlers.TryGetValue(key, out object? h))
        {
            //if not exist, create the List of handlers for T
            handlers = [handler];
            _handlers.Add(key, handlers);
        }
        else
        {
            handlers = (List<IMessageHandler<T>>)h;
            handlers.Add(handler);
        }
    }

    /// <summary>
    /// Raise event to internal (in-process) registered handlers;
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mode"></param>
    /// <param name="messages"></param>
    /// <returns></returns>
    public void ProcessRegistered<T>(ProcessInternalMode mode, ICollection<T> messages) where T : IMessage
    {
        var handlers = (List<IMessageHandler<T>>?)_handlers[typeof(T)];
        if (handlers == null) return;
        ProcessMessages(mode, handlers, messages);
    }

    //public bool UnregisterHandler(object hashCode)


    /// <summary>
    /// Raise event to internal (in-process) handlers (Service Collection); 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mode"></param>
    /// <param name="messages"></param>
    /// <returns></returns>
    public void Process<T>(ProcessInternalMode mode, ICollection<T> messages) where T : IMessage
    {
        logger.LogDebug("Process Start");

        List<IMessageHandler<T>> handlers = services.GetServices<IMessageHandler<T>>().ToList();
        if (handlers.Count > 0)
        {
            using (services.CreateScope())
            {
                ProcessMessages(mode, handlers, messages);
            }
        }

        logger.LogDebug("Process Finish");
    }

    /// <summary>
    /// Process the messages with a background task; hosted service BackgroundTaskService must be running
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mode"></param>
    /// <param name="handlers"></param>
    /// <param name="messages"></param>
    private void ProcessMessages<T>(ProcessInternalMode mode, List<IMessageHandler<T>>? handlers, ICollection<T> messages) where T : IMessage
    {
        if (handlers != null && handlers.Count > 0)
        {
            if (mode == ProcessInternalMode.Queue) handlers = [handlers[0]];

            backgroundTaskQueue.QueueBackgroundWorkItem(async (token) =>
            {
                var taskMessages = messages.Select(async (m) =>
                {
                    var taskHandlers = handlers.Select(h => HandleInternalAsync(h, m)); //handlers must be thread safe for concurrent processing
                    await Task.WhenAll(taskHandlers);
                });
                await Task.WhenAll(taskMessages);
            });
        }
    }

    private async Task HandleInternalAsync<T>(IMessageHandler<T> handler, T message) where T : IMessage
    {
        string jsonMessage = settings.Value.LogMessageBody ? JsonSerializer.Serialize(message) : "No logging";

        try
        {
            logger.LogDebug("HandleInternalAsync Start - {Handler} {Message}", handler.ToString(), jsonMessage);
            await handler.HandleAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(0, ex, "HandleInternalAsync Fail - {Handler} {Message}", handler.ToString(), jsonMessage);
            //continue to next handler (do not throw)
        }
    }
}
