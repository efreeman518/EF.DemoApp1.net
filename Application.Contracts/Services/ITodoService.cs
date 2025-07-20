using Application.Contracts.Model;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Domain;

namespace Application.Contracts.Services;

public interface ITodoService
{
    Task<PagedResponse<TodoItemDto>> SearchAsync(SearchRequest<TodoItemSearchFilter> request, CancellationToken cancellationToken = default);
    Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default);
    Task<Result<TodoItemDto>> GetItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<TodoItemDto>> CreateItemAsync(TodoItemDto dto, CancellationToken cancellationToken = default);
    Task<Result<TodoItemDto>> UpdateItemAsync(TodoItemDto dto, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);
    //Task<Result<PagedResponse<TodoItemDto>?>> GetPageExternalAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default);
}
