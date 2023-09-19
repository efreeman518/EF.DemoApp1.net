using Application.Contracts.Model;
using Package.Infrastructure.Data.Contracts;

namespace Application.Contracts.Interfaces;

public interface ITodoRepositoryQuery : IRepositoryBase
{
    Task<PagedResponse<TodoItemDto>> SearchTodoItemAsync(SearchRequest<TodoItemSearchFilter> request, CancellationToken cancellationToken = default);
}
