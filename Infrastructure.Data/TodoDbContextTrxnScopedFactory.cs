using Microsoft.EntityFrameworkCore;
using Package.Infrastructure.Common.Contracts;

namespace Infrastructure.Data;
public class TodoDbContextTrxnScopedFactory(IDbContextFactory<TodoDbContextTrxn> pooledFactory, DbContextOptions<TodoDbContextTrxn> options,
    IRequestContext<string, Guid?> requestContext) : IDbContextFactory<TodoDbContextTrxn>
{
    private readonly IDbContextFactory<TodoDbContextTrxn> _pooledFactory = pooledFactory;
    private readonly string _auditId = requestContext.AuditId;
    private readonly Guid? _tenantId = requestContext.TenantId;

    public TodoDbContextTrxn CreateDbContext()
    {
        var context = _pooledFactory.CreateDbContext();
        context.AuditId = _auditId;
        context.TenantId = _tenantId;
        return context;
    }
}
