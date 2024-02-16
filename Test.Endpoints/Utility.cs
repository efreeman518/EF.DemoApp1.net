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
    private static IConfigurationRoot? _config = null;
    private static readonly ConcurrentDictionary<string, IDisposable> _factories = new();

    public static IConfigurationRoot GetConfiguration()
    {
        if (_config != null) return _config;

        //order matters here (last wins)
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettingsPre.json", true) //test project settings
            .AddJsonFile("appsettings.json", false); //from api

        IConfigurationRoot config = builder.Build();
        string env = config.GetValue<string>("Environment", "Development")!;
        var isDevelopment = env?.ToLower() == "development";
        if (env != null)
        {
            builder.AddJsonFile($"appsettings.{env}.json", true);
        }
        builder
            .AddJsonFile("appsettingsPost.json", true) //test project override any api settings
            .AddEnvironmentVariables(); //pipeline can override settings

        if (isDevelopment) builder.AddUserSecrets<EndpointTestBase>();
        _config = builder.Build();

        return _config;
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

    private static SampleApiFactory<TEntryPoint> GetFactory<TEntryPoint>(string? factoryKey = null)
        where TEntryPoint : class
    {
        factoryKey ??= typeof(TEntryPoint).FullName!;
        if (_factories.TryGetValue(factoryKey, out var result)) return (SampleApiFactory<TEntryPoint>)result;
        var factory = new SampleApiFactory<TEntryPoint>(); //must live for duration of the client
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
