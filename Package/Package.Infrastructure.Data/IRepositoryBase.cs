using System.Threading;
using System.Threading.Tasks;

namespace Package.Infrastructure.Data;

public interface IRepositoryBase
{
    Task<int> SaveChangesAsync(string auditId, CancellationToken cancellationToken = default);
}
