using Application.Contracts.Model;
using Package.Infrastructure.Data.Contracts;
using System.Threading;

namespace Infrastructure.Repositories;

public interface ITodoRepositoryQuery : IRepositoryBase
{
    Task<PagedResponse<TodoItemDto>> GetPageTodoItemDtoAsync(int pageSize, int pageIndex, CancellationToken cancellationToken = default);
}
