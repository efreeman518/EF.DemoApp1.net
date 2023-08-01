namespace Package.Infrastructure.Messaging;

public class EventGridRequest
{
    public EventGridRequest(string clientName, EventGridEvent eventData)
    {
        ClientName = clientName;
        Event = eventData;
    }
    /// <summary>
    /// configured at startup with Creds/StorageAccountUri/ConnectionString
    /// </summary>
    public string ClientName { get; set; }

    public EventGridEvent Event { get; set; }
}
