using System.Threading;
using System.Threading.Tasks;

namespace Package.Infrastructure.Data;

public abstract class RepositoryBase<T> : IRepositoryBase where T : DbContextBase
{
    protected T DB;

    protected RepositoryBase(T dbContext)
    {
        DB = dbContext;
    }

    public async Task<int> SaveChangesAsync(string auditId, CancellationToken cancellationToken = default)
    {
        return await DB.SaveChangesAsync(auditId, true, cancellationToken);
    }
}
