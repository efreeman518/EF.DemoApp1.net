using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Package.Infrastructure.Data.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Package.Infrastructure.Data;

public abstract class RepositoryQBase<TDbContext> : IRepositoryQBase where TDbContext : DbContextBase
{
    protected TDbContext DB;

    protected RepositoryQBase(TDbContext db)
    {
        DB = db;
    }

    /// <summary>
    /// Returns a the first T with optional related data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tracking">DbContext will track changes (saves to db on CommitAsync()) or not</param>
    /// <param name="filter">Where clause (e => e.Id == somevalue) </param>
    /// <param name="orderBy">If filter does not identity a unique entity, use this to select the first based on some order</param>
    /// <param name="includes">get related data</param>
    /// <returns></returns>
    public async Task<T?> GetItemAsync<T>(bool tracking = false,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        return await DB.Set<T>().GetItemAsync<T>(tracking, filter, orderBy, cancellationToken, includes);
    }

    /// <summary>
    /// Returns a List<T> page of data with optional related data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tracking">DbContext will track changes (saves to db on CommitAsync()) or not</param>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <param name="filter">Where clause (e => e.Property == somevalue) </param>
    /// <param name="orderBy">Order By clause</param>
    /// <param name="includes">get related data</param>
    /// <returns></returns>
    public async Task<PagedResponse<T>> GetPagedListAsync<T>(bool tracking = false,
        int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool includeTotal = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        (List<T> data, int total) = await DB.Set<T>().GetPagedListAsync(tracking, pageSize, pageIndex, filter, orderBy,includeTotal, cancellationToken, includes);
        return new PagedResponse<T> 
        { 
            PageSize = pageSize ?? -1, 
            PageIndex = pageIndex ?? -1,
            Data = data, 
            Total = total 
        };
    }

    /// <summary>
    /// Returns data and total count based on SearchRequest param 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<PagedResponse<T>> SearchAsync<T>(SearchRequest<T> request, 
        CancellationToken cancellationToken = default, params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes) where T : class
    {
        IQueryable<T> q = DB.Set<T>();
        q = q.ApplyFilters<T>(request.FilterItem);
        if (request.Sorts != null) q = q.OrderBy(request.Sorts);

        (List<T> data, int total) = await q.GetPagedListAsync(false, request.PageSize, request.PageIndex, null, null, true, cancellationToken, includes);

        PagedResponse<T> response = new()
        {
            PageSize = request.PageSize,
            PageIndex = request.PageIndex,
            Data = data,
            Total = total
        };

        return response;
    }

    /// <summary>
    /// Use only for queries with multi record results, and SetLock() after; do not use for inserts/updates/deletes
    /// </summary>
    /// <returns></returns>
    protected async Task SetNoLock()
    {
        await ExecuteSqlCommandAsync("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;");
    }

    protected async Task SetLock()
    {
        await ExecuteSqlCommandAsync("SET TRANSACTION ISOLATION LEVEL READ COMMITTED;");
    }

    private async Task ExecuteSqlCommandAsync(string sql)
    {
        await DB.Database.ExecuteSqlRawAsync(sql);
    }
}
