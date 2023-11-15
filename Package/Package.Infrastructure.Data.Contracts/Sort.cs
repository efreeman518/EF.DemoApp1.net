using Microsoft.Data.SqlClient;

namespace Package.Infrastructure.Data.Contracts;
public class Sort(string propertyName, SortOrder sortOrder)
{
    public string PropertyName { get; set; } = propertyName;
    public SortOrder SortOrder { get; set; } = sortOrder;
}
