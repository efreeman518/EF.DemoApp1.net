using Microsoft.EntityFrameworkCore;
using Package.Infrastructure.Common.Contracts;

namespace Infrastructure.Data;
public class TodoDbContextQueryScopedFactory(IDbContextFactory<TodoDbContextQuery> pooledFactory,
    IRequestContext<string, Guid?> requestContext) : IDbContextFactory<TodoDbContextQuery>
{
    private readonly IDbContextFactory<TodoDbContextQuery> _pooledFactory = pooledFactory;
    private readonly Guid? _tenantId = requestContext.TenantId;

    public TodoDbContextQuery CreateDbContext()
    {
        var context = _pooledFactory.CreateDbContext();
        context.TenantId = _tenantId;
        return context;
    }
}
