using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Package.Infrastructure.Data.Contracts;
using System.Linq.Expressions;

namespace Package.Infrastructure.CosmosDb;

//https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.cosmosclient?view=azure-dotnet

/// <summary>
/// Uses the entity type T as the container name
/// </summary>
public class CosmosDbRepository : ICosmosDbRepository
{
    public readonly string? DbId;
    public readonly CosmosClient DbClient3;

    public CosmosDbRepository(CosmosDbRepositorySettings settings)
    {
        DbClient3 = settings.CosmosClient;
        DbId = settings.DbId;
    }

    public async Task<T> SaveItemAsync<T>(T item) where T : CosmosDbEntity
    {
        var itemResponse = await DbClient3.GetContainer(DbId, typeof(T).Name).UpsertItemAsync<T>(item, new PartitionKey(item.PartitionKey));
        return itemResponse.Resource;
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

    //using SDK3 for linq (GetItemLinqQueryable)
    //https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/query/linq-to-sql

    /// <summary>
    /// LINQ paged with filter and sort
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TProject"></typeparam>
    /// <param name="continuationToken"></param>
    /// <param name="pageSize"></param>
    /// <param name="filter"></param>
    /// <param name="sorts"></param>
    /// <param name="includeTotal">incurs potentially significant cost</param>
    /// <param name="maxConcurrency"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(List<TProject>, int, string?)> GetPagedListAsync<TSource, TProject>(string? continuationToken = null,
        int pageSize = 10, Expression<Func<TProject, bool>>? filter = null,
        List<Sort>? sorts = null, bool includeTotal = false, int maxConcurrency = -1, CancellationToken cancellationToken = default)
    {
        QueryRequestOptions o = new()
        {
            MaxItemCount = pageSize,
            MaxConcurrency = maxConcurrency //-1 system decides number of concurrent operations to run
        };

        var queryable = DbClient3.GetContainer(DbId, typeof(TSource).Name).GetItemLinqQueryable<TProject>(false, continuationToken, o);

        filter ??= i => true;
        var query = queryable.Where(filter);
        int total = includeTotal ? (await query.CountAsync(cancellationToken)).Resource : -1;
        if (sorts != null) query = query.OrderBy(sorts.AsEnumerable());
        List<TProject> items = new();

        using var feedIterator = query.ToFeedIterator();
        if (feedIterator.HasMoreResults) //Asynchronous query execution - loads to end; does not abide by MaxItemCount
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken); //this will load based on MaxItemCount/pageSize)
            continuationToken = response.ContinuationToken;
            foreach (var item in response)
            {
                items.Add(item);
            }
        }
        return (items, total, continuationToken);
    }

    /// <summary>
    /// SQL paged (sql can contain filter and sort)
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TProject"></typeparam>
    /// <param name="sql">SELECT... FROM... WHERE</param>
    /// <param name="parameters"></param>
    /// <param name="pageSize"></param>
    /// <param name="includeTotal">incurs potentially significant cost</param>
    /// <param name="maxConcurrency"></param>
    /// <param name="continuationToken"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(List<TProject>, int, string?)> GetPagedListAsync<TSource, TProject>(
        string? continuationToken = null, int pageSize = 10, string? sql = null,
        string? sqlCount = null, Dictionary<string, object>? parameters = null,
        int maxConcurrency = -1, CancellationToken cancellationToken = default)
    {
        _ = sql ?? throw new ArgumentNullException(nameof(sql));

        var query = BuildSqlQueryDefinition(sql, parameters);
        QueryRequestOptions o = new()
        {
            MaxItemCount = pageSize,
            MaxConcurrency = maxConcurrency //-1 system decides number of concurrent operations to run
        };
        List<TProject> items = new();

        using var feedIterator = DbClient3.GetContainer(DbId, typeof(TSource).Name).GetItemQueryIterator<TProject>(query, continuationToken, o);
        if (feedIterator.HasMoreResults) //Asynchronous query execution - loads to end; does not abide by MaxItemCount/pageSize
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken); //this will load based on MaxItemCount/pageSize)
            continuationToken = response.ContinuationToken;
            foreach (var item in response)
            {
                items.Add(item);
            }
        }

        int total = -1;
        if (sqlCount != null)
        {
            query = BuildSqlQueryDefinition(sqlCount, parameters);
            using var countIterator = DbClient3.GetContainer(DbId, typeof(TSource).Name).GetItemQueryIterator<int>(query);
            if (countIterator.HasMoreResults) //Asynchronous query execution - loads to end; does not abide by MaxItemCount/pageSize
            {
                var response = await countIterator.ReadNextAsync(cancellationToken);
                total = response.FirstOrDefault();
            }
        }
        return (items, total, continuationToken);
    }

    public async Task<Container> GetOrAddContainer(string containerId, string? partitionKeyPath = null, bool createIfNotExist = false)
    {
        ContainerProperties containerProperties = new(containerId, partitionKeyPath)
        {
            PartitionKeyDefinitionVersion = PartitionKeyDefinitionVersion.V2
        };

        var response = await DbClient3.GetDatabase(DbId).CreateContainerIfNotExistsAsync(containerProperties);
        var container = response.Container;
        return container!;
    }

    private static QueryDefinition BuildSqlQueryDefinition(string sql, Dictionary<string, object>? parameters = null)
    {
        QueryDefinition query = new(sql);
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                query.WithParameter(param.Key, param.Value);
            }
        }
        return query;
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
