using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Package.Infrastructure.Common;
using Package.Infrastructure.Data.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Package.Infrastructure.Data;

public abstract class RepositoryBase<TDbContext> : IRepositoryBase where TDbContext : DbContextBase
{
    protected TDbContext DB;
    private readonly string _auditId;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="auditId"></param>
    protected RepositoryBase(TDbContext dbContext, ServiceRequestContext src)
    {
        DB = dbContext;
        _auditId = src.AuditId;
    }

    public async Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> filter) where T : class
    {
        return await DB.Set<T>().ExistsAsync(filter);
    }

    /// <summary>
    /// Updates or inserts based on existence
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task UpsertAsync<T>(T entity) where T : EntityBase
    {
        await DB.UpsertAsync(entity);
    }

    /// <summary>
    /// Creates in the DbContext; inserts to DB upon SaveChangesAsync()
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public void Create<T>(ref T entity) where T : class
    {
        DB.Create(ref entity);
    }

    /// <summary>
    /// Prepare to update only the properties specified (subsequently updated) upon SaveChangesAsync(); not the entire row.
    /// </summary>
    /// <param name="update"></param>
    /// <returns></returns>
    public void PrepareForUpdate<T>(ref T entity) where T : EntityBase
    {
        //entity may already be attached so get that or create it in order to update
        DB.PrepareForUpdate<T>(ref entity);

    }

    /// <summary>
    /// IF entity is not already tracked (that will automatically update the row upon SaveChangesAsync()) 
    /// Attaches and updates the entire row upon SaveChangesAsync()
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public void UpdateFull<T>(ref T entity) where T : EntityBase
    {
        DB.UpdateFull<T>(ref entity);
    }

    /// <summary>
    /// Delete without loading first; entity must be populated with key value(s); need subsequent SaveChangesAsync()
    /// </summary>
    /// <param name="entity"></param>
    public void Delete<T>(T entity) where T : EntityBase
    {
        //entity may already be attached so get that or create it in order to remove
        DB.Delete(entity);
    }

    /// <summary>
    /// Retrieve tracked or from DB based on keys; subsequent SaveChangesAsync() will delete from DB
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="keys"></param>
    /// <returns></returns>
    public async Task DeleteAsync<T>(params object[] keys) where T : class
    {
        T? entity = await DB.Set<T>().FindAsync(keys);
        if (entity != null) DB.Set<T>().Remove(entity);
    }

    /// <summary>
    /// Retrieves List<TDbContext> based on filter and removes them from the context; subsequent SaveChangesAsync() will delete from DB
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="filter"></param>
    public async Task DeleteAsync<T>(Expression<Func<T, bool>> filter) where T : class
    {
        await DB.Set<T>().DeleteAsync(filter);
    }

    /// <summary>
    /// Only use when OptimisticConcurrencyWinner not decided; could result in Exception 
    /// EF thinks data has changed between retrieval and commit
    /// </summary>
    /// <returns></returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await DB.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Forces commit based on OptimisticConcurrencyWinner specified 
    /// when EF thinks data has changed between retrieval and commit
    /// </summary>
    /// <param name="winner">server/client/throw</param>
    /// <returns></returns>
    public Task<int> SaveChangesAsync(OptimisticConcurrencyWinner winner, CancellationToken cancellationToken = default)
    {
        return DB.SaveChangesAsync(winner, _auditId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Set to false ONLY for bulk inserts with no navigation properties since EF will ignore navigation children
    /// Be sure to turn back on after the bulk insert
    /// </summary>
    /// <param name="value"></param>
    public void SetAutoDetectChanges(bool value)
    {
        DB.ChangeTracker.AutoDetectChangesEnabled = value;
    }

    /// <summary>
    /// Scans the tracked entity instances to detect any changes made to the instance data.
    /// Typically only need to call this method if you have disabled AutoDetectChangesEnabled
    /// </summary>
    public void DetectChanges()
    {
        DB.ChangeTracker.DetectChanges();
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
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        return await DB.Set<T>().GetEntityAsync<T>(tracking, filter, orderBy, cancellationToken, includes);
    }

    /// <summary>
    /// Returns a List<T> page of data with optional related data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tracking">DbContext will track changes (saves to db on SaveChangesAsync()) or not</param>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <param name="filter">Where clause (e => e.Property == somevalue) </param>
    /// <param name="orderBy">Order By clause</param>
    /// <param name="includes">get related data</param>
    /// <returns></returns>
    public async Task<PagedResponse<T>> GetPageEntitiesAsync<T>(bool tracking = false,
        int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool includeTotal = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        (List<T> data, int total) = await DB.Set<T>().GetPageEntitiesAsync(tracking, pageSize, pageIndex, filter, orderBy, includeTotal, cancellationToken, includes);
        return new PagedResponse<T>
        {
            PageSize = pageSize ?? -1,
            PageIndex = pageIndex ?? -1,
            Data = data,
            Total = total
        };
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
