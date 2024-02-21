using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace Test.Endpoints;

/// <summary>
/// Uses WebApplicationFactory to create a test http service which can be hit using httpclient
/// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests
/// </summary>
public static class Utility
{
    public static readonly IConfigurationRoot Config = Config ?? BuildConfiguration();
    private static readonly ConcurrentDictionary<string, IDisposable> _factories = new();

    /// <summary>
    /// The api doesn't expose its configuration, so get it here if needed
    /// </summary>
    /// <returns></returns>
    public static IConfigurationRoot BuildConfiguration()
    {
        var builder = Support.Utility.BuildConfiguration();
        var config = builder.Build();
        string env = config.GetValue<string>("ASPNETCORE_ENVIRONMENT", "Development")!;
        var isDevelopment = env?.ToLower() == "development";
        if (isDevelopment) builder.AddUserSecrets<EndpointTestBase>();
        builder.AddJsonFile("appsettings-test.json", true); //test project settings file
        return builder.Build();
    }

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

    public static HttpClient GetClient<TEntryPoint>(string? factoryKey = null, bool allowAutoRedirect = true, string baseAddress = "http://localhost")
        where TEntryPoint : class
    {
        WebApplicationFactoryClientOptions options = new()
        {
            AllowAutoRedirect = allowAutoRedirect, //default = true; set to false for testing app's first response being a redirect with Location header
            BaseAddress = new Uri(baseAddress) //default = http://localhost
        };
        var factory = GetFactory<TEntryPoint>(factoryKey); //must live for duration of the client
        HttpClient client = factory.CreateClient(options);

        return client;
    }

    private readonly static object _lockFactories = new();
    private static SampleApiFactory<TEntryPoint> GetFactory<TEntryPoint>(string? factoryKey = null)
        where TEntryPoint : class
    {
        factoryKey ??= typeof(TEntryPoint).FullName!;
        SampleApiFactory<TEntryPoint> factory;
        if (_factories.TryGetValue(factoryKey, out var result)) return (SampleApiFactory<TEntryPoint>)result;

        lock (_lockFactories)
        {
            if (_factories.TryGetValue(factoryKey, out result))
                factory = (SampleApiFactory<TEntryPoint>)result;
            else
            {
                factory = new SampleApiFactory<TEntryPoint>(); //must live for duration of the client
                _factories.TryAdd(factoryKey, factory); //hold for subsequent use
            }
        }
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
