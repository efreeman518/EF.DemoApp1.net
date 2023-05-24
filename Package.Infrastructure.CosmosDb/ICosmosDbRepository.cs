using Microsoft.Azure.Cosmos;
using Package.Infrastructure.Data.Contracts;
using System.Linq.Expressions;

namespace Package.Infrastructure.CosmosDb;
public interface ICosmosDbRepository
{
    Task<T> SaveItemAsync<T>(T item) where T : CosmosDbEntity;
    Task<T?> GetItemAsync<T>(string id, string partitionKey) where T : CosmosDbEntity;
    Task DeleteItemAsync<T>(string id, string partitionKey);
    Task DeleteItemAsync<T>(T item) where T : CosmosDbEntity;

    Task<(List<TProject>, int, string?)> GetPagedListAsync<TSource, TProject>(string? continuationToken = null,
        int pageSize = 10, Expression<Func<TProject, bool>>? filter = null,
        List<Sort>? sorts = null, bool includeTotal = false, int maxConcurrency = -1,
        CancellationToken cancellationToken = default);

    Task<(List<TProject>, int, string?)> GetPagedListAsync<TSource, TProject>(
        string? continuationToken = null, int pageSize = 10, string? sql = null, string? sqlCount = null,
        Dictionary<string, object>? parameters = null, int maxConcurrency = -1,
         CancellationToken cancellationToken = default);

    Task<Container> GetOrAddContainer(string containerId, string? partitionKeyPath = null, bool createIfNotExist = false);
}