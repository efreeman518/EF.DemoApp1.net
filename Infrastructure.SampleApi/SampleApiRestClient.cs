using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Common.Extensions;

namespace Infrastructure.SampleApi;
public class SampleApiRestClient(ILogger<SampleApiRestClient> logger, IOptions<SampleApiRestClientSettings> settings, HttpClient httpClient) : ISampleApiRestClient
{
    private const string urlSegment = "todoitems";

    public async Task<Result<PagedResponse<TodoItemDto>?>> GetPageAsync(int pageSize = 10, int pageIndex = 1, CancellationToken cancellationToken = default)
    {
        _ = settings.GetHashCode();

        string path = $"{urlSegment}?pagesize={pageSize}&pageindex={pageIndex}";

        logger.LogInformation("SampleApiRestClient.GetPageAsync - {Url}", $"{httpClient.BaseAddress}{path}");

        (var _, Result<PagedResponse<TodoItemDto>?> result) = await httpClient.HttpRequestAndResponseResultAsync<PagedResponse<TodoItemDto>>(HttpMethod.Get, path, cancellationToken: cancellationToken);
        return result;
    }

    public async Task<TodoItemDto?> GetItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/{id}", cancellationToken: cancellationToken);
        return parsedResponse;
    }

    #region various security configurations

    //public async Task<TodoItemDto?> GetTodoItem_NoAttribute(Guid id, CancellationToken cancellationToken = default) //GetTodoItem_NoAttribute/
    //{
    //    (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/{id}", cancellationToken: cancellationToken);
    //    return parsedResponse;
    //}

    //public async Task<TodoItemDto?> GetTodoItem_Role_SomeAccess1(Guid id, CancellationToken cancellationToken = default)
    //{
    //    (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/GetTodoItem_Role_SomeAccess1/{id}", cancellationToken: cancellationToken);
    //    return parsedResponse;
    //}

    //public async Task<TodoItemDto?> GetTodoItem_Policy_AdminPolicy(Guid id, CancellationToken cancellationToken = default)
    //{
    //    (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/GetTodoItem_Policy_AdminPolicy/{id}", cancellationToken: cancellationToken);
    //    return parsedResponse;
    //}

    //public async Task<TodoItemDto?> GetTodoItem_Policy_SomeRolePolicy1(Guid id, CancellationToken cancellationToken = default)
    //{
    //    (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/GetTodoItem_Policy_SomeRolePolicy1/{id}", cancellationToken: cancellationToken);
    //    return parsedResponse;
    //}

    //public async Task<TodoItemDto?> GetTodoItem_Policy_SomeScopePolicy1(Guid id, CancellationToken cancellationToken = default)
    //{
    //    (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/GetTodoItem_Policy_SomeScopePolicy1/{id}", cancellationToken: cancellationToken);
    //    return parsedResponse;
    //}

    //public async Task<TodoItemDto?> GetTodoItem_Policy_ScopeOrRolePolicy1(Guid id, CancellationToken cancellationToken = default)
    //{
    //    (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/GetTodoItem_Policy_ScopeOrRolePolicy1/{id}", cancellationToken: cancellationToken);
    //    return parsedResponse;
    //}

    //public async Task<TodoItemDto?> GetTodoItem_Policy_ScopeOrRolePolicy2(Guid id, CancellationToken cancellationToken = default)
    //{
    //    (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/GetTodoItem_Policy_ScopeOrRolePolicy2/{id}", cancellationToken: cancellationToken);
    //    return parsedResponse;
    //}

    #endregion

    public async Task<Result<TodoItemDto?>> SaveItemAsync(TodoItemDto todo, CancellationToken cancellationToken = default)
    {
        HttpMethod httpMethod = todo.Id == null ? HttpMethod.Post : HttpMethod.Put;
        string idSegment = httpMethod == HttpMethod.Put ? $"/{todo.Id}" : ""; //PUT requires Id in the url
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<TodoItemDto>(httpMethod, $"{urlSegment}{idSegment}", todo, cancellationToken: cancellationToken);
        return parsedResponse;
    }

    public async Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        (var _, var _) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Delete, $"{urlSegment}/{id}", cancellationToken: cancellationToken);
    }

    public async Task<object?> GetUserAsync(CancellationToken cancellationToken = default)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getuser", cancellationToken: cancellationToken);
        return parsedResponse;
    }

    public async Task<object?> GetUserClaimsAsync(CancellationToken cancellationToken = default)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getuserclaims", cancellationToken: cancellationToken);
        return parsedResponse;
    }

    public async Task<object?> GetAuthHeaderAsync(CancellationToken cancellationToken = default)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getauthheader", cancellationToken: cancellationToken);
        return parsedResponse;
    }
}
