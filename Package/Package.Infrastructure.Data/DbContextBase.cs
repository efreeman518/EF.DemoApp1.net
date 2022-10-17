using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Package.Infrastructure.Data;

public abstract class DbContextBase : DbContext
{
    protected DbContextBase(DbContextOptions options) : base(options)
    {
    }

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

    public async Task<int> SaveChangesAsync(string auditId, bool acceptAllChangesOnSuccess = true, CancellationToken cancellationToken = default)
    {
        foreach (var entity in ChangeTracker.Entries<EntityBase>())
        {
            if (entity.State != EntityState.Added) continue;

            // Assign the entity Id if not already defined
            if (entity.Entity.Id == Guid.Empty)
            {
                entity.Entity.Id = Guid.NewGuid();
            }
        }

        foreach (var auditableEntity in ChangeTracker.Entries<IAuditable>())
        {
            if (auditableEntity.State == EntityState.Added ||
                auditableEntity.State == EntityState.Modified)
            {
                // modify updated audit columns.
                auditableEntity.Entity.UpdatedDate = DateTime.UtcNow;
                auditableEntity.Entity.UpdatedBy = auditId;

                // pupulate created columns for newly added record.
                if (auditableEntity.State == EntityState.Added)
                {
                    auditableEntity.Entity.CreatedDate = DateTime.UtcNow;
                    auditableEntity.Entity.CreatedBy = auditId;
                }
                else
                {
                    // we also want to make sure that code is not inadvertly modifying created columns 
                    auditableEntity.Property(t => t.CreatedDate).IsModified = false;
                    auditableEntity.Property(t => t.CreatedBy).IsModified = false;
                }
            }
        }
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
