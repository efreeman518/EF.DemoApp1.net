using Azure;
using Azure.Data.Tables;
using Package.Infrastructure.Data.Contracts;

namespace Package.Infrastructure.Table;

/// <summary>
/// Inheriting from EntityBase forces this package to a higher level of infrastructure packages
/// </summary>
public abstract class TableObject : EntityBase, ITableEntity
{
    //Table SDK required
    public abstract string PartitionKey { get; set; }
    public abstract string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; } 
    public ETag ETag { get; set; }
}
