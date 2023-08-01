namespace Package.Infrastructure.Messaging;

public class EventGridPublisherSettingsBase
{
    public string EventGridPublisherClientName { get; set; } = null!;
    public bool LogEventData { get; set; } = true;
}
