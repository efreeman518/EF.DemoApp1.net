using Microsoft.Data.SqlClient;

namespace Package.Infrastructure.Data.Contracts;
public class Sort
{
    public string PropertyName { get; set; }

    public SortOrder SortOrder { get; set; }

    public Sort(string propertyName, SortOrder sortOrder)
    {
        PropertyName = propertyName;
        SortOrder = sortOrder;
    }
}
