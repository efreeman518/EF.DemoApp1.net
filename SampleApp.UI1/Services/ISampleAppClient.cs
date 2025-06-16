using Package.Infrastructure.Common.Contracts;
using Refit;
using SampleApp.UI1.Model;

namespace SampleApp.UI1.Services;

public interface ISampleAppClient
{
    [Post("/api1/v1.1/todoitems/search")]
    Task<PagedResponse<TodoItemDto>> SearchAsync(SearchRequest<TodoItemSearchFilter> request, CancellationToken cancellationToken = default);

    [Get("/api1/v1.1/todoitems?pagesize={pageSize}&pageindex={pageIndex}")]
    Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 1, CancellationToken cancellationToken = default);

    [Get("/api1/v1.1/todoitems/{id}")]
    Task<TodoItemDto> GetItemAsync(Guid id, CancellationToken cancellationToken = default);

    [Post("/api1/v1.1/todoitems")]
    Task<TodoItemDto> CreateItemAsync(TodoItemDto todo, CancellationToken cancellationToken = default);

    [Put("/api1/v1.1/todoitems/{id}")]
    Task<TodoItemDto> UpdateItemAsync(Guid id, TodoItemDto todo, CancellationToken cancellationToken = default);

    [Delete("/api1/v1.1/todoitems/{id}")]
    Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);

    //Task<object?> GetUserAsync(CancellationToken cancellationToken = default);
    //Task<object?> GetUserClaimsAsync(CancellationToken cancellationToken = default);
    //Task<object?> GetAuthHeaderAsync(CancellationToken cancellationToken = default);
}