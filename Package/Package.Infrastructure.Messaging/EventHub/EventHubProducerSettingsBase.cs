namespace Package.Infrastructure.Messaging.EventHub;
public class EventHubProducerSettingsBase
{
    public string EventHubProducerClientName { get; set; } = null!;
    public long? MaxBatchByteSize { get; set; } = null;
    public bool LogMessageData { get; set; } = true;
}
