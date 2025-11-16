namespace Package.Infrastructure.Messaging.EventHub;

public class EventHubProcessorSettingsBase
{
    public string EventHubProcessorClientName { get; set; } = null!;
    public int TaskSleepIntervalSeconds { get; set; } = 10;
}
