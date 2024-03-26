using Microsoft.Azure.Cosmos;
using Package.Infrastructure.Common.Contracts;
using System.Linq.Expressions;
using System.Net;

namespace Package.Infrastructure.CosmosDb;
public interface ICosmosDbRepository
{
    Task<T> SaveItemAsync<T>(T item) where T : CosmosDbEntity;
    Task<T?> GetItemAsync<T>(string id, string partitionKey) where T : CosmosDbEntity;
    Task DeleteItemAsync<T>(string id, string partitionKey);
    Task DeleteItemAsync<T>(T item) where T : CosmosDbEntity;

    Task<(List<TProject>, int, string?)> QueryPageProjectionAsync<TSource, TProject>(string? continuationToken = null,
        int pageSize = 10, Expression<Func<TProject, bool>>? filter = null,
        List<Sort>? sorts = null, bool includeTotal = false, int maxConcurrency = -1,
        CancellationToken cancellationToken = default);

    Task<(List<TProject>, int, string?)> QueryPageProjectionAsync<TSource, TProject>(
        string? continuationToken = null, int pageSize = 10, string? sql = null, string? sqlCount = null,
        Dictionary<string, object>? parameters = null, int maxConcurrency = -1,
         CancellationToken cancellationToken = default);

    Task<Container> GetOrAddContainerAsync(string containerId, string? partitionKeyPath = null);
    Task<HttpStatusCode?> DeleteContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<HttpStatusCode> SetOrCreateDatabaseAsync(string dbId, int? throughput = null, CancellationToken cancellationToken = default);
    Task<HttpStatusCode> DeleteDatabaseAsync(string? dbId = null, CancellationToken cancellationToken = default);

}