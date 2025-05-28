using Microsoft.EntityFrameworkCore;
using Package.Infrastructure.Common.Contracts;

namespace Package.Infrastructure.Data;
public class DbContextScopedFactory<TDbContext, TAuditIdType, TTenantIdType>(IDbContextFactory<TDbContext> pooledFactory,
    IRequestContext<TAuditIdType, TTenantIdType?> requestContext) : IDbContextFactory<TDbContext>
    where TDbContext : DbContextBase<TAuditIdType, TTenantIdType>
{
    private readonly IDbContextFactory<TDbContext> _pooledFactory = pooledFactory;
    private readonly TAuditIdType _auditId = requestContext.AuditId;
    private readonly TTenantIdType? _tenantId = requestContext.TenantId;

    public TDbContext CreateDbContext()
    {
        var context = _pooledFactory.CreateDbContext();
        context.AuditId = _auditId;
        context.TenantId = _tenantId;
        return context;
    }
}
