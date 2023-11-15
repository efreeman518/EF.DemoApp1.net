using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.CosmosDb;

namespace Package.Infrastructure.Test.Integration.Cosmos;
public class CosmosDbRepository1(ILogger<CosmosDbRepository1> logger, IOptions<CosmosDbRepositorySettings1> settings) :
    CosmosDbRepositoryBase(logger, settings), ICosmosDbRepository1
{
}
