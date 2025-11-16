using Azure.Messaging.ServiceBus;

namespace Package.Infrastructure.Messaging.ServiceBus;

public class ServiceBusProcessorSettingsBase
{
    public string ServiceBusClientName { get; set; } = null!;
    public bool LogMessageData { get; set; } = true;
    public ServiceBusProcessorOptions ServiceBusProcessorOptions { get; set; } = null!;
}
