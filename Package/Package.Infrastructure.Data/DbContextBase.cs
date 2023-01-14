using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Package.Infrastructure.Data.Contracts;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Package.Infrastructure.Data;

public abstract class DbContextBase : DbContext
{
    protected DbContextBase(DbContextOptions options) : base(options)
    {
    }

    //OnConfiguring occurs last and can overwrite options obtained from DI or the constructor.
    //This approach does not lend itself to testing (unless you target the full database).
    //https://docs.microsoft.com/en-us/ef/core/miscellaneous/configuring-dbcontext

    public async override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("SaveChangesAsync() overload with auditId must be used.");
    }

    public async override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("SaveChangesAsync() overload with auditId must be used.");

    }

    /// <summary>
    /// Transient error could show up as a concurreny exception
    /// </summary>
    /// <param name="winner"></param>
    /// <returns></returns>
    /// https://msdn.microsoft.com/en-us/data/jj592904.aspx
    public async Task<int> SaveChangesAsync(OptimisticConcurrencyWinner winner, string auditId, bool acceptAllChangesOnSuccess = true, CancellationToken cancellationToken = default)
    {
        int ret = 0;

        try
        {
            ret = await SaveChangesAsync(auditId, acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            //for a single record update, when the DB record has changed since last retrieval and subsequent update,
            //consider OptimisticConcurrencyWinner if specified
            if (ex.Entries.Count == 1 && winner != OptimisticConcurrencyWinner.Throw)
            {
                var entry = ex.Entries[0];

                if (winner == OptimisticConcurrencyWinner.DBWins)
                {
                    //cannot update a missing record
                    //RelationalStrings.UpdateConcurrencyException
                    if (ex.Message.StartsWith("The database operation was expected to affect 1 row(s), but actually affected 0 row(s); data may have been modified or deleted since entities were loaded"))
                    {
                        string? id = IsEntityEntryBaseDerived(entry, typeof(EntityBase)) ? ((EntityBase)entry.Entity).Id.ToString() : "";
                        throw new InvalidOperationException($"DB missing the row {id}; cannot update row where key does not exist; {ex.Message}", ex);
                    }
                    // Update the values of the entity that failed to save from the store 
                    await ex.Entries.Single().ReloadAsync(cancellationToken);
                }
                else if (winner == OptimisticConcurrencyWinner.ClientWins)
                {
                    // Update original values from the database 
                    try
                    {
                        var propValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                        if (propValues != null)
                        {
                            entry.OriginalValues.SetValues(propValues);
                        }
                        else
                        {
                            //cannot update a missing record
                            string? id = IsEntityEntryBaseDerived(entry, typeof(EntityBase)) ? ((EntityBase)entry.Entity).Id.ToString() : "";
                            throw new InvalidOperationException($"DB missing the row {id}; cannot update row where key does not exist; {ex.Message}", ex);
                        }
                    }
                    catch (ArgumentNullException ex1)
                    {
                        //cannot update a missing record
                        string? id = IsEntityEntryBaseDerived(entry, typeof(EntityBase)) ? ((EntityBase)entry.Entity).Id.ToString() : "";
                        throw new InvalidOperationException($"DB missing the row {id}; cannot update row where key does not exist; {ex1.Message}; {ex.Message}", ex1);
                    }
                }
            }
            else
            {
                throw;
            }
        }

        return ret;
    }

    private static bool IsEntityEntryBaseDerived(EntityEntry entry, Type typeBase)
    {
        return entry.Entity.GetType().IsSubclassOf(typeBase);
    }

    private async Task<int> SaveChangesAsync(string auditId, bool acceptAllChangesOnSuccess = true, CancellationToken cancellationToken = default)
    {
        foreach (var entity in ChangeTracker.Entries<EntityBase>())
        {
            //assign Id
            if (entity.State != EntityState.Added) continue;

            //check entity/properties for audit
            var auditChange = Attribute.GetCustomAttribute(entity.GetType(), typeof(AuditChangeAttribute));
            if (auditChange != null)
            {
                //TODO: add entries to audit table
            }
        }

        //basic entity row audit
        foreach (var auditableEntity in ChangeTracker.Entries<IAuditable>())
        {
            if (auditableEntity.State == EntityState.Added ||
                auditableEntity.State == EntityState.Modified)
            {
                // modify updated date and updated by column for 
                // adds of updates.
                auditableEntity.Entity.UpdatedDate = DateTime.UtcNow;
                auditableEntity.Entity.UpdatedBy = auditId;

                // pupulate created date and created by columns for
                // newly added record.
                if (auditableEntity.State == EntityState.Added)
                {
                    auditableEntity.Entity.CreatedDate = DateTime.UtcNow;
                    auditableEntity.Entity.CreatedBy = auditId;
                }
                else
                {
                    // we also want to make sure that code is not inadvertly
                    // modifying created date and created by columns 
                    auditableEntity.Property(t => t.CreatedDate).IsModified = false;
                    auditableEntity.Property(t => t.CreatedBy).IsModified = false;
                }
            }
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
