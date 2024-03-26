using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Data.Contracts;
using System.Linq.Expressions;

namespace Package.Infrastructure.Data;

public abstract class RepositoryBase<TDbContext, TAuditIdType>(TDbContext dbContext, IRequestContext<TAuditIdType> requestContext)
    : IRepositoryBase where TDbContext : DbContextBase
{
    protected TDbContext DB => dbContext;
    private TAuditIdType AuditId => requestContext.AuditId;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="requestContext"></param>
    //protected RepositoryBase(TDbContext dbContext, IRequestContext requestContext)
    //{
    //    _auditId = requestContext.AuditId;
    //}

    public async Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> filter) where T : class
    {
        return await dbContext.Set<T>().ExistsAsync(filter).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// Updates or inserts based on existence
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task UpsertAsync<T>(T entity) where T : EntityBase
    {
        await dbContext.UpsertAsync(entity).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// Creates in the DbContext; inserts to DB upon SaveChangesAsync()
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public void Create<T>(ref T entity) where T : class
    {
        dbContext.Create(ref entity);
    }

    /// <summary>
    /// Prepare to update only the properties specified (subsequently updated) upon SaveChangesAsync(); not the entire row.
    /// </summary>
    /// <param name="update"></param>
    /// <returns></returns>
    public void PrepareForUpdate<T>(ref T entity) where T : EntityBase
    {
        //entity may already be attached so get that or create it in order to update
        dbContext.PrepareForUpdate<T>(ref entity);
    }

    /// <summary>
    /// Use when entity is not already tracked (that will automatically update the row upon SaveChangesAsync()) 
    /// Attaches and updates the entire row upon SaveChangesAsync()
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public void UpdateFull<T>(ref T entity) where T : EntityBase
    {
        dbContext.UpdateFull<T>(ref entity);
    }

    /// <summary>
    /// Delete without loading first; entity must be populated with key value(s); need subsequent SaveChangesAsync()
    /// </summary>
    /// <param name="entity"></param>
    public void Delete<T>(T entity) where T : EntityBase
    {
        //entity may already be attached so get that or create it in order to remove
        dbContext.Delete(entity);
    }

    /// <summary>
    /// Retrieve tracked or from DB based on keys; subsequent SaveChangesAsync() will delete from DB
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="keys"></param>
    /// <returns></returns>
    public async Task DeleteAsync<T>(CancellationToken cancellationToken = default, params object[] keys) where T : class
    {
        T? entity = await dbContext.Set<T>().FindAsync(keys, cancellationToken).ConfigureAwait(false);
        if (entity != null) dbContext.Set<T>().Remove(entity);
    }

    /// <summary>
    /// Retrieves List<TDbContext> based on filter and removes them from the context; subsequent SaveChangesAsync() will delete from DB
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="filter"></param>
    public async Task DeleteAsync<T>(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) where T : class
    {
        await dbContext.Set<T>().DeleteAsync(filter, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// Only use when OptimisticConcurrencyWinner not decided; could result in Exception 
    /// EF determines data has changed between retrieval and commit
    /// </summary>
    /// <returns></returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// Forces commit based on OptimisticConcurrencyWinner specified 
    /// when EF determines data has changed between retrieval and commit
    /// </summary>
    /// <param name="winner">server/client/throw</param>
    /// <returns></returns>
    public Task<int> SaveChangesAsync(OptimisticConcurrencyWinner winner, CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(winner, AuditId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Set to false ONLY for bulk inserts with no navigation properties since EF will ignore navigation children
    /// Be sure to turn back on after the bulk insert
    /// </summary>
    /// <param name="value"></param>
    public void SetAutoDetectChanges(bool value)
    {
        dbContext.ChangeTracker.AutoDetectChangesEnabled = value;
    }

    /// <summary>
    /// Scans the tracked entity instances to detect any changes made to the instance data.
    /// Typically only need to call this method if you have disabled AutoDetectChangesEnabled
    /// </summary>
    public void DetectChanges()
    {
        dbContext.ChangeTracker.DetectChanges();
    }

    /// <summary>
    /// Returns a the first T with optional related data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tracking">DbContext will track changes (saves to db on SaveChangesAsync()) or not</param>
    /// <param name="filter">Where clause (e => e.Id == somevalue) </param>
    /// <param name="orderBy">If filter does not identity a unique entity, use this to select the first based on some order</param>
    /// <param name="includes">get related data</param>
    /// <returns></returns>
    public async Task<T?> GetEntityAsync<T>(bool tracking = false,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool splitQuery = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        return await dbContext.Set<T>().GetEntityAsync<T>(tracking, filter, orderBy, splitQuery, cancellationToken, includes).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// Returns a List<T> page of data with optional related data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="readNoLock">Sets DB Isolation level to read uncommitted to prevent locks</param>
    /// <param name="tracking">DbContext will track changes (saves to db on SaveChangesAsync()) or not</param>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <param name="filter">Where clause (e => e.Property == somevalue) </param>
    /// <param name="orderBy">Order By clause</param>
    /// <param name="includes">get related data</param>
    /// <returns></returns>
    public async Task<PagedResponse<T>> QueryPageAsync<T>(bool readNoLock = true, bool tracking = false,
        int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool includeTotal = false, bool splitQuery = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        if (readNoLock) await SetNoLock().ConfigureAwait(ConfigureAwaitOptions.None);
        (IReadOnlyList<T> data, int total) = await dbContext.Set<T>()
            .QueryPageAsync(tracking, pageSize, pageIndex, filter, orderBy, includeTotal, splitQuery, cancellationToken, includes)
            .ConfigureAwait(ConfigureAwaitOptions.None);
        if (readNoLock) await SetLock().ConfigureAwait(ConfigureAwaitOptions.None);
        return new PagedResponse<T>
        {
            PageSize = pageSize ?? -1,
            PageIndex = pageIndex ?? -1,
            Data = data,
            Total = total
        };
    }

    /// <summary>
    /// Returns a List<TProject> page of data with optional related data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TProject"></typeparam>
    /// <param name="mapperConfigProvider"></param>
    /// <param name="readNoLock">Sets DB Isolation level to read uncommitted to prevent locks</param>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="includeTotal"></param>
    /// <param name="splitQuery"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public async Task<PagedResponse<TProject>> QueryPageProjectionAsync<T, TProject>(
        IConfigurationProvider mapperConfigProvider,
        bool readNoLock = true,
        int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool includeTotal = false, bool splitQuery = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        if (readNoLock) await SetNoLock().ConfigureAwait(ConfigureAwaitOptions.None);
        (IReadOnlyList<TProject> data, int total) = await dbContext.Set<T>().QueryPageProjectionAsync<T, TProject>(mapperConfigProvider,
            pageSize, pageIndex, filter, orderBy, includeTotal, splitQuery, cancellationToken, includes).ConfigureAwait(ConfigureAwaitOptions.None);
        if (readNoLock) await SetLock().ConfigureAwait(ConfigureAwaitOptions.None);
        return new PagedResponse<TProject>
        {
            PageSize = pageSize ?? -1,
            PageIndex = pageIndex ?? -1,
            Data = data,
            Total = total
        };
    }

    /// <summary>
    /// Return IAsyncEnumerable for streaming - await foreach (var x in GetStream<Entity>(...).WithCancellation(cancellationTokenSource.Token))
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tracking"></param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public IAsyncEnumerable<T> GetStream<T>(bool tracking = false, Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool splitQuery = false,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        return dbContext.Set<T>().GetStream(tracking, filter, orderBy, splitQuery, includes);
    }

    /// <summary>
    /// Return IAsyncEnumerable projection for streaming - await foreach (var x in GetStreamProjection<Entity, Dto>(...).WithCancellation(cancellationTokenSource.Token))
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TProject"></typeparam>
    /// <param name="mapperConfigProvider"></param>
    /// <param name="tracking"></param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public IAsyncEnumerable<TProject> GetStreamProjection<T, TProject>(IConfigurationProvider mapperConfigProvider,
        bool tracking = false, Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool splitQuery = false,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        return dbContext.Set<T>().GetStreamProjection<T, TProject>(mapperConfigProvider, tracking, filter, orderBy, splitQuery, includes);
    }

    /// <summary>
    /// Dirty reads for no lock multi record results; subsequent SetLock() after
    /// Use only for queries with multi record results, and SetLock() after; do not use for inserts/updates/deletes
    /// </summary>
    /// <returns></returns>
    protected async Task SetNoLock()
    {
        if (!dbContext.Database.IsSqlServer()) return; //InMemoryDbContext does not support
        await ExecuteSqlCommandAsync("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;").ConfigureAwait(ConfigureAwaitOptions.None);
    }

    protected async Task SetLock()
    {
        if (!dbContext.Database.IsSqlServer()) return; //InMemoryDbContext does not support
        await ExecuteSqlCommandAsync("SET TRANSACTION ISOLATION LEVEL READ COMMITTED;").ConfigureAwait(ConfigureAwaitOptions.None);
    }

    private async Task ExecuteSqlCommandAsync(string sql)
    {
        await dbContext.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(ConfigureAwaitOptions.None);
    }
}
