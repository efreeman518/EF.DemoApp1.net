using Application.Contracts.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Data.Contracts;
using System.Collections.Generic;

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

    public async Task<PagedResponse<TodoItemDto>> GetPage(int pageSize = 10, int pageIndex = 1)
    {
        _ = _logger.GetHashCode();
        _ = _settings.GetHashCode();
        string qs = $"?pagesize={pageSize}&pageindex={pageIndex}";
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<object, PagedResponse<TodoItemDto>>(HttpMethod.Get, $"{urlSegment}{qs}", null);
        return parsedResponse!;
    }

    public async Task<TodoItemDto?> GetTodoItem(Guid id)
    {
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<object, TodoItemDto>(HttpMethod.Get, $"{urlSegment}/{id}", null);
        return parsedResponse;
    }

    public async Task<TodoItemDto?> SaveEntity(TodoItemDto todo)
    {
        HttpMethod httpMethod = todo.Id == Guid.Empty ? HttpMethod.Post : HttpMethod.Put;
        (var _, var parsedResponse) = await _httpClient.HttpRequestAndResponseAsync<TodoItemDto, TodoItemDto>(httpMethod, $"{urlSegment}", todo);
        return parsedResponse;
    }

    public async Task DeleteEntity(Guid id)
    {
        (var _, var _) = await _httpClient.HttpRequestAndResponseAsync<object, TodoItemDto>(HttpMethod.Delete, $"{urlSegment}/{id}", null);
    }
}
