using Refit;
using SampleApp.UI1.Model;

namespace SampleApp.UI1.Services;

public interface ISampleAppClient
{
    [Get("/api1/v1.1/todoitems?pagesize={pageSize}&pageindex={pageIndex}")]
    Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 1, CancellationToken cancellationToken = default);

    [Get("/api1/v1.1/todoitems/{id}")]
    Task<TodoItemDto> GetItemAsync(Guid id, CancellationToken cancellationToken = default);

    #region various security configurations
    //Task<TodoItemDto?> GetTodoItem_NoAttribute(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Role_SomeAccess1(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Policy_AdminPolicy(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Policy_SomeRolePolicy1(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Policy_SomeScopePolicy1(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Policy_ScopeOrRolePolicy1(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Policy_ScopeOrRolePolicy2(Guid id, CancellationToken cancellationToken = default);
    #endregion

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