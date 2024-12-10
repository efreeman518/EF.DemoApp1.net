using Application.Contracts.Interfaces;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Infrastructure.RapidApi.WeatherApi;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Cache;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Test.Integration.AzureAIChat;
using Package.Infrastructure.Test.Integration.Blob;
using Package.Infrastructure.Test.Integration.Cosmos;
using Package.Infrastructure.Test.Integration.KeyVault;
using Package.Infrastructure.Test.Integration.Messaging;
using Package.Infrastructure.Test.Integration.Service;
using Package.Infrastructure.Test.Integration.Table;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace Package.Infrastructure.Test.Integration;

[TestClass]
public abstract class IntegrationTestBase
{
    protected readonly IConfiguration Config;
    protected readonly IServiceProvider Services;
    protected readonly ILogger<IntegrationTestBase> Logger;

    //[AssemblyInitialize]
    //public static void Initialize(TestContext ctx) //ctx required for [AssemblyInitialize] run 
    //{ }

    protected IntegrationTestBase()
    {
        //Configuration
        Config = Utility.BuildConfiguration<IntegrationTestBase>();

        //DI
        ServiceCollection services = new();

        //add logging for integration tests
        services.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug());

        //queued background service - fire and forget 
        services.AddHostedService<BackgroundTaskService>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        //IConfigurationSection configSection;

        //Azure Service Clients - Blob, EventGridPublisher, KeyVault, etc; enables injecting IAzureClientFactory<>
        //https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection
        //https://devblogs.microsoft.com/azure-sdk/lifetime-management-and-thread-safety-guarantees-of-azure-sdk-net-clients/
        //https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.azure.azureclientfactorybuilder?view=azure-dotnet
        //https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Microsoft.Extensions.Azure/1.0.0/index.html
        services.AddAzureClients(builder =>
        {
            // Set up any default settings
            builder.ConfigureDefaults(Config.GetSection("AzureClientDefaults"));
            // Use DefaultAzureCredential by default
            builder.UseCredential(new DefaultAzureCredential());

            //Azure storage generating SAS tokens require a StorageSharedKeyCredential or the managed identity to have permissions to create SAS tokens.
            //StorageSharedKeyCredential storageSharedKeyCredential = new(accountName, accountKey);
            //https://learn.microsoft.com/en-us/azure/storage/common/storage-sas-overview
            var blobConfigSection = Config.GetSection("ConnectionStrings:AzureBlobStorageAccount1");
            if (blobConfigSection.Exists())
            {
                //Ideally use ServiceUri (w/DefaultAzureCredential)
                builder.AddBlobServiceClient(blobConfigSection).WithName("AzureBlobStorageAccount1");
            }

            //Table 
            var tableConfigSection = Config.GetSection("ConnectionStrings:AzureTable1");
            if (tableConfigSection.Exists())
            {
                //Ideally use ServiceUri (w/DefaultAzureCredential)
                builder.AddTableServiceClient(tableConfigSection).WithName("AzureTable1");
            }

            //EventGrid Publisher
            var egpConfigSection = Config.GetSection("EventGridPublisherTopic1");
            if (egpConfigSection.Exists())
            {
                //Ideally use TopicEndpoint Uri only (DefaultAzureCredential defined above for all azure clients)
                builder.AddEventGridPublisherClient(new Uri(egpConfigSection.GetValue<string>("TopicEndpoint")!),
                    new AzureKeyCredential(egpConfigSection.GetValue<string>("Key")!))
                .WithName("EventGridPublisherTopic1");
            }

            //KeyVault
            var kvConfigSection = Config.GetSection(KeyVaultManager1Settings.ConfigSectionName);
            if (kvConfigSection.Exists())
            {
                var akvUrl = kvConfigSection.GetValue<string>("VaultUrl")!;
                var name = kvConfigSection.GetValue<string>("KeyVaultClientName")!;
                builder.AddSecretClient(new Uri(akvUrl)).WithName(name);
                builder.AddKeyClient(new Uri(akvUrl)).WithName(name);
                builder.AddCertificateClient(new Uri(akvUrl)).WithName(name);

                //wrapper for key vault sdks
                services.Configure<KeyVaultManager1Settings>(kvConfigSection);
                services.AddSingleton<IKeyVaultManager1, KeyVaultManager1>();
            }

            //Azure OpenAI
            var jobChatConfigSection = Config.GetSection(JobChatSettings.ConfigSectionName);
            if (jobChatConfigSection.Exists())
            {
                // Register a custom client factory since this client does not currently have a service registration method
                builder.AddClient<AzureOpenAIClient, AzureOpenAIClientOptions>((options, _, _) =>
                {
                    var key = jobChatConfigSection.GetValue<string?>("Key", null);
                    if (!string.IsNullOrEmpty(key))
                    {
                        return new AzureOpenAIClient(new Uri(jobChatConfigSection.GetValue<string>("Url")!), new AzureKeyCredential(key), options);
                    }

                    //this throws internally when running local (no network for managed identity check) but subsequent checks succeed; could avoid with defaultAzCredOptions.ExcludeManagedIdentityCredential = true;
                    return new AzureOpenAIClient(new Uri(jobChatConfigSection.GetValue<string>("Url")!), new DefaultAzureCredential(), options);
                });

                //AzureOpenAI chat service wrapper (not an Azure Client but a wrapper that uses it)
                services.AddTransient<IJobChatService, JobChatService>();
                services.Configure<JobChatSettings>(jobChatConfigSection);
            }
        });

        //BlobRepository
        var blobRepoConfigSection = Config.GetSection(BlobRepositorySettings1.ConfigSectionName);
        if (blobRepoConfigSection.Exists())
        {
            services.AddSingleton<IBlobRepository1, BlobRepository1>();
            services.Configure<BlobRepositorySettings1>(blobRepoConfigSection);
        }

        //TableRepository
        var tableRepoConfigSection = Config.GetSection(TableRepositorySettings1.ConfigSectionName);
        if (tableRepoConfigSection.Exists())
        {
            services.AddSingleton<ITableRepository1, TableRepository1>();
            services.Configure<TableRepositorySettings1>(tableRepoConfigSection);
        }

        //EventGridPublisher
        var egpConfigSection = Config.GetSection(EventGridPublisherSettings1.ConfigSectionName);
        if (egpConfigSection.Exists())
        {
            services.AddSingleton<IEventGridPublisher1, EventGridPublisher1>();
            services.Configure<EventGridPublisherSettings1>(egpConfigSection);
        }

        //CosmosDb - CosmosClient is thread-safe. Its recommended to maintain a single instance of CosmosClient per lifetime of the application which enables efficient connection management and performance.
        var cosmosConnectionString = Config.GetConnectionString("CosmosClient1");
        if (!string.IsNullOrEmpty(cosmosConnectionString))
        {
            var cosmosConfigSection = Config.GetSection(CosmosDbRepositorySettings1.ConfigSectionName);
            if (cosmosConfigSection.Exists())
            {
                services.AddTransient<ICosmosDbRepository1, CosmosDbRepository1>();
                services.Configure<CosmosDbRepositorySettings1>(s =>
                {
                    s.CosmosClient = new CosmosClientBuilder(cosmosConnectionString) //(AccountEndpoint, DefualtAzureCredential())
                                                                                     //.With...options
                        .Build();
                    s.CosmosDbId = cosmosConfigSection.GetValue<string>("CosmosDbId")!;
                });
            }
        }

        //LazyCache.AspNetCore, lightweight wrapper around memorycache; prevent race conditions when multiple threads attempt to refresh empty cache item
        //https://github.com/alastairtree/LazyCache
        //services.AddLazyCache();

        //Redis distributed cache
        //cache providers - https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-5.0#multiple-cache-providers
        //multiple redis instances requires a different pattern - https://stackoverflow.com/questions/71329765/how-to-use-multiple-implementations-of-microsoft-extensions-caching-stackexchang
        //connectionString = Config.GetConnectionString("Redis1");
        //if (!string.IsNullOrEmpty(connectionString))
        //{
        //    services.AddStackExchangeRedisCache(options =>
        //    {
        //        options.Configuration = connectionString;
        //        options.InstanceName = "redis1";
        //    });
        //}
        //else
        //{
        //    services.AddDistributedMemoryCache(); //local server only, not distributed. Helps with tests
        //}

        //distributed cache manager
        //services.AddScoped<IDistributedCacheManager, DistributedCacheManager>();


        //FusionCache settings
        List<CacheSettings> cacheSettings = [];
        Config.GetSection("CacheSettings").Bind(cacheSettings);

        //FusionCache supports multiple named instances with different default settings
        foreach (var cacheInstance in cacheSettings)
        {
            var fcBuilder = services.AddFusionCache(cacheInstance.Name)
            .WithCysharpMemoryPackSerializer() //FusionCache supports several different serializers (different FusionCache nugets)
            .WithCacheKeyPrefix($"{cacheInstance.Name}:")
            .WithDefaultEntryOptions(new FusionCacheEntryOptions()
            {
                //memory cache duration
                Duration = TimeSpan.FromMinutes(cacheInstance.DurationMinutes),
                //distributed cache duration
                DistributedCacheDuration = TimeSpan.FromMinutes(cacheInstance.DistributedCacheDurationMinutes),
                //how long to use expired cache value if the factory is unable to provide an updated value
                FailSafeMaxDuration = TimeSpan.FromMinutes(cacheInstance.FailSafeMaxDurationMinutes),
                //how long to wait before trying to get a new value from the factory after a fail-safe expiration
                FailSafeThrottleDuration = TimeSpan.FromSeconds(cacheInstance.FailSafeThrottleDurationMinutes),
                //allow some jitter in the Duration for variable expirations
                JitterMaxDuration = TimeSpan.FromSeconds(10),
                //factory timeout before returning stale value, if fail-safe is enabled and we have a stale value
                FactorySoftTimeout = TimeSpan.FromSeconds(1),
                //max allowed for factory even with no stale value to use; something may be wrong with the factory/service
                FactoryHardTimeout = TimeSpan.FromSeconds(30),
                //refresh active cache items upon cache retrieval, if getting close to expiration
                EagerRefreshThreshold = 0.9f
            });

            //using redis for L2 distributed cache & backplane
            //ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis
            //https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/Backplane.md#-wire-format-versioning
            //https://markmcgookin.com/2020/01/15/azure-redis-cache-no-endpoints-specified-error-in-dotnet-core/
            //Redis Settings - Data Access Configuration requires the identities to have 'Data Contributor' access policy for subscribing on the backchannel
            if (!string.IsNullOrEmpty(cacheInstance.RedisConfigurationSection))
            {
                RedisConfiguration redisConfigFusion = new();
                Config.GetSection(cacheInstance.RedisConfigurationSection).Bind(redisConfigFusion);
                var redisConfigurationOptions = new ConfigurationOptions
                {
                    EndPoints =
                    {
                        {
                            redisConfigFusion.EndpointUrl,
                            redisConfigFusion.Port
                        }
                    },
                    Ssl = true,
                    AbortOnConnectFail = false
                };
                if (cacheInstance.BackplaneChannelName != null)
                {
                    redisConfigurationOptions.ChannelPrefix = new RedisChannel(cacheInstance.BackplaneChannelName, RedisChannel.PatternMode.Auto);
                }

                if (!string.IsNullOrEmpty(redisConfigFusion.Password))
                {
                    redisConfigurationOptions.Password = redisConfigFusion.Password;
                }
                else
                {
                    //configure redis with managed identity
                    _ = Task.Run(() => redisConfigurationOptions.ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential())).Result;
                }

                //var connectionString = config.GetConnectionString(cacheInstance.RedisName);
                fcBuilder
                    .WithDistributedCache(new RedisCache(new RedisCacheOptions()
                    {
                        //Configuration = connectionString,  
                        ConfigurationOptions = redisConfigurationOptions
                    }))

                    .WithBackplane(new RedisBackplane(new RedisBackplaneOptions
                    {
                        //Configuration = connectionString,
                        ConfigurationOptions = redisConfigurationOptions
                    }));
            }

        }

        //IRequestContext - injected into repositories, cache managers, etc
        services.AddScoped<IRequestContext<string>>(provider =>
        {
            return new RequestContext<string>(Guid.NewGuid().ToString(), "IntegrationTest", "SomeTenantId");
        });

        //external weather service
        var weatherConfigSection = Config.GetSection(WeatherServiceSettings.ConfigSectionName);
        if (weatherConfigSection.Exists())
        {
            services.Configure<WeatherServiceSettings>(weatherConfigSection);
            services.AddScoped<IWeatherService, WeatherService>();
            services.AddHttpClient<IWeatherService, WeatherService>(client =>
            {
                client.BaseAddress = new Uri(Config.GetValue<string>("WeatherServiceSettings:BaseUrl")!);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Key", Config.GetValue<string>("WeatherServiceSettings:Key")!);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Host", Config.GetValue<string>("WeatherServiceSettings:Host")!);
            })
            //resiliency
            //.AddPolicyHandler(PollyRetry.GetHttpRetryPolicy())
            //.AddPolicyHandler(PollyRetry.GetHttpCircuitBreakerPolicy());
            //Microsoft.Extensions.Http.Resilience - https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience?tabs=dotnet-cli
            .AddStandardResilienceHandler();
        }

        //OpenAI chat service wrapper
        var oaiChatConfigSection = Config.GetSection(OpenAI.ChatApi.ChatServiceSettings.ConfigSectionName);
        if (oaiChatConfigSection.Exists())
        {
            services.AddTransient<OpenAI.ChatApi.IChatService, OpenAI.ChatApi.ChatService>();
            services.Configure<OpenAI.ChatApi.ChatServiceSettings>(oaiChatConfigSection);
        }

        //Sample scoped service for testing BackgroundTaskQueue.QueueScopedBackgroundWorkItem
        services.AddScoped<ISomeScopedService, SomeScopedService>();

        services.AddLogging(configure => configure.AddConsole().AddDebug());

        //build IServiceProvider for subsequent use finding/injecting services
        Services = services.BuildServiceProvider();

        //LoggerFactory = Services.GetRequiredService<ILoggerFactory>();
        Logger = Services.GetRequiredService<ILogger<IntegrationTestBase>>();
        Logger.Log(LogLevel.Information, "Test Initialized.");
    }
}
