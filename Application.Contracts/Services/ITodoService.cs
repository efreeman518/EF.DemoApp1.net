using Application.Contracts.Model;
using System;
using System.Threading.Tasks;

namespace Application.Contracts.Services;

public interface ITodoService
{
    Task<PagedResponse<TodoItemDto>> GetItemsAsync(int pageSize = 10, int pageIndex = 0);
    Task<TodoItemDto> GetItemAsync(Guid id);
    Task<TodoItemDto> AddItemAsync(TodoItemDto dto);
    Task<TodoItemDto?> UpdateItemAsync(TodoItemDto dto);
    Task DeleteItemAsync(Guid id);
}
