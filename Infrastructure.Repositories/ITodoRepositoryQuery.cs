using Application.Contracts.Model;
using Package.Infrastructure.Data.Contracts;

namespace Infrastructure.Repositories;

public interface ITodoRepositoryQuery : IRepositoryBase
{
    Task<PagedResponse<TodoItemDto>> SearchTodoItemAsync(SearchRequest<TodoItemSearchFilter> request, CancellationToken cancellationToken = default);
}
