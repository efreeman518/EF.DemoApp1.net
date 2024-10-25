using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Package.Infrastructure.Data.Contracts;
using System.Linq.Expressions;

namespace Package.Infrastructure.Data;

/*
 * The extension methods wrap EF specific code and enable most repository methods to only have a single or few lines of code.
 * The methods' parameters can take lambdas for filter criteria (Where clause), Ordering, Includes (for any related entities), 
 * also paging parameters for the List methods.
 * 
 * Most of the extension methods have a ‘Tracking’ parameter – when true, the entity will be attached to the EF DbContext and 
 * any property changes will be saved to the DB upon SaveChangesAsync(). No need to call Update() or PrepareForMerge().
 * 
 * UpdateFull() - used when you have an untracked entity(not attached) that you decide you DO want to update in the DB.
 * Call UpdateFull(), then upon SaveChangesAsync() the entire record will be updated in the DB.
 * 
 * PrepareForUpdate() – used when the entity to be updated is not even loaded. This enables EF to send an update 
 * statement for specific property changes based on the entity’s primary key. If the key is known, create the object 
 * in code with the key, then call PrepareFoUpdate().  Any subsequent changes made to the entity’s properties will  
 * be included in an update statement by the entity’s primary key upon SaveChangesAsync().
 * 
 * Filter parameter structure:
 *    Expression<Func<Entity, bool>>? filter = null;
 *    if (someparameter != null) filter = t => t.Property == someparameter;
 * 
 * Include parameter structure: 
 *   List<Func<IQueryable<Entity>, IIncludableQueryable<Entity, object?>>> includes = new List<Func<IQueryable<Entity>, IIncludableQueryable<Entity, object?>>>(); 
 *   if (includeChildren) includes.Add(e1 => e1.Include(e => e.Children));
 */

public static class EFExtensions
{
    //DbContext Extensions

    /// <summary>
    /// If entity is already in the context (Entry) amd detached, then attach
    /// Otherwise add the entity to the DbSet; subsequent SaveChangesAsync() will insert the row in the DB
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="entity"></param>
    public static void Create<T>(this DbContext context, ref T entity) where T : class
    {
        EntityEntry<T> entry = context.Entry(entity);
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Added;
        }
        else
        {
            context.Set<T>().Add(entity);
        }
    }

    /// <summary>
    /// Returns a single T based on keys, optionally attach & track
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="keys"></param>
    /// <returns></returns>
    public static async Task<T?> GetByKeyAsync<T>(this DbContext context, bool tracking = false, CancellationToken cancellationToken = default, params object[] keys) where T : class
    {
        T? entity = await context.Set<T>().FindAsync(keys, cancellationToken).ConfigureAwait(false);
        if (entity != null && !tracking) context.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    /// <summary>
    /// Used when the entity to be updated is not loaded in the DbContext. 
    /// This enables EF to send an update statement for specific property changes based on the entity’s primary key.
    /// If the key is known, create the object in code with the key, then call PrepareForUpdate().  
    /// Any subsequent changes made to the entity’s properties will be included in an update statement based on
    /// the entity’s primary key upon SaveChangesAsync().
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static void PrepareForUpdate<T>(this DbContext context, ref T entity) where T : EntityBase
    {
        context.GetLocalOrAttach(ref entity); //Attaches if not tracked already, sets State = Unchanged
        context.Entry(entity).State = EntityState.Unchanged; //in case State is not Unchanged; update only subsequent changes
    }

    /// <summary>
    /// Used when you have an untracked entity(not attached) that you decide you DO want to update in the DB. 
    /// Call UpdateFull(), then upon SaveChangesAsync() the entire record will be updated in the DB.
    /// If already attached, this method is not needed - calling SaveChangesAsync() will update that record.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static void UpdateFull<T>(this DbContext context, ref T entity) where T : EntityBase
    {
        context.GetLocalOrAttach(ref entity); //Attaches if not tracked already, sets State = Unchanged
        context.Update(entity); //sets State = Modified, full record wil be updated
    }

    /// <summary>
    /// Inserts if not existing, otherwise ensure attached and set for Update
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="entity"></param>
    public static async Task UpsertAsync<T>(this DbContext context, T entity, CancellationToken cancellationToken = default) where T : EntityBase
    {
        if (!await context.Set<T>().AnyAsync(e => e.Id == entity.Id, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None))
            Create(context, ref entity);
        else
            UpdateFull(context, ref entity);
    }

    /// <summary>
    /// Delete an entity - subsequent SaveChangesAsync() will delete from DB
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="entity"></param>
    public static void Delete<T>(this DbContext context, T entity) where T : EntityBase
    {
        context.GetLocalOrAttach<T>(ref entity);
        context.Set<T>().Remove(entity);
    }

    /// <summary>
    /// Retrieve tracked or from DB based on keys; subsequent SaveChangesAsync() will delete from DB
    /// </summary>
    /// <param name="keys"></param>
    public static async Task DeleteAsync<T>(this DbContext context, CancellationToken cancellationToken = default, params object[] keys) where T : class
    {
        T? entity = await context.Set<T>().FindAsync(keys, cancellationToken).ConfigureAwait(false);
        if (entity != null) context.Set<T>().Remove(entity);
    }

    /// <summary>
    /// Using the entity, return false if already attached, 
    /// otherwise find in local DbSet<T> if exists (and Attach if necessary), otherwise Attach ref entity
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbContext"></param>
    /// <param name="entity"></param>
    /// <param name="replaceAttached">if already attached, replace with incoming entity</param>
    /// <returns>true if newly attached, otherwise false (already attached)</returns>
    public static bool GetLocalOrAttach<T>(this DbContext dbContext, ref T entity, bool replaceAttached = true) where T : EntityBase
    {
        //already attached 
        if (dbContext.Entry(entity).State != EntityState.Detached) return false;

        bool attach;
        Guid id = entity.Id;
        T? localEntity = dbContext.Set<T>().Local.FirstOrDefault(e => e.Id == id);
        if (localEntity != null) //already in local context
        {
            if (replaceAttached) //replace local existing with new incoming
            {
                dbContext.Entry(localEntity).State = EntityState.Detached;
                attach = true;
            }
            else
            {
                entity = localEntity; //keep local existing; ignore incoming
                attach = dbContext.Entry(entity).State == EntityState.Detached;
            }
        }
        else
        {
            attach = true;
        }
        if (attach) dbContext.Attach(entity); //sets State = Unchanged

        //note - if untracked then tracked (attached), and using a shadow property for RowVersion - need to load the RowVersion
        //otherwise use a regular property for RowVersion so it loads even when not tracked
        //var rowVersion = dbContext.Set<T>().Where(x => x.Id == id).Select(x => EF.Property<byte[]>(x, "RowVersion")).FirstOrDefault();

        return attach;
    }

    /// <summary>
    /// Audit helper method - Get all the changes for all the Modified entities in the context
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static List<EntityChangeInfo> GetAllEntityChanges(this DbContext context)
    {
        var changes = new List<EntityChangeInfo>();

        // Loop through all the tracked entities
        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Only process modified entities
            if (entry.State == EntityState.Modified)
            {
                var entityChangeInfo = GetEntityChanges(entry);

                if (entityChangeInfo.PropertyChanges.Count != 0)
                {
                    changes.Add(entityChangeInfo);
                }
            }
        }

        return changes;
    }

    //EntityEntry extensions

    /// <summary>
    /// Audit helper method - Get the changes for a specific entity
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    public static EntityChangeInfo GetEntityChanges(this EntityEntry entry, List<string>? maskedPropertyNames = null)
    {
        var entityChangeInfo = new EntityChangeInfo
        {
            EntityType = entry.Entity.GetType().Name,
            KeyValues = entry.GetPrimaryKeyValues(),
            PropertyChanges = []
        };

        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            var masked = maskedPropertyNames?.Contains(property.Metadata.Name) ?? false;
            entityChangeInfo.PropertyChanges.Add(new PropertyChangeInfo
            {
                PropertyName = property.Metadata.Name,
                OriginalValue = masked ? "***" : property.OriginalValue,
                NewValue = masked ? "***" : property.CurrentValue
            });
        }

        return entityChangeInfo;
    }

    /// <summary>
    /// Audit helper method - get the primary key values of an entity
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="whenNull">specify an optional default when key value is null</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Dictionary<string, object> GetPrimaryKeyValues(this EntityEntry entry, string? whenNull = null)
    {
        var keyValues = new Dictionary<string, object>();

        entry.Metadata.FindPrimaryKey()?.Properties.ToList().ForEach(kName =>
        {
            if (kName != null)
            {
                keyValues.Add(kName.Name, entry.Property(kName.Name).CurrentValue ?? whenNull ?? throw new InvalidOperationException($"Primary key value for '{kName}' is null."));
            }
        });

        return keyValues;
    }


    //DbSet Extensions

    /// <summary>
    /// Returns True if item exists based on the filter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static async Task<bool> ExistsAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) where T : class
    {
        return await dbSet.AnyAsync(filter, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// Returns true if item exists based on key values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbSet"></param>
    /// <param name="keys"></param>
    /// <returns></returns>
    public static async Task<bool> ExistsAsync<T>(this DbSet<T> dbSet, CancellationToken cancellationToken = default, params object[] keys) where T : class
    {
        return await dbSet.FindAsync(keys, cancellationToken).ConfigureAwait(false) != null;
    }

    /// <summary>
    /// Retrieves List<T> based on filter and removes them from the context; subsequent SaveChangesAsync() will delete from DB
    /// </summary>
    /// <param name="filter"></param>
    public static async Task DeleteAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) where T : class
    {
        var objects = (await QueryPageAsync(dbSet, false, null, null, filter, cancellationToken: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None)).Item1;
        //Parallel.ForEach(objects, o => { dbSet.Remove(o); });
        foreach (var item in objects)
        {
            dbSet.Remove(item);
        }
    }

    /// <summary>
    /// Some EF methods will fail (Attach or State change) if the entity is already in the DbContext
    /// Use this method to get that entity or use a new one (for PrepareForUpdate/Update, Delete 
    /// when entity does not need to be loaded first, but it may be already in the context)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="filter"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static T GetLocalOrCreateAndAttach<T>(this DbSet<T> dbSet, Guid id) where T : EntityBase
    {
        //context.Set<T>().Local.Any(e => e.Id == id);
        T? entity = dbSet.Local.FirstOrDefault(e => e.Id == id);

        if (entity == null)
        {
            entity = Activator.CreateInstance<T>();
            //entity.Id = id;
            dbSet.Attach(entity); //sets State = Unchanged
        }

        return entity;
    }

    /// <summary>
    /// Returns a single T based on keys, attached and tracked by the DbContext
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="keys"></param>
    /// <returns></returns>
    public static async Task<T?> GetByKeyAsync<T>(this DbSet<T> dbSet, CancellationToken cancellationToken = default, params object[] keys) where T : class
    {
        return await dbSet.FindAsync(keys, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns first T if exists, otherwise null 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbSet"></param>
    /// <param name="tracking"></param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="splitQuery"></param>Discretionary; avoid cartesian explosion, applicable with Includes; understand the risks/repercussions (when paging, etc) of using this https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
    /// <param name="includes"></param>
    /// <returns></returns>
    public static async Task<T?> GetEntityAsync<T>(this DbSet<T> dbSet, bool tracking,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool splitQuery = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        IQueryable<T> query = dbSet.ComposeIQueryable(tracking, null, null, filter, orderBy, splitQuery, includes);
        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// IQueryable<typeparamref name="T"/> extension takes the query, applies filter, order, paging, runs the query and returns results 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <param name="tracking"></param>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex">1-based</param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="includeTotal"></param>
    /// <param name="splitQuery"></param>Discretionary; avoid cartesian explosion, applicable with Includes; understand the risks/repercussions (when paging, etc) of using this https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
    /// <param name="cancellationToken"></param>
    /// <param name="includes"></param>
    /// <returns>IReadOnlyList<T> page results with total (-1 if includeTotal = false) </returns>
    public static async Task<(IReadOnlyList<T>, int)> QueryPageAsync<T>(this IQueryable<T> query, bool tracking = false,
        int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool includeTotal = false, bool splitQuery = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        int total = includeTotal ? await query.ComposeIQueryable(filter: filter).CountAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None) : -1;
        query = query.ComposeIQueryable(tracking, pageSize, pageIndex, filter, orderBy, splitQuery, includes);
        return ((await query.ToListAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None)).AsReadOnly(), total);
    }

    /// <summary>
    /// IQueryable<typeparamref name="T"/> extension takes the query, applies filter, order, paging, runs the query and returns results 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <param name="projector">Projection Func</param>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex">1-based</param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="includeTotal"></param>
    /// <param name="splitQuery"></param>Discretionary; avoid cartesian explosion, applicable with Includes; understand the risks/repercussions (when paging, etc) of using this https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
    /// <param name="cancellationToken"></param>
    /// <param name="includes"></param>
    /// <returns>List<TProject> page results with total (-1 if includeTotal = false) </returns>
    public static async Task<(IReadOnlyList<TProject>, int)> QueryPageProjectionAsync<T, TProject>(this IQueryable<T> query,
        Expression<Func<T, TProject>> projector,
        int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool includeTotal = false, bool splitQuery = false,
        CancellationToken cancellationToken = default,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        int total = includeTotal ? await query.ComposeIQueryable(filter: filter).CountAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None) : -1;
        query = query.ComposeIQueryable(false, pageSize, pageIndex, filter, orderBy, splitQuery, includes);
        var results = await query.Select(projector).ToListAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        return (results.AsReadOnly(), total);
    }

    /// <summary>
    /// Returns the count given the filter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbSet"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static async Task<long> GetCountAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default) where T : class
    {
        return await dbSet.ComposeIQueryable(filter: filter).CountAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// Helps creating many-to-many relationships when both sides already exist in the db
    /// http://stackoverflow.com/questions/14098600/many-to-many-relationships-not-saving
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbSet"></param>
    /// <param name="entity"></param>
    public static void Attach<T>(this DbSet<T> dbSet, T entity) where T : class
    {
        dbSet.Attach(entity);
    }
}
