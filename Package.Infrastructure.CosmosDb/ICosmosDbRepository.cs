using System.Linq.Expressions;

namespace Package.Infrastructure.CosmosDb;

public interface ICosmosDbRepository
{
    Task<List<T>> GetListAsync<T>(string query, string? continuationToken = null);
    Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> predicate, string? continuationToken = null, int maxConcurrency = -1);
    Task<T?> GetItemAsync<T>(string id, string partitionKey) where T : CosmosDbEntity;
    Task SaveItemAsync<T>(T item) where T : CosmosDbEntity;
    Task DeleteItemAsync<T>(string id, string partitionKey);
    Task DeleteItemAsync<T>(T item) where T : CosmosDbEntity;
}