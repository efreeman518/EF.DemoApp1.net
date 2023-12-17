using Application.Contracts.Model;
using Domain.Model;
using Domain.Shared.Enums;
using Package.Infrastructure.Data.Contracts;

namespace Application.Contracts.Interfaces;

public interface ITodoRepositoryQuery : IRepositoryBase
{
    IAsyncEnumerable<TodoItem> GetTodoItemsByStatus(TodoItemStatus status);
    Task<PagedResponse<TodoItemDto>> SearchTodoItemAsync(SearchRequest<TodoItemSearchFilter> request, CancellationToken cancellationToken = default);
}
