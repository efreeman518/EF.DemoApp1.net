namespace Package.Infrastructure.Table;

/// <summary>
/// Maps to Azure.Data.Tables so client does not need that reference
/// </summary>
public interface ITableEntity : Azure.Data.Tables.ITableEntity
{

}
