using Application.Contracts.Model;
using LanguageExt.Common;
using Package.Infrastructure.Common.Contracts;

namespace Application.Contracts.Interfaces;

public interface ISampleApiRestClient
{
    Task<Result<PagedResponse<TodoItemDto>?>> GetPageAsync(int pageSize = 10, int pageIndex = 1, CancellationToken cancellationToken = default);
    Task<TodoItemDto?> GetItemAsync(Guid id, CancellationToken cancellationToken = default);

    #region various security configurations
    //Task<TodoItemDto?> GetTodoItem_NoAttribute(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Role_SomeAccess1(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Policy_AdminPolicy(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Policy_SomeRolePolicy1(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Policy_SomeScopePolicy1(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Policy_ScopeOrRolePolicy1(Guid id, CancellationToken cancellationToken = default);
    //Task<TodoItemDto?> GetTodoItem_Policy_ScopeOrRolePolicy2(Guid id, CancellationToken cancellationToken = default);
    #endregion

    Task<Result<TodoItemDto?>> SaveItemAsync(TodoItemDto todo, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<object?> GetUserAsync(CancellationToken cancellationToken = default);
    Task<object?> GetUserClaimsAsync(CancellationToken cancellationToken = default);
    Task<object?> GetAuthHeaderAsync(CancellationToken cancellationToken = default);
}
