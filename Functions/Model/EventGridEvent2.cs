namespace Functions.Model;

/// <summary>
/// needed when running isolated functions; underlying azure function runtime attempts to map 
/// </summary>
public class EventGridEvent2
{
    public string? Id { get; set; }

    public string? Topic { get; set; }

    public string? Subject { get; set; }

    public string? EventType { get; set; }

    public DateTime? EventTime { get; set; }

    public object? Data { get; set; }
}
