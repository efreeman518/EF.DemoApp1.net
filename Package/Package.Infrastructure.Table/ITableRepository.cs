using Azure.Data.Tables;
using System.Linq.Expressions;
using System.Net;

namespace Package.Infrastructure.Table;

public interface ITableRepository
{
    Task<T?> GetItemAsync<T>(string partitionKey, string rowkey, IEnumerable<string>? selectProps = null, CancellationToken cancellationToken = default)
        where T : class, ITableEntity;
    Task<HttpStatusCode> CreateItemAsync<T>(T item, CancellationToken cancellationToken = default)
        where T : ITableEntity;
    Task<HttpStatusCode> UpsertItemAsync<T>(T item, TableUpdateMode updateMode, CancellationToken cancellationToken = default)
        where T : ITableEntity;
    Task<HttpStatusCode> UpdateItemAsync<T>(T item, TableUpdateMode updateMode, CancellationToken cancellationToken = default)
        where T : ITableEntity;
    Task<HttpStatusCode> DeleteItemAsync<T>(string partitionKey, string rowkey, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<T>?, int, string?)> QueryAsync<T>(string? continuationToken = null, int pageSize = 10,
        Expression<Func<T, bool>>? filterLinq = null, string? filterOData = null, IEnumerable<string>? selectProps = null,
        bool includeTotal = false, CancellationToken cancellationToken = default)
        where T : class, ITableEntity;
    IAsyncEnumerable<T> GetStream<T>(Expression<Func<T, bool>>? filterLinq = null, string? filterOData = null,
        IEnumerable<string>? selectProps = null, CancellationToken cancellationToken = default)
        where T : class, ITableEntity;
    Task<TableClient> GetOrCreateTableAsync(string tableName, CancellationToken cancellationToken = default);
    Task<HttpStatusCode> DeleteTableAsync(string tableName, CancellationToken cancellationToken = default);
}
