using Package.Infrastructure.Domain;

namespace Package.Infrastructure.CosmosDb;

/// <summary>
/// Inheriting from EntityBase forces this package to a higher level of infrastructure packages
/// </summary>
public abstract class CosmosDbEntity : EntityBase
{
    //CosmosDB required
    public abstract string PartitionKey { get; }

#pragma warning disable IDE1006 // Naming Styles
    public string id => Id.ToString();
#pragma warning restore IDE1006 // Naming Styles

    //Audit fields?

}
