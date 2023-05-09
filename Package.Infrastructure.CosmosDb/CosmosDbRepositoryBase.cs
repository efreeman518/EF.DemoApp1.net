using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Linq.Expressions;

namespace Package.Infrastructure.CosmosDb;

//https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.cosmosclient?view=azure-dotnet
public abstract class CosmosDbRepositoryBase : ICosmosDbRepositoryBase
{
    public readonly string? DbId;
    public readonly CosmosClient DbClient3;

    protected CosmosDbRepositoryBase(CosmosDbRepositorySettings settings)
    {
        DbClient3 = settings.CosmosClient;
        DbId = settings.DbId;
    }

    public async Task SaveItemAsync<T>(T item) where T : CosmosDbEntity
    {
        await DbClient3.GetContainer(DbId, typeof(T).Name).UpsertItemAsync<T>(item, new PartitionKey(item.PartitionKey));
    }

    public async Task DeleteItemAsync<T>(string id, string partitionKey)
    {
        await DbClient3.GetContainer(DbId, typeof(T).Name).DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
    }

    public async Task DeleteItemAsync<T>(T item) where T : CosmosDbEntity
    {
        await DbClient3.GetContainer(DbId, typeof(T).Name).DeleteItemAsync<T>(item.id, new PartitionKey(item.PartitionKey));
    }

    public async Task<T?> GetItemAsync<T>(string id, string partitionKey) where T : CosmosDbEntity
    {
        try
        {
            ItemResponse<T> response = await DbClient3.GetContainer(DbId, typeof(T).Name).ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    //using SDK3 for linq
    //https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/query/linq-to-sql
    public async Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> predicate, string? continuationToken = null, int maxConcurrency = -1)
    {
        QueryRequestOptions o = new()
        {
            MaxConcurrency = maxConcurrency //system decides number of concurrent operations to run
        };

        FeedIterator<T> feedIterator = DbClient3.GetContainer(DbId, typeof(T).Name).GetItemLinqQueryable<T>(false, continuationToken, o)
            .Where(predicate)
            .ToFeedIterator();

        List<T> items = new();

        //Asynchronous query execution
        while (feedIterator.HasMoreResults)
        {
            foreach (var item in await feedIterator.ReadNextAsync())
            {
                items.Add(item);
            }
        }

        return items;
    }

    public async Task<List<T>> GetListAsync<T>(string query, string? continuationToken = null)
    {
        var feedIterator = DbClient3.GetContainer(DbId, typeof(T).Name).GetItemQueryIterator<T>(
            query,
            continuationToken);
        List<T> items = new();

        //Asynchronous query execution
        while (feedIterator.HasMoreResults)
        {
            foreach (var item in await feedIterator.ReadNextAsync())
            {
                items.Add(item);
            }
        }

        return items;
    }

    public async Task<Container> GetOrAddContainer(string containerId, string? partitionKeyPath = null, bool createIfNotExist = false)
    {
        Container container = DbClient3.GetContainer(DbId, containerId);
        if(container == null && createIfNotExist)
        {
            ContainerProperties containerProperties = new(containerId, partitionKeyPath)
            {
                PartitionKeyDefinitionVersion = PartitionKeyDefinitionVersion.V2
            };
            var response = await DbClient3.GetDatabase(DbId).CreateContainerIfNotExistsAsync(containerProperties);
            container = response.Container;
        }
        return container!;
    }

    //sdk4 ? no linq (yet)
    //public async Task<List<T>> GetListAsync<T>(string query)
    //{
    //    CosmosSDK4.CosmosContainer container = DbClient4.GetContainer(DbId, typeof(T).Name);
    //    Azure.Cosmos.QueryDefinition queryDefinition = new Azure.Cosmos.QueryDefinition(query);
    //    List<T> items = new List<T>();

    //    //Asynchronous query execution
    //    await foreach (T item in container.GetItemQueryIterator<T>(queryDefinition))
    //    {
    //        items.Add(item);
    //    }
    //    return items;
    //}
}
