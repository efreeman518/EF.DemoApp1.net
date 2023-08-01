namespace Package.Infrastructure.Table;

/// <summary>
/// Maps to Azure.Data.Tables so client does not need a reference to Azure SDK
/// </summary>
public interface ITableEntity : Azure.Data.Tables.ITableEntity
{

}
