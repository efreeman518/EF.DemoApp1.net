using Application.Contracts.Model;
using LanguageExt;
using LanguageExt.Common;
using Package.Infrastructure.Common.Contracts;

namespace Application.Contracts.Services;

public interface ITodoService
{
    Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default);
    Task<Option<TodoItemDto>> GetItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<TodoItemDto>> CreateItemAsync(TodoItemDto dto, CancellationToken cancellationToken = default);
    Task<Result<TodoItemDto>> UpdateItemAsync(TodoItemDto dto, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResponse<TodoItemDto>?>> GetPageExternalAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default);
}
