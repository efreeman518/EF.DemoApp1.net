using LazyCache;
using Microsoft.AspNetCore.Mvc.Testing;
using Package.Infrastructure.Auth.Tokens;
using System.Collections.Concurrent;

namespace Test.Endpoints;

/// <summary>
/// Uses WebApplicationFactory to create a test http service which can be hit using httpclient
/// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests
/// </summary>
public static class ApiFactoryManager
{
    private static readonly ConcurrentDictionary<string, IDisposable> _factories = new();
    private static readonly IAppCache _appcache = new CachingService();

    public static async Task<HttpClient> GetClientAsync<TEntryPoint>(string? factoryKey = null, bool allowAutoRedirect = true, string baseAddress = "https://localhost:443",
        string? dbConnectionString = null, string? tokenResourceId = null, params DelegatingHandler[] handlers)
        where TEntryPoint : class
    {
        var uri = new Uri(baseAddress);
        WebApplicationFactoryClientOptions options = new()
        {
            AllowAutoRedirect = allowAutoRedirect, //default = true; set to false for testing app's first response being a redirect with Location header
            BaseAddress = uri
        };

        var factory = GetFactory<TEntryPoint>(factoryKey, dbConnectionString); //must live for duration of the client
        HttpClient httpClient = (handlers.Length > 0)
            ? factory.CreateDefaultClient(uri, handlers)
            : factory.CreateClient(options);

        if (tokenResourceId != null)
        {
            await httpClient.ApplyBearerAuthHeaderAsync(tokenResourceId);
        }
        return httpClient;
    }

    /// <summary>
    /// Apply auth if api is secured; not needed if httpclient has an auth handler already
    /// </summary>
    /// <returns></returns>
    private static async Task ApplyBearerAuthHeaderAsync(this HttpClient httpClient, string tokenResourceId)
    {
        var tokenProvider = new AzureDefaultCredTokenProvider(_appcache);
        var token = await tokenProvider.GetAccessTokenAsync(tokenResourceId);
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private static CustomApiFactory<TEntryPoint> GetFactory<TEntryPoint>(string? factoryKey = null, string? dbConnectionString = null)
        where TEntryPoint : class
    {
        factoryKey ??= typeof(TEntryPoint).FullName!;
        CustomApiFactory<TEntryPoint> factory;
        if (_factories.TryGetValue(factoryKey, out var result)) return (CustomApiFactory<TEntryPoint>)result;

        factory = new CustomApiFactory<TEntryPoint>(dbConnectionString); //must live for duration of the client
        _factories.TryAdd(factoryKey, factory); //hold for subsequent use
        return factory;
    }

    public static void Cleanup<TEntryPoint>(string? factoryKey = null)
    {
        factoryKey ??= typeof(TEntryPoint).FullName!;
        if (_factories.TryGetValue(factoryKey, out var _))
        {
            _factories[factoryKey].Dispose();
            _factories.TryRemove(factoryKey, out _);
        }
    }
}
