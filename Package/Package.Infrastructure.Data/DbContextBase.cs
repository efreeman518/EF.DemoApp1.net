using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Package.Infrastructure.Data.Contracts;

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


    /// https://msdn.microsoft.com/en-us/data/jj592904.aspx
    /// 
    /// <summary>
    /// Transient error could show up as a concurreny exception
    /// </summary>
    /// <param name="winner"></param>
    /// <param name="auditId"></param>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <param name="concurrencyExceptionRetries"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="DbUpdateConcurrencyException"></exception>
    public async Task<int> SaveChangesAsync(OptimisticConcurrencyWinner winner, string auditId, bool acceptAllChangesOnSuccess = true, int concurrencyExceptionRetries = 3, CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        while (retryCount++ < concurrencyExceptionRetries)
        {
            try
            {
                // Attempt to save changes to the database
                return await SaveChangesAsync(auditId, acceptAllChangesOnSuccess, cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (winner == OptimisticConcurrencyWinner.Throw) throw;

                foreach (var entry in ex.Entries)
                {
                    var proposedValues = entry.CurrentValues;
                    var dbValues = entry.GetDatabaseValues();
                    _ = dbValues ?? throw new InvalidOperationException("DbUpdateConcurrencyException retry attempted to retieve DB values that returned null.");

                    foreach (var property in proposedValues.Properties)
                    {
                        var proposedValue = proposedValues[property];
                        var dbValue = dbValues[property];
                        proposedValues[property] = (winner == OptimisticConcurrencyWinner.ClientWins) ? proposedValue : dbValue;
                    }

                    // Refresh original values to bypass next concurrency check
                    entry.OriginalValues.SetValues(dbValues);
                }
            }
        }

        throw new DbUpdateConcurrencyException($"DbUpdateConcurrencyException retry limit reached, unable to save {winner}");
    }

    //private static bool IsEntityEntryBaseDerived(EntityEntry entry, Type typeBase)
    //{
    //    return entry.Entity.GetType().IsSubclassOf(typeBase);
    //}

    private async Task<int> SaveChangesAsync(string auditId, bool acceptAllChangesOnSuccess = true, CancellationToken cancellationToken = default)
    {
        //Audit table tracking option - create audit records alternative to audit properties on the entity
        foreach (var entity in ChangeTracker.Entries<EntityBase>())
        {
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
                //update audit preperties
                auditableEntity.Entity.UpdatedDate = DateTime.UtcNow;
                auditableEntity.Entity.UpdatedBy = auditId;

                //populate created date and created by columns for newly added record.
                if (auditableEntity.State == EntityState.Added)
                {
                    auditableEntity.Entity.CreatedDate = DateTime.UtcNow;
                    auditableEntity.Entity.CreatedBy = auditId;
                }
                else
                {
                    //make sure that code is not inadvertly modifying created date and created by columns 
                    auditableEntity.Property(t => t.CreatedDate).IsModified = false;
                    auditableEntity.Property(t => t.CreatedBy).IsModified = false;
                }
            }
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
