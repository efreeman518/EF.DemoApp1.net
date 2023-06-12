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

    public async Task<PagedResponse<TodoItemDto>> GetPage(int pageSize = 10, int pageIndex = 1)
    {
        _ = _logger.GetHashCode();
        _ = _settings.GetHashCode();

        string qs = $"?pagesize={pageSize}&pageindex={pageIndex}";
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<PagedResponse<TodoItemDto>>(HttpMethod.Get, $"{urlSegment}{qs}");
        return parsedResponse!;
    }

    public async Task<TodoItemDto?> GetTodoItem(Guid id)
    {
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlSegment}/{id}", null);
        return parsedResponse;
    }

    public async Task<TodoItemDto?> SaveTodoItem(TodoItemDto todo)
    {
        HttpMethod httpMethod = todo.Id == Guid.Empty ? HttpMethod.Post : HttpMethod.Put;
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<TodoItemDto>(httpMethod, $"{urlSegment}", todo);
        return parsedResponse;
    }

    public async Task DeleteTodoItem(Guid id)
    {
        (var _, var _) = await _httpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Delete, $"{urlSegment}/{id}", null);
    }

    public async Task<object?> GetUser()
    {
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getuser", null);
        return parsedResponse;
    }

    public async Task<object?> GetUserClaims()
    {
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getuserclaims", null);
        return parsedResponse;
    }

    public async Task<object?> GetAuthHeader()
    {
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Get, $"{urlSegment}/getauthheader", null);
        return parsedResponse;
    }
}
