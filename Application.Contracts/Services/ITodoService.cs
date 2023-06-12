using Application.Contracts.Model;
using Package.Infrastructure.Data.Contracts;
using System;
using System.Threading.Tasks;

namespace Application.Contracts.Services;

public interface ITodoService
{
    Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 0);
    Task<TodoItemDto> GetItemAsync(Guid id);
    Task<TodoItemDto> AddItemAsync(TodoItemDto dto);
    Task<TodoItemDto?> UpdateItemAsync(TodoItemDto dto);
    Task DeleteItemAsync(Guid id);
    Task<PagedResponse<TodoItemDto>> GetPageExternalAsync(int pageSize = 10, int pageIndex = 0);
}
