using Application.Contracts.Interfaces;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Infrastructure.RapidApi.WeatherApi;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Beta;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Cache;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.KeyVault;
using Package.Infrastructure.Test.Integration.AzureOpenAI.Assistant;
using Package.Infrastructure.Test.Integration.AzureOpenAI.Chat;
using Package.Infrastructure.Test.Integration.Blob;
using Package.Infrastructure.Test.Integration.Cosmos;
using Package.Infrastructure.Test.Integration.KeyVault;
using Package.Infrastructure.Test.Integration.Messaging;
using Package.Infrastructure.Test.Integration.MSGraph;
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

    protected IntegrationTestBase()
    {
        // Configuration
        Config = Utility.BuildConfiguration<IntegrationTestBase>();

        // DI
        ServiceCollection services = new();

        // Configure services
        RegisterServices(services);

        // Build IServiceProvider for subsequent use finding/injecting services
        Services = services.BuildServiceProvider();

        Logger = Services.GetRequiredService<ILogger<IntegrationTestBase>>();
        Logger.Log(LogLevel.Information, "Test Initialized.");
    }

    private void RegisterServices(ServiceCollection services)
    {
        AddLogging(services);
        AddBackgroundServices(services);
        AddAzureClients(services, Config);
        AddBlobRepository(services, Config);
        AddTableRepository(services, Config);
        AddEventGridPublisher(services, Config);
        AddCosmosDbRepository(services, Config);
        AddMSGraphService(services, Config);
        AddCaching(services, Config);
        AddRequestContext(services);
        AddExternalServices(services, Config);
        AddApplicationServices(services);
    }

    private static void AddLogging(ServiceCollection services)
    {
        services.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug());
    }

    private static void AddBackgroundServices(ServiceCollection services)
    {
        // Queued background service - fire and forget 
        services.AddHostedService<BackgroundTaskService>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
    }

    private static void AddAzureClients(ServiceCollection services, IConfiguration config)
    {
        services.AddAzureClients(builder =>
        {
            // Set up any default settings
            builder.ConfigureDefaults(config.GetSection("AzureClientDefaults"));

            // Use DefaultAzureCredential by default
            builder.UseCredential(new DefaultAzureCredential());

            AddBlobServiceClient(builder, config);
            AddTableServiceClient(builder, config);
            AddEventGridPublisherClient(builder, config);
            AddKeyVaultClients(builder, config, services);
            AddAzureOpenAIClient(builder, config);
        });
    }

    private static void AddBlobServiceClient(AzureClientFactoryBuilder builder, IConfiguration config)
    {
        var blobConfigSection = config.GetSection("ConnectionStrings:AzureBlobStorageAccount1");
        if (blobConfigSection.GetChildren().Any())
        {
            builder.AddBlobServiceClient(blobConfigSection).WithName("AzureBlobStorageAccount1");
        }
    }

    private static void AddTableServiceClient(AzureClientFactoryBuilder builder, IConfiguration config)
    {
        var tableConfigSection = config.GetSection("ConnectionStrings:AzureTable1");
        if (tableConfigSection.GetChildren().Any())
        {
            builder.AddTableServiceClient(tableConfigSection).WithName("AzureTable1");
        }
    }

    private static void AddEventGridPublisherClient(AzureClientFactoryBuilder builder, IConfiguration config)
    {
        var egpConfigSection = config.GetSection("EventGridPublisherTopic1");
        if (egpConfigSection.GetChildren().Any())
        {
            builder.AddEventGridPublisherClient(
                new Uri(egpConfigSection.GetValue<string>("TopicEndpoint")!),
                new AzureKeyCredential(egpConfigSection.GetValue<string>("Key")!)
            ).WithName("EventGridPublisherTopic1");
        }
    }

    private static void AddKeyVaultClients(AzureClientFactoryBuilder builder, IConfiguration config, ServiceCollection services)
    {
        var kvConfigSection = config.GetSection(KeyVaultManager1Settings.ConfigSectionName);
        if (!kvConfigSection.GetChildren().Any())
        {
            return;
        }

        var akvUrl = kvConfigSection.GetValue<string>("VaultUrl")!;
        var name = kvConfigSection.GetValue<string>("KeyVaultClientName")!;

        builder.AddSecretClient(new Uri(akvUrl)).WithName(name);
        builder.AddKeyClient(new Uri(akvUrl)).WithName(name);
        builder.AddCertificateClient(new Uri(akvUrl)).WithName(name);

        // Crypto Utility; keyed enables registering several if needed
        services.AddKeyedSingleton<IKeyVaultCryptoUtility>("SomeCryptoUtil", (sp, _) =>
        {
            var keyClient = sp.GetRequiredService<IAzureClientFactory<KeyClient>>().CreateClient(name);
            var cryptoClient = keyClient.GetCryptographyClient(kvConfigSection.GetValue<string>("CryptoKey")!);
            return new KeyVaultCryptoUtility(cryptoClient);
        });

        // Wrapper for key vault SDKs
        services.Configure<KeyVaultManager1Settings>(kvConfigSection);
        services.AddSingleton<IKeyVaultManager1, KeyVaultManager1>();
    }

    private static void AddAzureOpenAIClient(AzureClientFactoryBuilder builder, IConfiguration config)
    {
        var azureOpenIAConfigSection = config.GetSection("AzureOpenAI");
        if (azureOpenIAConfigSection.GetChildren().Any())
        {
            // Register a custom client factory since this client does not currently have a service registration method
            builder.AddClient<AzureOpenAIClient, AzureOpenAIClientOptions>((options, _, _) =>
            {
                var key = azureOpenIAConfigSection.GetValue<string?>("Key", null);
                AzureOpenAIClient azureOpenAIClient;

                if (!string.IsNullOrEmpty(key))
                {
                    azureOpenAIClient = new AzureOpenAIClient(
                        new Uri(azureOpenIAConfigSection.GetValue<string>("Url")!),
                        new AzureKeyCredential(key),
                        options
                    );
                }
                else
                {
                    azureOpenAIClient = new AzureOpenAIClient(
                        new Uri(azureOpenIAConfigSection.GetValue<string>("Url")!),
                        new DefaultAzureCredential(),
                        options
                    );
                }

                return azureOpenAIClient;
            });
        }
    }

    private static void AddBlobRepository(ServiceCollection services, IConfiguration config)
    {
        var blobRepoConfigSection = config.GetSection(BlobRepositorySettings1.ConfigSectionName);
        if (blobRepoConfigSection.GetChildren().Any())
        {
            services.AddSingleton<IBlobRepository1, BlobRepository1>();
            services.Configure<BlobRepositorySettings1>(blobRepoConfigSection);
        }
    }

    private static void AddTableRepository(ServiceCollection services, IConfiguration config)
    {
        var tableRepoConfigSection = config.GetSection(TableRepositorySettings1.ConfigSectionName);
        if (tableRepoConfigSection.GetChildren().Any())
        {
            services.AddSingleton<ITableRepository1, TableRepository1>();
            services.Configure<TableRepositorySettings1>(tableRepoConfigSection);
        }
    }

    private static void AddEventGridPublisher(ServiceCollection services, IConfiguration config)
    {
        var egpConfigSection = config.GetSection(EventGridPublisherSettings1.ConfigSectionName);
        if (egpConfigSection.GetChildren().Any())
        {
            services.AddSingleton<IEventGridPublisher1, EventGridPublisher1>();
            services.Configure<EventGridPublisherSettings1>(egpConfigSection);
        }
    }

    private static void AddCosmosDbRepository(ServiceCollection services, IConfiguration config)
    {
        var cosmosConnectionString = config.GetConnectionString("CosmosClient1");
        if (string.IsNullOrEmpty(cosmosConnectionString))
        {
            return;
        }

        var cosmosConfigSection = config.GetSection(CosmosDbRepositorySettings1.ConfigSectionName);
        if (cosmosConfigSection.GetChildren().Any())
        {
            services.AddTransient<ICosmosDbRepository1, CosmosDbRepository1>();
            services.Configure<CosmosDbRepositorySettings1>(s =>
            {
                s.CosmosClient = new CosmosClientBuilder(cosmosConnectionString).Build();
                s.CosmosDbId = cosmosConfigSection.GetValue<string>("CosmosDbId")!;
            });
        }
    }

    private static void AddMSGraphService(ServiceCollection services, IConfiguration config)
    {
        var graphService1ConfigSection = config.GetSection(MSGraphServiceSettings1.ConfigSectionName);
        if (graphService1ConfigSection.GetChildren().Any())
        {
            //register the GraphServiceClient keyed by name
            services.AddKeyedSingleton("MSGraphService1", (_, _) =>
            {
                var tenantId = graphService1ConfigSection["TenantId"];
                var clientId = graphService1ConfigSection["ClientId"];
                var clientSecret = graphService1ConfigSection["ClientSecret"];
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                return new GraphServiceClient(credential, [graphService1ConfigSection["GraphBaseUrl"]]);
            });

            //register the MSGraphService implementation (injected with the keyed GraphServiceClient)
            services.AddSingleton<IMSGraphService1, MSGraphService1>();
            services.Configure<MSGraphServiceSettings1>(graphService1ConfigSection);
        }
    }

    private static void AddCaching(ServiceCollection services, IConfiguration config)
    {
        // FusionCache settings
        List<CacheSettings> cacheSettings = [];
        config.GetSection("CacheSettings").Bind(cacheSettings);

        foreach (var cacheInstance in cacheSettings)
        {
            ConfigureFusionCacheInstance(services, config, cacheInstance);
        }
    }

    private static void ConfigureFusionCacheInstance(ServiceCollection services, IConfiguration config, CacheSettings cacheInstance)
    {
        var fcBuilder = services.AddFusionCache(cacheInstance.Name)
            .WithCysharpMemoryPackSerializer()
            .WithCacheKeyPrefix($"{cacheInstance.Name}:")
            .WithDefaultEntryOptions(new FusionCacheEntryOptions()
            {
                Duration = TimeSpan.FromMinutes(cacheInstance.DurationMinutes),
                DistributedCacheDuration = TimeSpan.FromMinutes(cacheInstance.DistributedCacheDurationMinutes),
                FailSafeMaxDuration = TimeSpan.FromMinutes(cacheInstance.FailSafeMaxDurationMinutes),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(cacheInstance.FailSafeThrottleDurationMinutes),
                JitterMaxDuration = TimeSpan.FromSeconds(10),
                FactorySoftTimeout = TimeSpan.FromSeconds(1),
                FactoryHardTimeout = TimeSpan.FromSeconds(30),
                EagerRefreshThreshold = 0.9f
            });

        ConfigureRedisBackplane(fcBuilder, config, cacheInstance);
    }

    private static void ConfigureRedisBackplane(IFusionCacheBuilder fcBuilder, IConfiguration config, CacheSettings cacheInstance)
    {
        if (string.IsNullOrEmpty(cacheInstance.RedisConfigurationSection))
        {
            return;
        }

        RedisConfiguration redisConfigFusion = new();
        config.GetSection(cacheInstance.RedisConfigurationSection).Bind(redisConfigFusion);

        if (redisConfigFusion.EndpointUrl == null)
        {
            return;
        }

        var redisConfigurationOptions = CreateRedisConfigurationOptions(redisConfigFusion, cacheInstance);

        fcBuilder
            .WithDistributedCache(new RedisCache(new RedisCacheOptions()
            {
                ConfigurationOptions = redisConfigurationOptions
            }))
            .WithBackplane(new RedisBackplane(new RedisBackplaneOptions
            {
                ConfigurationOptions = redisConfigurationOptions
            }));
    }

    private static ConfigurationOptions CreateRedisConfigurationOptions(RedisConfiguration redisConfig, CacheSettings cacheInstance)
    {
        var options = new ConfigurationOptions
        {
            EndPoints = { { redisConfig.EndpointUrl, redisConfig.Port } },
            Ssl = true,
            AbortOnConnectFail = false
        };

        if (cacheInstance.BackplaneChannelName != null)
        {
            options.ChannelPrefix = new RedisChannel(cacheInstance.BackplaneChannelName, RedisChannel.PatternMode.Auto);
        }

        if (!string.IsNullOrEmpty(redisConfig.Password))
        {
            options.Password = redisConfig.Password;
        }
        else
        {
            var azureCredentialOptions = new DefaultAzureCredentialOptions();
            _ = Task.Run(() => options.ConfigureForAzureWithTokenCredentialAsync(
                new DefaultAzureCredential(azureCredentialOptions))).Result;
        }

        return options;
    }

    private static void AddRequestContext(ServiceCollection services)
    {
        // IRequestContext - injected into repositories, cache managers, etc.
        services.AddScoped<IRequestContext<string, Guid?>>(provider =>
        {
            return new RequestContext<string, Guid?>(
                Guid.NewGuid().ToString(),
                "IntegrationTest",
                null,
                []
            );
        });
    }

    private static void AddExternalServices(ServiceCollection services, IConfiguration config)
    {
        AddWeatherService(services, config);
        AddOpenAIServices(services, config);
    }

    private static void AddWeatherService(ServiceCollection services, IConfiguration config)
    {
        var weatherConfigSection = config.GetSection(WeatherServiceSettings.ConfigSectionName);
        if (!weatherConfigSection.GetChildren().Any())
        {
            return;
        }

        services.Configure<WeatherServiceSettings>(weatherConfigSection);
        services.AddScoped<IWeatherService, WeatherService>();

        services.AddHttpClient<IWeatherService, WeatherService>(client =>
        {
            client.BaseAddress = new Uri(config.GetValue<string>("WeatherServiceSettings:BaseUrl")!);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Key", config.GetValue<string>("WeatherServiceSettings:Key")!);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Host", config.GetValue<string>("WeatherServiceSettings:Host")!);
        }).AddStandardResilienceHandler();
    }

    private static void AddOpenAIServices(ServiceCollection services, IConfiguration config)
    {
        AddSomeChatService(services, config);
        AddSomeAssistantService(services, config);
        AddOpenAIChatService(services, config);
    }

    private static void AddSomeChatService(ServiceCollection services, IConfiguration config)
    {
        var someChatConfigSection = config.GetSection(SomeChatSettings.ConfigSectionName);
        if (someChatConfigSection.GetChildren().Any())
        {
            services.AddTransient<ISomeChatService, SomeChatService>();
            services.Configure<SomeChatSettings>(someChatConfigSection);
        }
    }

    private static void AddSomeAssistantService(ServiceCollection services, IConfiguration config)
    {
        var someAssistantConfigSection = config.GetSection(SomeAssistantSettings.ConfigSectionName);
        if (someAssistantConfigSection.GetChildren().Any())
        {
            services.AddTransient<ISomeAssistantService, SomeAssistantService>();
            services.Configure<SomeAssistantSettings>(someAssistantConfigSection);
        }
    }

    private static void AddOpenAIChatService(ServiceCollection services, IConfiguration config)
    {
        var oaiChatConfigSection = config.GetSection(OpenAI.ChatApi.ChatServiceSettings.ConfigSectionName);
        if (oaiChatConfigSection.GetChildren().Any())
        {
            services.AddTransient<OpenAI.ChatApi.IChatService, OpenAI.ChatApi.ChatService>();
            services.Configure<OpenAI.ChatApi.ChatServiceSettings>(oaiChatConfigSection);
        }
    }

    private static void AddApplicationServices(ServiceCollection services)
    {
        // Sample scoped service for testing BackgroundTaskQueue.QueueScopedBackgroundWorkItem
        services.AddScoped<ISomeScopedService, SomeScopedService>();
    }
}