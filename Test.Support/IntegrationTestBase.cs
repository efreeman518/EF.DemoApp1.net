using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common.Contracts;
using SampleApp.Bootstrapper;

namespace Test.Support;

/// <summary>
/// Testing Domain, Application, and Infrastructure services/logic; not http endpoints
/// MSTest Constructor (if defined) runs before each test
/// </summary>
public abstract class IntegrationTestBase
{
    protected readonly static IConfigurationRoot Config = Utility.BuildConfiguration().AddUserSecrets<IntegrationTestBase>().Build();
    protected readonly static IConfigurationSection TestConfigSection = Config.GetSection("TestSettings");
    //protected static IServiceProvider Services => _services;
    //protected static IServiceScope ServiceScope => _serviceScope;
    //protected static ILogger Logger => _logger;

    //MSTest requires static ClassInitialize/ClassCleanup methods which are used to initialize the DB
#pragma warning disable S2223 // Non-constant static fields should not be visible; 
#pragma warning disable CA2211 // Non-constant fields should not be visible
    protected static IServiceProvider Services = null!;
    protected static IServiceScope ServiceScope = null!;
    protected static ILogger<IntegrationTestBase> Logger = null!;
    protected static ServiceCollection ServicesCollection = [];
#pragma warning restore CA2211 // Non-constant fields should not be visible
#pragma warning restore S2223 // Non-constant static fields should not be visible

    /// <summary>
    /// Runs before each test
    /// </summary>
    protected IntegrationTestBase()
    {

    }

    /// <summary>
    /// Configure the test class; runs once before any test class [MSTest:ClassInitialize], [BenchmarkDotNet:GlobalSetup]
    /// </summary>
    /// <param name="testContextName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected static void ConfigureServices(string testContextName)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders().AddConsole().AddDebug().AddApplicationInsights();
        });
        ServicesCollection.AddSingleton(loggerFactory);

        //bootstrapper service registrations - infrastructure, domain, application 
        ServicesCollection
            .RegisterInfrastructureServices(Config)
            .RegisterBackgroundServices(Config)
            .RegisterDomainServices(Config)
            .RegisterApplicationServices(Config);

        //register services for testing that are not already registered in the bootstraper
        //ServicesCollection.AddAzureClients(builder =>
        //{
        //    //Azure OpenAI
        //    var configSection = Config.GetSection(JobChatSettings.ConfigSectionName);
        //    if (configSection.Exists())
        //    {
        //        // Register a custom client factory since this client does not currently have a service registration method
        //        builder.AddClient<AzureOpenAIClient, AzureOpenAIClientOptions>((options, _, _) =>
        //            new AzureOpenAIClient(new Uri(configSection.GetValue<string>("Url")!), new DefaultAzureCredential(), options));

        //        //AzureOpenAI chat service wrapper (not an Azure Client but a wrapper that uses it)
        //        ServicesCollection.AddTransient<IJobChatService, JobChatService>();
        //    }
        //});

        //moved to bootstrapper
        //FusionCache settings
        //List<CacheSettings> cacheSettings = [];
        //Config.GetSection("CacheSettings").Bind(cacheSettings);

        ////FusionCache supports multiple named instances with different default settings
        //foreach (var cacheInstance in cacheSettings)
        //{
        //    var fcBuilder = ServicesCollection.AddFusionCache(cacheInstance.Name)
        //    .WithCysharpMemoryPackSerializer() //FusionCache supports several different serializers (different FusionCache nugets)
        //    .WithCacheKeyPrefix($"{cacheInstance.Name}:")
        //    .WithDefaultEntryOptions(new FusionCacheEntryOptions()
        //    {
        //        //memory cache duration
        //        Duration = TimeSpan.FromMinutes(cacheInstance.DurationMinutes),
        //        //distributed cache duration
        //        DistributedCacheDuration = TimeSpan.FromMinutes(cacheInstance.DistributedCacheDurationMinutes),
        //        //how long to use expired cache value if the factory is unable to provide an updated value
        //        FailSafeMaxDuration = TimeSpan.FromMinutes(cacheInstance.FailSafeMaxDurationMinutes),
        //        //how long to wait before trying to get a new value from the factory after a fail-safe expiration
        //        FailSafeThrottleDuration = TimeSpan.FromSeconds(cacheInstance.FailSafeThrottleDurationMinutes),
        //        //allow some jitter in the Duration for variable expirations
        //        JitterMaxDuration = TimeSpan.FromSeconds(10),
        //        //factory timeout before returning stale value, if fail-safe is enabled and we have a stale value
        //        FactorySoftTimeout = TimeSpan.FromSeconds(1),
        //        //max allowed for factory even with no stale value to use; something may be wrong with the factory/service
        //        FactoryHardTimeout = TimeSpan.FromSeconds(30),
        //        //refresh active cache items upon cache retrieval, if getting close to expiration
        //        EagerRefreshThreshold = 0.9f
        //    });
        //    //using redis for L2 distributed cache
        //    if (!string.IsNullOrEmpty(cacheInstance.RedisName))
        //    {
        //        var connectionString = Config.GetConnectionString(cacheInstance.RedisName);
        //        fcBuilder
        //            .WithDistributedCache(new RedisCache(new RedisCacheOptions() { Configuration = connectionString }))
        //            //https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/Backplane.md#-wire-format-versioning
        //            .WithBackplane(new RedisBackplane(new RedisBackplaneOptions
        //            {
        //                Configuration = connectionString,
        //                ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
        //                {
        //                    ChannelPrefix = new StackExchange.Redis.RedisChannel(cacheInstance.BackplaneChannelName, StackExchange.Redis.RedisChannel.PatternMode.Auto)
        //                }
        //            }));
        //    }
        //}


        ServicesCollection.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug().AddApplicationInsights());
        Logger = ServicesCollection.BuildServiceProvider().GetRequiredService<ILogger<IntegrationTestBase>>();

        //IRequestContext - replace the Bootstrapper registered non-http 'BackgroundService' registration; injected into repositories
        ServicesCollection.AddTransient<IRequestContext<string>>(provider =>
        {
            var correlationId = Guid.NewGuid().ToString();
            return new RequestContext<string>(correlationId, $"Test.Support.IntegrationTestBase-{correlationId}");
        });

        //build IServiceProvider for subsequent use finding/injecting services
        Services = ServicesCollection.BuildServiceProvider(validateScopes: true);
        ServiceScope = Services.CreateScope();
        Logger.Log(LogLevel.Information, "{TestContextName} Base ConfigureServices complete.", testContextName);
    }

    //protected static void BuildServiceProviderAndScope()
    //{
    //    //build IServiceProvider for subsequent use finding/injecting services
    //    _services = ServicesCollection.BuildServiceProvider(validateScopes: true);
    //    _serviceScope = _services.CreateScope();
    //    _logger.Log(LogLevel.Information, "{TestContextName} Initialized.", testContextName);
    //}
}
