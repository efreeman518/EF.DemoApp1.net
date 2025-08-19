﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Package.Infrastructure.Data.Contracts;
using Package.Infrastructure.Domain;
using System.Linq.Expressions;

namespace Package.Infrastructure.Data;

/// <summary>
/// For detailed db exceptions, this library has been referenced: https://github.com/Giorgi/EntityFramework.Exceptions.Common
/// The transaction DbContextPool also has a reference to https://github.com/Giorgi/EntityFramework.Exceptions.SqlServer and is registered with .UseExceptionProcessor()
/// This allows for more detailed exceptions to be caught when saving changes in client code instead of investigating/parsing DBUpdateException inner exception details.
/// https://github.com/Giorgi/EntityFramework.Exceptions/blob/main/EntityFramework.Exceptions.Common/Exceptions.cs
/// </summary>
/// <param name="options"></param>
public abstract class DbContextBase<TAuditIdType, TTenantIdType>(DbContextOptions options) : DbContext(options)
{
    // AuditId set in the factory, used for auditing
    public required TAuditIdType AuditId { get; set; }

    //TenantId set in the factory, so it can be used in query filters
    public TTenantIdType? TenantId { get; set; }

    protected LambdaExpression BuildTenantFilter(Type entityType)
    {
        // e =>
        //     (this.TenantId == null)          // global admin: no filtering
        //     || (e.TenantId == this.TenantId) // tenant-scoped user
        var parameter = Expression.Parameter(entityType, "e");

        // entityTenant: e.TenantId
        var entityTenant = Expression.Property(parameter, nameof(TenantId));

        // this.TenantId
        var contextConst = Expression.Constant(this);
        var contextTenant = Expression.Property(contextConst, nameof(TenantId)); // type: TTenantIdType?

        // Normalize types for comparison (handle nullable/non-nullable)
        Expression left, right;
        if (entityTenant.Type != contextTenant.Type)
        {
            // Convert both to the underlying non-nullable type (e.g. Guid)
            var targetType = Nullable.GetUnderlyingType(entityTenant.Type)
                              ?? Nullable.GetUnderlyingType(contextTenant.Type)
                              ?? entityTenant.Type;

            left = entityTenant.Type == targetType ? entityTenant : Expression.Convert(entityTenant, targetType);
            right = contextTenant.Type == targetType ? contextTenant : Expression.Convert(contextTenant, targetType);
        }
        else
        {
            left = entityTenant;
            right = contextTenant;
        }

        var tenantEquals = Expression.Equal(left, right);

        // this.TenantId == null
        var contextTenantIsNull = Expression.Equal(
            contextTenant,
            Expression.Constant(null, contextTenant.Type)
        );

        // (this.TenantId == null) || (e.TenantId == this.TenantId)
        var body = Expression.OrElse(contextTenantIsNull, tenantEquals);

        return Expression.Lambda(body, parameter);
    }


    //OnConfiguring occurs last and can overwrite options obtained from DI or the constructor.
    //This approach does not lend itself to testing (unless you target the full database).
    //https://docs.microsoft.com/en-us/ef/core/miscellaneous/configuring-dbcontext

    public async override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
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
    public async Task<int> SaveChangesAsync(OptimisticConcurrencyWinner winner, bool acceptAllChangesOnSuccess = true, int concurrencyExceptionRetries = 3, CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        while (retryCount++ < concurrencyExceptionRetries)
        {
            try
            {
                // Attempt to save changes to the database
                return await SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (winner == OptimisticConcurrencyWinner.Throw) throw;

                foreach (var entry in ex.Entries)
                {
                    var proposedValues = entry.CurrentValues;
                    var dbValues = await entry.GetDatabaseValuesAsync(cancellationToken);
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

    public async override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
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

        //basic entity row audit;  Maybe outbox pattern as a better audit solution.
        foreach (var auditableEntity in ChangeTracker.Entries<IAuditable<TAuditIdType>>())
        {
            if (auditableEntity.State == EntityState.Added ||
                auditableEntity.State == EntityState.Modified)
            {
                //update audit preperties
                auditableEntity.Entity.UpdatedDate = DateTime.UtcNow;
                auditableEntity.Entity.UpdatedBy = AuditId;

                //populate created date and created by columns for newly added record.
                if (auditableEntity.State == EntityState.Added)
                {
                    auditableEntity.Entity.CreatedDate = DateTime.UtcNow;
                    auditableEntity.Entity.CreatedBy = AuditId;
                }
                else
                {
                    //make sure that code is not inadvertly modifying created date and created by columns 
                    auditableEntity.Property(t => t.CreatedDate).IsModified = false;
                    auditableEntity.Property(t => t.CreatedBy).IsModified = false;
                }
            }
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }
}
