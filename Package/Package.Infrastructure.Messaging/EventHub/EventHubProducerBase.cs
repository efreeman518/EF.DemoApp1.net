using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Package.Infrastructure.Messaging.EventHub;

//https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventhubs.processor-readme?view=azure-dotnet

public abstract class EventHubProducerBase : IEventHubProducer
{
    private readonly ILogger<EventHubProducerBase> _logger;
    private readonly EventHubProducerSettingsBase _settings;
    private readonly EventHubProducerClient _ehProducerClient;

    protected EventHubProducerBase(ILogger<EventHubProducerBase> logger, IOptions<EventHubProducerSettingsBase> settings, IAzureClientFactory<EventHubProducerClient> clientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _ehProducerClient = clientFactory.CreateClient(_settings.EventHubProducerClientName);
    }

    public async Task SendAsync(string message, string? partitionId = null, string? partitionKey = null, string? correlationId = null, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        var eventBody = new BinaryData(message);
        var eventData = new EventData(eventBody);
        if (correlationId != null) eventData.CorrelationId = correlationId;
        if (metadata != null)
        {
            foreach (var item in metadata)
            {
                eventData.Properties.Add(item);
            }
        }

        //optional specific partition
        SendEventOptions? sendEventOptions = null;
        if (partitionId != null || partitionKey != null)
        {
            sendEventOptions = new SendEventOptions
            {
                PartitionId = partitionId,
                PartitionKey = partitionKey
            };
        }
        //producer can only send a batch
        IEnumerable<EventData> batch = [eventData];

        try
        {
            _logger.LogDebug("SendAsync Start - {EventHubProducerClientName} {Message}", _settings.EventHubProducerClientName, _settings.LogMessageData ? eventData.Body : "LogMessageData = false");
            if (sendEventOptions == null)
            {
                await _ehProducerClient.SendAsync(batch, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
            }
            else
            {
                await _ehProducerClient.SendAsync(batch, sendEventOptions, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
            }
            _logger.LogDebug("SendAsync Finish - {EventHubProducerClientName}", _settings.EventHubProducerClientName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendAsync Fail - {EventHubProducerClientName} {Message}", _settings.EventHubProducerClientName, _settings.LogMessageData ? eventData.Body : "LogMessageData = false");
            throw;
        }
    }

    public async Task SendBatchAsync<T>(ICollection<T> batch, string? partitionId = null, string? partitionKey = null, string? correlationId = null, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        //assemble batch into a local queue
        Queue<EventData> q = new();
        EventData eventData;
        batch.ToList().ForEach(m =>
        {
            eventData = new EventData(new BinaryData(JsonSerializer.Serialize(m)));
            eventData.Properties.Add("MessageType", typeof(T).Name);
            eventData.Properties.Add("MessageTypeFull", typeof(T).FullName);
            if (correlationId != null) eventData.CorrelationId = correlationId;
            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    eventData.Properties.Add(item);
                }
            }
            q.Enqueue(eventData);
        });

        int messageCount = q.Count;
        CreateBatchOptions? batchOptions = null;
        if (partitionId != null || partitionKey != null || _settings.MaxBatchByteSize != null)
        {
            batchOptions = new CreateBatchOptions { MaximumSizeInBytes = _settings.MaxBatchByteSize, PartitionId = partitionId, PartitionKey = partitionKey };
        }

        //while - continue until all messages are sent 
        while (q.Count > 0)
        {
            // start a new batch 
            using var eventBatch = (batchOptions == null)
                ? await _ehProducerClient.CreateBatchAsync(cancellationToken).ConfigureAwait(false)
                : await _ehProducerClient.CreateBatchAsync(batchOptions, cancellationToken).ConfigureAwait(false);

            // add the first message to the batch
            if (eventBatch.TryAdd(q.Peek()))
            {
                // dequeue the message from the .NET queue once the message is added to the batch
                q.Dequeue();
            }
            else
            {
                // if the first message can't fit, then it is too large for the batch
                throw new InvalidOperationException($"{_settings.EventHubProducerClientName} Message {messageCount - q.Count} is too large and cannot be added to the Event Hub Batch: {q.Peek().Body.ToString().BinaryToString(Encoding.UTF8)}");
            }

            // add as many messages as possible to the current batch
            while (q.Count > 0 && eventBatch.TryAdd(q.Peek()))
            {
                // dequeue the message from the .NET queue as it has been added to the batch
                q.Dequeue();
            }

            //send the batch
            try
            {
                _logger.LogDebug("SendBatchAsync Start - {EventHubProducerClientName} batch count {BatchCount}; remaining message count {RemainingMessageCount}", _settings.EventHubProducerClientName, batch.Count, q.Count);
                await _ehProducerClient.SendAsync(eventBatch, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
                _logger.LogDebug("SendBatchAsync Finish - {EventHubProducerClientName} batch count {BatchCount}; remaining message count {RemainingMessageCount}", _settings.EventHubProducerClientName, batch.Count, q.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendBatchAsync Fail - {EventHubProducerClientName} batch count {BatchCount}; remaining message count {RemainingMessageCount}", _settings.EventHubProducerClientName, batch.Count, q.Count);
            }
            // if there are any remaining messages in the .NET queue, the while loop repeats 
        }
    }
}
