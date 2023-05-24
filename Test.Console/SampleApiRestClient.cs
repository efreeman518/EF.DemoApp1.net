using Application.Contracts.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;

namespace Test.Console;
internal class SampleApiRestClient
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

    public async Task<TodoItemDto?> GetTodoItem(Guid id)
    {
        _ = _logger.GetHashCode();
        _ = _settings.GetHashCode();

        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<object, TodoItemDto>(HttpMethod.Get, $"{urlSegment}/{id}", null);
        return parsedResponse;
    }

    public async Task<TodoItemDto?> SaveEntity(TodoItemDto todo)
    {
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<TodoItemDto, TodoItemDto>(HttpMethod.Post, $"{urlSegment}", todo);
        return parsedResponse;
    }

    public async Task DeleteEntity(Guid id)
    {
        (var _, var _) = await _httpClient.HttpRequestAndResponseAsync<object, TodoItemDto>(HttpMethod.Delete, $"{urlSegment}/{id}", null);
    }
}
