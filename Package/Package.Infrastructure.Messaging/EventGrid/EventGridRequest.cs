namespace Package.Infrastructure.Messaging.EventGrid;

public class EventGridRequest(string clientName, EventGridEvent eventData)
{
    /// <summary>
    /// configured at startup with Creds/StorageAccountUri/ConnectionString
    /// </summary>
    public string ClientName { get; set; } = clientName;

    public EventGridEvent Event { get; set; } = eventData;
}
