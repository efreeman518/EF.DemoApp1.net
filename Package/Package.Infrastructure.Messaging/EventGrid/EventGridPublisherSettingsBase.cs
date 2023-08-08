namespace Package.Infrastructure.Messaging.EventGrid;

public class EventGridPublisherSettingsBase
{
    public string EventGridPublisherClientName { get; set; } = null!;
    public bool LogEventData { get; set; } = true;
}
