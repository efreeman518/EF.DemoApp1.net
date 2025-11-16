namespace Package.Infrastructure.Messaging.ServiceBus;

public class ServiceBusSenderSettingsBase
{
    public string ServiceBusClientName { get; set; } = null!;
    public bool LogMessageData { get; set; } = true;
}
