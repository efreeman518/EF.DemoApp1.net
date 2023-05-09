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

    Task<(List<T>, string?)> GetPagedListAsync<T>(string? continuationToken = null, int pageSize = 10,
        Expression<Func<T, bool>>? filter = null,
        List<Sort>? sorts = null,
        int maxConcurrency = -1) where T : CosmosDbEntity;
    Task<(List<T>, string?)> GetPagedListAsync<T>(
        string sql, Dictionary<string, object>? parameters = null,
        string? continuationToken = null, int pageSize = 10,
        int maxConcurrency = -1) where T : CosmosDbEntity;

    Task<Container> GetOrAddContainer(string containerId, string? partitionKeyPath = null, bool createIfNotExist = false);
}