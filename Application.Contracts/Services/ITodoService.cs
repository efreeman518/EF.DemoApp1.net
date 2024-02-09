using Application.Contracts.Model;
using LanguageExt.Common;
using Package.Infrastructure.Data.Contracts;

namespace Application.Contracts.Services;

public interface ITodoService
{
    Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 0);
    Task<TodoItemDto?> GetItemAsync(Guid id);
    Task<Result<TodoItemDto?>> AddItemAsync(TodoItemDto dto);
    Task<Result<TodoItemDto?>> UpdateItemAsync(TodoItemDto dto);
    Task DeleteItemAsync(Guid id);
    Task<PagedResponse<TodoItemDto>> GetPageExternalAsync(int pageSize = 10, int pageIndex = 0);
}
