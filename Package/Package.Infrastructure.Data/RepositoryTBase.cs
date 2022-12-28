using Microsoft.EntityFrameworkCore;
using Package.Infrastructure.Data.Contracts;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Package.Infrastructure.Data;

public abstract class RepositoryTBase<TDbContext> : RepositoryQBase<TDbContext>, IRepositoryTBase where TDbContext : DbContextBase
{
    private readonly string _auditId;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="auditId"></param>
    protected RepositoryTBase(TDbContext dbContext, IAuditDetail audit) : base(dbContext)
    {
        DB = dbContext;
        _auditId = audit.AuditId;
    }

    public async Task<bool> Exists<T>(Expression<Func<T, bool>> filter) where T : class
    {
        return await DB.Set<T>().ExistsAsync(filter);
    }

    /// <summary>
    /// Create or UpdateFull entity based on the Id being a default value or not; need subsequent CommitAsync()
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public void Save<T>(ref T entity) where T : EntityBase
    {
        //dependent on Id for this logic; could also Upsert which checks db for a given Id
        if (entity.Id == Guid.Empty)
        {
            Create(ref entity);
        }
        else
        {
            UpdateFull(ref entity);
        }
    }

    /// <summary>
    /// Updates or inserts based on existence
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task Upsert<T>(T entity) where T : EntityBase
    {
        await DB.Upsert(entity);
    }

    /// <summary>
    /// Creates in the DbContext; inserts to DB upon CommitAsync()
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public void Create<T>(ref T entity) where T : class
    {
        DB.Create(ref entity);
    }

    /// <summary>
    /// Prepare to update only the properties specified (subsequently updated) upon CommitAsync(); not the entire row.
    /// </summary>
    /// <param name="update"></param>
    /// <returns></returns>
    public void PrepareForUpdate<T>(ref T entity) where T : EntityBase
    {
        //entity may already be attached so get that or create it in order to update
        DB.PrepareForUpdate<T>(ref entity);

    }

    /// <summary>
    /// IF entity is not already tracked (that will automatically update the row upon CommitAsync()) 
    /// Attaches and updates the entire row upon CommitAsync()
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public void UpdateFull<T>(ref T entity) where T : EntityBase
    {
        DB.UpdateFull<T>(ref entity);
    }

    /// <summary>
    /// Delete without loading first; entity must be populated with key value(s); need subsequent CommitAsync()
    /// </summary>
    /// <param name="entity"></param>
    public void Delete<T>(T entity) where T : EntityBase
    {
        //async - using key must await since retrieval from DB is required first; still requires subsequent CommitAsync()
        //await _dbContext.Delete<T>(entity.Id);

        //entity may already be attached so get that or create it in order to remove
        DB.Delete(entity);
    }

    /// <summary>
    /// Retrieve tracked or from DB based on keys; subsequent CommitAsync() will delete from DB
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="keys"></param>
    /// <returns></returns>
    public async Task Delete<T>(params object[] keys) where T : class
    {
        T? entity = await DB.Set<T>().FindAsync(keys);
        if (entity != null) DB.Set<T>().Remove(entity);
    }

    /// <summary>
    /// Retrieves List<TDbContext> based on filter and removes them from the context; subsequent CommitAsync() will delete from DB
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="filter"></param>
    public async Task Delete<T>(Expression<Func<T, bool>> filter) where T : class
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

    protected async Task ExecuteSqlCommandAsync(string sql)
    {
        await DB.Database.ExecuteSqlRawAsync(sql);
    }
}
