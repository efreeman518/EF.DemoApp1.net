using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Data.Contracts;

namespace Infrastructure.SampleApi;
public class SampleApiRestClient(ILogger<SampleApiRestClient> logger, IOptions<SampleApiRestClientSettings> settings, HttpClient httpClient) : ISampleApiRestClient
{
    private const string urlSegment = "todoitems";

    public async Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 1)
    {
        _ = settings.GetHashCode();

        string path = $"{urlSegment}?pagesize={pageSize}&pageindex={pageIndex}";

        logger.LogInformation("SampleApiRestClient.GetPageAsync - {Url}", $"{httpClient.BaseAddress}{path}");
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<PagedResponse<TodoItemDto>>(HttpMethod.Get, path);
        return parsedResponse!;
    }

    public async Task<TodoItemDto?> GetItemAsync(Guid id)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/{id}", null);
        return parsedResponse;
    }

    public async Task<TodoItemDto?> SaveItemAsync(TodoItemDto todo)
    {
        HttpMethod httpMethod = todo.Id == Guid.Empty ? HttpMethod.Post : HttpMethod.Put;
        string idSegment = httpMethod == HttpMethod.Put ? $"/{todo.Id}" : ""; //PUT requires Id in the url
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(httpMethod, $"{urlSegment}{idSegment}", todo);
        return parsedResponse;
    }

    public async Task DeleteItemAsync(Guid id)
    {
        (var _, var _) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Delete, $"{urlSegment}/{id}", null);
    }

    public async Task<object?> GetUserAsync()
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getuser", null);
        return parsedResponse;
    }

    public async Task<object?> GetUserClaimsAsync()
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getuserclaims", null);
        return parsedResponse;
    }

    public async Task<object?> GetAuthHeaderAsync()
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getauthheader", null);
        return parsedResponse;
    }
}
