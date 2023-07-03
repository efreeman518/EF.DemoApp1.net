namespace Package.Infrastructure.Messaging;

public class EventGridRequest
{
    public EventGridRequest(string clientName, EventGridEvent2 eventData)
    {
        ClientName = clientName;
        Event = eventData;
    }
    /// <summary>
    /// configured at startup with Creds/StorageAccountUri/ConnectionString
    /// </summary>
    public string ClientName { get; set; }

    public EventGridEvent2 Event { get; set; }
}
