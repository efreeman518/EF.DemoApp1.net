using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Package.Infrastructure.Messaging.ServiceBus;
public abstract class ServiceBusSenderBase : IServiceBusSender
{
    private readonly ILogger<ServiceBusSenderBase> _logger;
    private readonly ServiceBusSenderSettingsBase _settings;
    private readonly ServiceBusClient _sbClient;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _sbSenders = new();
    private static readonly object _lock = new();

    protected ServiceBusSenderBase(ILogger<ServiceBusSenderBase> logger,
        IOptions<ServiceBusSenderSettingsBase> settings, IAzureClientFactory<ServiceBusClient> clientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _sbClient = clientFactory.CreateClient(_settings.ServiceBusClientName);
    }

    /// <summary>
    /// Sends message to Azure Service Bus; ServiceBusProcessor will receive the messages
    /// </summary>
    /// <param name="queueOrTopicName"></param>
    /// <param name="message"></param>
    /// <param name="metadata"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task SendMessageAsync(string queueOrTopicName, string message, string? correlationId = null, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        ServiceBusSender sender = GetServiceBusSenderAsync(queueOrTopicName);

        ServiceBusMessage sbMessage = new(message);
        if (correlationId != null) sbMessage.CorrelationId = correlationId;
        if (metadata != null)
        {
            foreach (var item in metadata)
            {
                sbMessage.ApplicationProperties.Add(item);
            }
        }
        _logger.LogDebug("SendMessageAsync Start - {Message}", _settings.LogMessageData ? sbMessage.Body : "LogMessageData = false");
        await sender.SendMessageAsync(sbMessage, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        _logger.LogDebug("SendMessageAsync Finish - {Message}", _settings.LogMessageData ? sbMessage.Body : "LogMessageData = false");
    }

    /// <summary>
    /// Sends message batches to Azure Service Bus
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queueOrTopicName"></param>
    /// <param name="batch"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task SendBatchAsync<T>(string queueOrTopicName, ICollection<T> batch, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        //assemble batch into a local queue
        Queue<ServiceBusMessage> q = new();
        ServiceBusMessage sbm;
        batch.ToList().ForEach(m =>
        {
            sbm = new ServiceBusMessage(JsonSerializer.Serialize(m));
            sbm.ApplicationProperties.Add("MessageType", typeof(T).Name);
            sbm.ApplicationProperties.Add("MessageTypeFull", typeof(T).FullName);
            if (correlationId != null) sbm.CorrelationId = correlationId;
            q.Enqueue(sbm);
        });

        ServiceBusSender sender = GetServiceBusSenderAsync(queueOrTopicName);
        int messageCount = q.Count;

        //while - continue until all messages are sent to the Service Bus topic/queue
        while (q.Count > 0)
        {
            // start a new batch 
            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync(cancellationToken).ConfigureAwait(false);

            // add the first message to the batch
            if (messageBatch.TryAddMessage(q.Peek()))
            {
                // dequeue the message from the .NET queue once the message is added to the batch
                q.Dequeue();
            }
            else
            {
                // if the first message can't fit, then it is too large for the batch
                throw new InvalidOperationException($"Message {messageCount - q.Count} is too large and cannot be added to the ServiceBusMessageBatch: {q.Peek().Body.ToString().BinaryToString(Encoding.UTF8)}");
            }

            // add as many messages as possible to the current batch
            while (q.Count > 0 && messageBatch.TryAddMessage(q.Peek()))
            {
                // dequeue the message from the .NET queue as it has been added to the batch
                q.Dequeue();
            }

            //send the batch
            _logger.LogDebug("SendBatchAsync Start - batch count {BatchCount}; remaining message count {RemainingMessageCount}", messageBatch.Count, q.Count);
            await sender.SendMessagesAsync(messageBatch, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
            _logger.LogDebug("SendBatchAsync Finish - batch count {BatchCount}; remaining message count {RemainingMessageCount}", messageBatch.Count, q.Count);
            // if there are any remaining messages in the .NET queue, the while loop repeats 
        }
    }

    private ServiceBusSender GetServiceBusSenderAsync(string queueOrTopicName)
    {
        if (_sbSenders.TryGetValue(queueOrTopicName, out var result)) return result;

        lock (_lock)
        {
            //Try to fetch from cache again now that we have entered the critical section
            if (_sbSenders.TryGetValue(queueOrTopicName, out result)) return result;

            //update cache and return.
            ServiceBusSender sbs = _sbClient.CreateSender(queueOrTopicName);
            _sbSenders.TryAdd(queueOrTopicName, sbs);
            return sbs;
        }
    }
}
