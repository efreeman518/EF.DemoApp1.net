using Application.Contracts.Model;
using Package.Infrastructure.Data.Contracts;

namespace Infrastructure.SampleApi;

public interface ISampleApiRestClient
{
    Task<PagedResponse<TodoItemDto>> GetPage(int pageSize = 10, int pageIndex = 1);
    Task<TodoItemDto?> GetTodoItem(Guid id);
    Task<TodoItemDto?> SaveTodoItem(TodoItemDto todo);
    Task DeleteTodoItem(Guid id);
    Task<object?> GetUser();
    Task<object?> GetUserClaims();
    Task<object?> GetAuthHeader();
}
