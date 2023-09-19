using Application.Contracts.Model;
using Package.Infrastructure.Data.Contracts;

namespace Application.Contracts.Interfaces;

public interface ISampleApiRestClient
{
    Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 1);
    Task<TodoItemDto?> GetItemAsync(Guid id);
    Task<TodoItemDto?> SaveItemAsync(TodoItemDto todo);
    Task DeleteItemAsync(Guid id);
    Task<object?> GetUserAsync();
    Task<object?> GetUserClaimsAsync();
    Task<object?> GetAuthHeaderAsync();
}
