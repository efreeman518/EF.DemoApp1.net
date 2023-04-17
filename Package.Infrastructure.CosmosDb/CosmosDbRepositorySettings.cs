using Microsoft.Azure.Cosmos;

namespace Package.Infrastructure.CosmosDb;
public class CosmosDbRepositorySettings
{
    public CosmosClient CosmosClient { get; set; } = null!;
    public string? DbId { get; set; }
}
