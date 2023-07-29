using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.CosmosDb;

namespace Package.Infrastructure.Test.Integration.Cosmos;
public class CosmosDbRepository1 : CosmosDbRepositoryBase, ICosmosDbRepository1
{
    public CosmosDbRepository1(ILogger<CosmosDbRepository1> logger, IOptions<CosmosDbRepositorySettings1> settings) 
        : base(logger, settings.Value.CosmosClient, settings.Value.CosmosDbId)
    {
            
    }
}
