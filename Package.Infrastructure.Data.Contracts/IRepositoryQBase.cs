using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Package.Infrastructure.Data.Contracts;

public interface IRepositoryQBase
{
    Task<T?> GetItemAsync<T>(bool tracking = false,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class;

    Task<PagedResponse<T>> GetPagedListAsync<T>(bool tracking = false,
        int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool includeTotal = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class;

    Task<PagedResponse<T>> SearchAsync<T>(SearchRequest<T> request,
        CancellationToken cancellationToken = default, params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes) where T : class;
}
