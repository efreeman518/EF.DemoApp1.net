using Application.Contracts.Model;
using Package.Infrastructure.Data.Contracts;
using System.Threading;

namespace Infrastructure.Repositories;

public interface ITodoRepositoryQuery : IRepositoryBase
{
    Task<PagedResponse<TodoItemDto>> SearchTodoItemAsync(SearchRequest<TodoItemSearchFilter> request, CancellationToken cancellationToken = default);
}
