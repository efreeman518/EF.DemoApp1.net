using Application.Contracts.Model;
using Package.Infrastructure.Common.Contracts;

namespace Application.Contracts.Interfaces;

public interface ISampleApiRestClient
{
    Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 1, CancellationToken cancellationToken = default);
    Task<TodoItemDto?> GetItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TodoItemDto?> SaveItemAsync(TodoItemDto todo, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<object?> GetUserAsync(CancellationToken cancellationToken = default);
    Task<object?> GetUserClaimsAsync(CancellationToken cancellationToken = default);
    Task<object?> GetAuthHeaderAsync(CancellationToken cancellationToken = default);
}
