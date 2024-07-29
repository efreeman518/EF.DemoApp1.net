using Microsoft.EntityFrameworkCore.Query;
using Package.Infrastructure.Common.Contracts;
using System.Linq.Expressions;

namespace Package.Infrastructure.Data.Contracts;

public interface IRepositoryBase
{
    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> filter) where T : class;

    void Create<T>(ref T entity) where T : class;

    void PrepareForUpdate<T>(ref T entity) where T : EntityBase;

    void UpdateFull<T>(ref T entity) where T : EntityBase;

    void Delete<T>(T entity) where T : EntityBase;

    Task DeleteAsync<T>(CancellationToken cancellationToken = default, params object[] keys) where T : class;

    Task DeleteAsync<T>(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(OptimisticConcurrencyWinner winner, CancellationToken cancellationToken = default);

    Task<T?> GetEntityAsync<T>(bool tracking = false,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool splitQuery = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class;

    Task<PagedResponse<T>> QueryPageAsync<T>(bool readNoLock = true, bool tracking = false,
        int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool includeTotal = false, bool splitQuery = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class;

    Task<PagedResponse<TProject>> QueryPageProjectionAsync<T, TProject>(Expression<Func<T, TProject>> projector,
        bool readNoLock = true, int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool includeTotal = false, bool splitQuery = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class;

    IAsyncEnumerable<T> GetStream<T>(bool tracking = false, Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool splitQuery = false,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class;

    public IAsyncEnumerable<TProject> GetStreamProjection<T, TProject>(Func<T, TProject> projector,
        bool tracking = false, Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool splitQuery = false,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class;
}
