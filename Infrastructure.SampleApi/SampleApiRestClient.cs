using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Data.Contracts;

namespace Infrastructure.SampleApi;
public class SampleApiRestClient : ISampleApiRestClient
{
    private readonly ILogger _logger;
    private readonly SampleApiRestClientSettings _settings;
    private readonly HttpClient _httpClient;
    private const string urlSegment = "todoitems";

    public SampleApiRestClient(ILogger<SampleApiRestClient> logger, IOptions<SampleApiRestClientSettings> settings, HttpClient httpClient)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClient;
    }

    public async Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 1)
    {
        _ = _logger.GetHashCode();
        _ = _settings.GetHashCode();

        string qs = $"?pagesize={pageSize}&pageindex={pageIndex}";
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<PagedResponse<TodoItemDto>>(HttpMethod.Get, $"{urlSegment}{qs}");
        return parsedResponse!;
    }

    public async Task<TodoItemDto?> GetItemAsync(Guid id)
    {
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/{id}", null);
        return parsedResponse;
    }

    public async Task<TodoItemDto?> SaveItemAsync(TodoItemDto todo)
    {
        HttpMethod httpMethod = todo.Id == Guid.Empty ? HttpMethod.Post : HttpMethod.Put;
        string idSegment = httpMethod == HttpMethod.Put ? $"/{todo.Id}" : ""; //PUT requires Id in the url
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<TodoItemDto>(httpMethod, $"{urlSegment}{idSegment}", todo);
        return parsedResponse;
    }

    public async Task DeleteItemAsync(Guid id)
    {
        (var _, var _) = await _httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Delete, $"{urlSegment}/{id}", null);
    }

    public async Task<object?> GetUserAsync()
    {
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getuser", null);
        return parsedResponse;
    }

    public async Task<object?> GetUserClaimsAsync()
    {
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getuserclaims", null);
        return parsedResponse;
    }

    public async Task<object?> GetAuthHeaderAsync()
    {
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getauthheader", null);
        return parsedResponse;
    }
}
