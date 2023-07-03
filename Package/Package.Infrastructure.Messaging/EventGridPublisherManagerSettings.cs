namespace Package.Infrastructure.Messaging;

public class EventGridPublisherManagerSettings
{
    public const string ConfigSectionName = "EventGridPublisherManagerSettings";
    public bool LogEventData { get; set; } = true;
}
