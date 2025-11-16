using Package.Infrastructure.CosmosDb;

namespace Package.Infrastructure.Test.Integration.Cosmos;

public class CosmosDbRepositorySettings1 : CosmosDbRepositorySettingsBase
{
    public static string ConfigSectionName => "CosmosClient1";

    public CosmosDbRepositorySettings1() : base()
    {

    }
}
