namespace Package.Infrastructure.Common.Contracts;

public class Sort(string propertyName, SortOrder sortOrder)
{
    public string PropertyName { get; set; } = propertyName;
    public SortOrder SortOrder { get; set; } = sortOrder;
}
