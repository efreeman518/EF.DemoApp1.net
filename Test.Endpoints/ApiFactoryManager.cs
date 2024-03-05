using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Concurrent;

namespace Test.Endpoints;

/// <summary>
/// Uses WebApplicationFactory to create a test http service which can be hit using httpclient
/// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests
/// </summary>
public static class ApiFactoryManager
{
    //public static readonly IConfigurationRoot Config = Support.Utility.BuildConfiguration().AddUserSecrets<Program>().Build();
    private static readonly ConcurrentDictionary<string, IDisposable> _factories = new();

    public static async Task StartDbContainerAsync<TEntryPoint>(string? factoryKey = null) where TEntryPoint : class
    {
        var factory = GetFactory<TEntryPoint>(factoryKey); //must live for duration of the client
        await factory.StartDbContainer();
    }

    public static async Task StopDbContainerAsync<TEntryPoint>(string? factoryKey = null) where TEntryPoint : class
    {
        var factory = GetFactory<TEntryPoint>(factoryKey); //must live for duration of the client
        await factory.StopDbContainer();
    }

    public static async Task InitializeRespawnerAsync<TEntryPoint>(string? factoryKey = null) where TEntryPoint : class
    {
        var factory = GetFactory<TEntryPoint>(factoryKey); //must live for duration of the client
        await factory.InitializeRespawner();
    }

    public static async Task ResetDatabaseAsync<TEntryPoint>(string? factoryKey = null) where TEntryPoint : class
    {
        var factory = GetFactory<TEntryPoint>(factoryKey); //must live for duration of the client
        await factory.ResetDatabaseAsync();
    }

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

    private static CustomApiFactory<TEntryPoint> GetFactory<TEntryPoint>(string? factoryKey = null)
        where TEntryPoint : class
    {
        factoryKey ??= typeof(TEntryPoint).FullName!;
        CustomApiFactory<TEntryPoint> factory;
        if (_factories.TryGetValue(factoryKey, out var result)) return (CustomApiFactory<TEntryPoint>)result;

        factory = new CustomApiFactory<TEntryPoint>(); //must live for duration of the client
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
