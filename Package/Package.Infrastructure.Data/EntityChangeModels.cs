namespace Package.Infrastructure.Data;
// Class to represent changes for a specific entity
public class EntityChangeInfo
{
    public string EntityType { get; set; } = null!;
    public Dictionary<string, object> KeyValues { get; set; } = null!;
    public List<PropertyChangeInfo> PropertyChanges { get; set; } = [];
}

// Class to represent changes to a specific property
public class PropertyChangeInfo
{
    public string PropertyName { get; set; } = null!;
    public object? OriginalValue { get; set; }
    public object? NewValue { get; set; }
}
