using Package.Infrastructure.Data.Contracts;

namespace Package.Infrastructure.CosmosDb;
public abstract class CosmosDbEntity : EntityBase
{
    //CosmosDB required
    public abstract string PartitionKey { get; }

#pragma warning disable IDE1006 // Naming Styles
    public string id => Id.ToString();
#pragma warning restore IDE1006 // Naming Styles

    //Audit fields?




}
