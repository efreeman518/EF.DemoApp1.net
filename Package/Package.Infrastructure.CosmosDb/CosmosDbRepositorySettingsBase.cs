using Microsoft.Azure.Cosmos;

namespace Package.Infrastructure.CosmosDb;
public abstract class CosmosDbRepositorySettingsBase
{
    public CosmosClient CosmosClient { get; set; } = null!;
    public string CosmosDbId { get; set; } = null!;
}
