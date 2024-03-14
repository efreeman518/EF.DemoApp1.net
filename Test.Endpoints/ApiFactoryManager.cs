using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Concurrent;

namespace Test.Endpoints;

/// <summary>
/// Uses WebApplicationFactory to create a test http service which can be hit using httpclient
/// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests
/// </summary>
public static class ApiFactoryManager
{
    private static readonly ConcurrentDictionary<string, IDisposable> _factories = new();

    public static HttpClient GetClient<TEntryPoint>(string? factoryKey = null, bool allowAutoRedirect = true, string baseAddress = "http://localhost")
        where TEntryPoint : class
    {
        WebApplicationFactoryClientOptions options = new()
        {
            AllowAutoRedirect = allowAutoRedirect, //default = true; set to false for testing app's first response being a redirect with Location header
            BaseAddress = new Uri(baseAddress) //default = http://localhost
        };
        var factory = GetFactory<TEntryPoint>(factoryKey); //must live for duration of the client
        HttpClient client = factory.CreateClient(options); //could reuse the client

        return client;
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
