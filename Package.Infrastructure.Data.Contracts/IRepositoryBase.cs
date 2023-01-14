using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Package.Infrastructure.Data.Contracts;

public interface IRepositoryBase
{
    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> filter) where T : class;

    void Create<T>(ref T entity) where T : class;

    void PrepareForUpdate<T>(ref T entity) where T : EntityBase;

    void UpdateFull<T>(ref T entity) where T : EntityBase;

    void Delete<T>(T entity) where T : EntityBase;

    Task DeleteAsync<T>(params object[] keys) where T : class;

    Task DeleteAsync<T>(Expression<Func<T, bool>> filter) where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(OptimisticConcurrencyWinner winner, CancellationToken cancellationToken = default);

    Task<T?> GetEntityAsync<T>(bool tracking = false,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class;

    Task<PagedResponse<T>> GetPageEntitiesAsync<T>(bool tracking = false,
        int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool includeTotal = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class;
}
