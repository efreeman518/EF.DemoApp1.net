namespace Package.Infrastructure.Table;

/// <summary>
/// Maps to Azure.Data.Tables so client does not need a reference to Azure SDK
/// </summary>
public enum TableUpdateMode
{
    /// <summary>
    /// Merge the properties of the supplied entity with the entity in the table.
    /// </summary>
    Merge = 0,

    /// <summary>
    /// Replace the entity in the table with the supplied entity.
    /// </summary>
    Replace = 1
}

