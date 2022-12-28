using Application.Contracts.Model;
using Package.Infrastructure.Data;
using Package.Infrastructure.Data.Contracts;
using System.Threading;

namespace Infrastructure.Repositories;

public interface ITodoRepository : IRepositoryTBase
{
    Task<PagedResponse<TodoItemDto>> GetDtosPagedAsync(int pageSize, int pageIndex, CancellationToken cancellationToken = default);
}
