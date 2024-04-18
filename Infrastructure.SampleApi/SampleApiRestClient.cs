using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Common.Extensions;

namespace Infrastructure.SampleApi;
public class SampleApiRestClient(ILogger<SampleApiRestClient> logger, IOptions<SampleApiRestClientSettings> settings, HttpClient httpClient) : ISampleApiRestClient
{
    private const string urlSegment = "todoitems";

    public async Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 1, CancellationToken cancellationToken = default)
    {
        _ = settings.GetHashCode();

        string path = $"{urlSegment}?pagesize={pageSize}&pageindex={pageIndex}";

        logger.LogInformation("SampleApiRestClient.GetPageAsync - {Url}", $"{httpClient.BaseAddress}{path}");
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<PagedResponse<TodoItemDto>>(HttpMethod.Get, path, cancellationToken: cancellationToken);
        return parsedResponse!;
    }

    public async Task<TodoItemDto?> GetItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/{id}", cancellationToken: cancellationToken);
        return parsedResponse;
    }

    public async Task<TodoItemDto?> SaveItemAsync(TodoItemDto todo, CancellationToken cancellationToken = default)
    {
        HttpMethod httpMethod = todo.Id == null ? HttpMethod.Post : HttpMethod.Put;
        string idSegment = httpMethod == HttpMethod.Put ? $"/{todo.Id}" : ""; //PUT requires Id in the url
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseAsync<TodoItemDto>(httpMethod, $"{urlSegment}{idSegment}", todo, cancellationToken: cancellationToken);
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
