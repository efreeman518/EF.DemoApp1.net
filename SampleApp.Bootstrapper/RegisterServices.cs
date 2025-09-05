using Application.Contracts.Interfaces;
using Application.Contracts.Services;
using Application.MessageHandlers;
using Application.Services;
using Application.Services.JobAssistant;
using Application.Services.JobChat;
using Application.Services.JobSK;
using Application.Services.JobSK.Plugins;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using EntityFramework.Exceptions.SqlServer;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.JobsApi;
using Infrastructure.MSGraphB2C;
using Infrastructure.RapidApi.WeatherApi;
using Infrastructure.Repositories;
using Infrastructure.SampleApi;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Graph;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Package.Infrastructure.AspNetCore.Chaos;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.BackgroundServices.InternalMessageBus;
using Package.Infrastructure.BlandAI;
using Package.Infrastructure.Cache;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Data;
using Package.Infrastructure.Data.Interceptors;
using Polly;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;
using SampleApp.BackgroundServices.Scheduler;
using SampleApp.Bootstrapper.StartupTasks;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text.Json;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using static Microsoft.KernelMemory.AzureOpenAIConfig;

namespace SampleApp.Bootstrapper;

public static class RegisterServices
{
    //can only be registered once; web application factory can determine if it needs to register, otherwise it will throw an exception
    private static bool _keyStoreProviderRegistered = false;

    /// <summary>
    /// Register/configure domain services for DI
    /// </summary>
    public static IServiceCollection RegisterDomainServices(this IServiceCollection services, IConfiguration config)
    {
        _ = services.GetHashCode();
        _ = config.GetHashCode();
        return services;
    }

    /// <summary>
    /// Register/configure app services for DI
    /// </summary>
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        AddApplicationServices(services, config);
        AddMessageHandlers(services);
        AddJobChatServices(services, config);
        AddJobAssistantServices(services, config);
        AddJobSearchServices(services, config);
        return services;
    }

    private static void AddApplicationServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITodoService, TodoService>();
        services.Configure<TodoServiceSettings>(config.GetSection(TodoServiceSettings.ConfigSectionName));

        services.AddScoped<IB2CManagement, B2CManagement>();
    }

    private static void AddMessageHandlers(IServiceCollection services)
    {
        services.AddSingleton<IMessageHandler<AuditEntry<string>>, AuditHandler>();
        services.AddScoped<IMessageHandler<AuditEntry<string>>, SomeScopedHandler>();
    }

    private static void AddJobChatServices(IServiceCollection services, IConfiguration config)
    {
        var jobChatOrchestratorConfigSection = config.GetSection(JobChatOrchestratorSettings.ConfigSectionName);
        if (jobChatOrchestratorConfigSection.GetChildren().Any())
        {
            services.AddTransient<IJobChatOrchestrator, JobChatOrchestrator>();
            services.Configure<JobChatOrchestratorSettings>(jobChatOrchestratorConfigSection);
        }

        var jobChatConfigSection = config.GetSection(JobChatServiceSettings.ConfigSectionName);
        if (jobChatConfigSection.GetChildren().Any())
        {
            //AzureOpenAI chat service wrapper (not an Azure Client but a wrapper that uses it)
            services.AddTransient<IJobChatService, JobChatService>();
            services.Configure<JobChatServiceSettings>(jobChatConfigSection);
        }
    }

    private static void AddJobAssistantServices(IServiceCollection services, IConfiguration config)
    {
        var jobAssistantOrchestratorConfigSection = config.GetSection(JobAssistantOrchestratorSettings.ConfigSectionName);
        if (jobAssistantOrchestratorConfigSection.GetChildren().Any())
        {
            services.AddTransient<IJobAssistantOrchestrator, JobAssistantOrchestrator>();
            services.Configure<JobAssistantOrchestratorSettings>(jobAssistantOrchestratorConfigSection);
        }

        var jobAssistantConfigSection = config.GetSection(JobAssistantServiceSettings.ConfigSectionName);
        if (jobAssistantConfigSection.GetChildren().Any())
        {
            //AzureOpenAI assistant service wrapper (not an Azure Client but a wrapper that uses it)
            services.AddTransient<IJobAssistantService, JobAssistantService>();
            services.Configure<JobAssistantServiceSettings>(jobAssistantConfigSection);
        }
    }

    private static void AddJobSearchServices(IServiceCollection services, IConfiguration config)
    {
        var jobSearchOrchestratorConfigSection = config.GetSection(JobSearchOrchestratorSettings.ConfigSectionName);
        if (jobSearchOrchestratorConfigSection.GetChildren().Any())
        {
            //Semantic Kernel service wrapper (not an Azure Client but a wrapper that uses it)
            services.AddTransient<IJobSearchOrchestrator, JobSearchOrchestrator>();
            services.Configure<JobSearchOrchestratorSettings>(jobSearchOrchestratorConfigSection);
        }
    }

    /// <summary>
    /// Register/configure infrastructure services for DI
    /// </summary>
    public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        AddConfigurationServices(services, config);
        AddInternalServices(services);
        AddCachingServices(services, config);
        AddB2CManagementService(services, config);
        AddRequestContextServices(services);
        AddDatabaseServices(services, config);
        AddJobsApiServices(services, config);
        AddAzureClientServices(services, config);
        AddChaosServices(services, config);
        AddBlandAIServices(services, config);
        AddSampleApiServices(services, config);
        AddWeatherServices(services, config);
        AddStartupTasks(services);

        return services;
    }

    private static void AddB2CManagementService(IServiceCollection services, IConfiguration config)
    {
        var msgraphServiceConfigSection = config.GetSection(MSGraphServiceB2CSettings.ConfigSectionName);
        if (msgraphServiceConfigSection.GetChildren().Any())
        {
            //register the GraphServiceClient keyed by name, does not currently support managed identity, so using ClientSecretCredential
            services.AddKeyedSingleton("MSGraphServiceB2C", (_, _) =>
            {
                var tenantId = msgraphServiceConfigSection["TenantId"];
                var clientId = msgraphServiceConfigSection["ClientId"];
                var clientSecret = msgraphServiceConfigSection["ClientSecret"];
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                return new GraphServiceClient(credential, [msgraphServiceConfigSection["GraphBaseUrl"]]);
            });

            //register the MSGraphService implementation (injected with the keyed GraphServiceClient)
            services.AddSingleton<IMSGraphServiceB2C, MSGraphServiceB2C>();
            services.Configure<MSGraphServiceB2CSettings>(msgraphServiceConfigSection);
        }
    }

    private static void AddInternalServices(IServiceCollection services)
    {
        services.AddSingleton<IInternalMessageBus, InternalMessageBus>();
    }

    private static void AddConfigurationServices(IServiceCollection services, IConfiguration config)
    {
        //enable config reloading at runtime using Sentinel along with app.UseAzureAppConfiguration();
        if (config.GetValue<string>("AzureAppConfig:Endpoint") != null)
        {
            services.AddAzureAppConfiguration();
        }
    }

    private static void AddCachingServices(IServiceCollection services, IConfiguration config)
    {
        // Configure FusionCache
        List<CacheSettings> cacheSettings = [];
        config.GetSection("CacheSettings").Bind(cacheSettings);

        foreach (var cacheSettingsInstance in cacheSettings)
        {
            ConfigureFusionCacheInstance(services, config, cacheSettingsInstance);
        }

        // Configure Redis (backfill with local only Memory Cache) for direct use if needed
        ConfigureDistributedCache(services, config);
    }

    private static void ConfigureFusionCacheInstance(IServiceCollection services, IConfiguration config, CacheSettings cacheSettingsInstance)
    {
        var fcBuilder = services.AddFusionCache(cacheSettingsInstance.Name)
            .WithCysharpMemoryPackSerializer()
            .WithCacheKeyPrefix($"{cacheSettingsInstance.Name}:")
            .WithDefaultEntryOptions(new FusionCacheEntryOptions()
            {
                //memory cache duration
                Duration = TimeSpan.FromMinutes(cacheSettingsInstance.DurationMinutes),
                //distributed cache duration
                DistributedCacheDuration = TimeSpan.FromMinutes(cacheSettingsInstance.DistributedCacheDurationMinutes),
                //how long to use expired cache value if the factory is unable to provide an updated value
                FailSafeMaxDuration = TimeSpan.FromMinutes(cacheSettingsInstance.FailSafeMaxDurationMinutes),
                //how long to wait before trying to get a new value from the factory after a fail-safe expiration
                FailSafeThrottleDuration = TimeSpan.FromSeconds(cacheSettingsInstance.FailSafeThrottleDurationMinutes),
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
        ConfigureFusionCacheRedis(fcBuilder, config, cacheSettingsInstance);
    }

    private static void ConfigureFusionCacheRedis(IFusionCacheBuilder fcBuilder, IConfiguration config, CacheSettings cacheSettingsInstance)
    {
        // possible use the redis connection string
        if (!string.IsNullOrEmpty(cacheSettingsInstance.RedisConnectionStringName))
        {
            var redisConnectionString = config.GetConnectionString(cacheSettingsInstance.RedisConnectionStringName);
            fcBuilder
                .WithDistributedCache(new RedisCache(new RedisCacheOptions()
                {
                    Configuration = redisConnectionString
                }))
                .WithBackplane(new RedisBackplane(new RedisBackplaneOptions
                {
                    Configuration = redisConnectionString
                }));
        }
        //Azure seems to have a backplane issue when using redis connection string, so optionally parse into explicit config options
        else
        {
            RedisConfiguration redisConfigFusion = new();
            config.GetSection(cacheSettingsInstance.RedisConfigurationSection!).Bind(redisConfigFusion);

            if (redisConfigFusion.EndpointUrl != null)
            {
                var redisConfigurationOptions = CreateRedisConfigurationOptions(redisConfigFusion, cacheSettingsInstance.BackplaneChannelName);

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
            else
                throw new InvalidOperationException("Redis not configured.");
        }
    }

    /// <summary>
    /// Not needed for FusionCache, but can be used for direct Redis access if needed
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    private static void ConfigureDistributedCache(IServiceCollection services, IConfiguration config)
    {
        var redisConfigSection = "Redis1Configuration";
        var configSectionRedis = config.GetSection(redisConfigSection);

        if (configSectionRedis.GetChildren().Any())
        {
            RedisConfiguration redisConfig = new();
            configSectionRedis.Bind(redisConfig);
            var redisConfigurationOptions = CreateRedisConfigurationOptions(redisConfig, null);

            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = redisConfigurationOptions;
            });
        }
        else
        {
            services.AddDistributedMemoryCache(); // Local server only, not distributed. Helps with tests
        }
    }

    private static ConfigurationOptions CreateRedisConfigurationOptions(RedisConfiguration redisConfig, string? channelName)
    {
        var redisConfigurationOptions = new ConfigurationOptions
        {
            EndPoints =
            {
                {
                    redisConfig.EndpointUrl,
                    redisConfig.Port
                }
            },
            Ssl = true,
            AbortOnConnectFail = false
        };

        if (channelName != null)
        {
            redisConfigurationOptions.ChannelPrefix = new RedisChannel(channelName, RedisChannel.PatternMode.Auto);
        }

        if (!string.IsNullOrEmpty(redisConfig.Password))
        {
            redisConfigurationOptions.Password = redisConfig.Password;
        }
        else
        {
            var options = new DefaultAzureCredentialOptions();
            _ = Task.Run(() => redisConfigurationOptions.ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential(options))).Result;
        }

        return redisConfigurationOptions;
    }

    private static void AddRequestContextServices(IServiceCollection services)
    {
        services.AddScoped<IRequestContext<string, Guid?>>(provider =>
        {
            var httpContext = provider.GetService<IHttpContextAccessor>()?.HttpContext;
            var correlationId = httpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();

            // Background services will not have an http context
            if (httpContext == null)
            {
                return new RequestContext<string, Guid?>(correlationId, $"BackgroundService-{correlationId}", null, []);
            }

            // Get auditId from token claim or header - tries multiple claim types in order of preference:
            // 1. Email claim (typically user email)
            // 2. Object ID claim (Azure AD user or service principal object ID)
            // 3. App ID claim (client application ID)
            // 4. Falls back to "NoAuthImplemented" if no claims found
            //var user = httpContext.User;
            //string? auditId =
            //    user?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
            //    ?? user?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
            //    ?? user?.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            //    ?? user?.Claims.FirstOrDefault(c => c.Type == "appid")?.Value
            //    ?? "NoAuthImplemented";

            // Get original claims from header; this is typically set by the reverse proxy or API gateway to pass through user claims
            // OBO (on behalf of) tokens are not an option when the ui/gateway authority (AzureB2C) is different than this API authority (EntraID)
            var origClaimsHeader = httpContext.Request.Headers["X-Orig-Request"].FirstOrDefault();
            var claims = JsonSerializer.Deserialize<Dictionary<string, string>>(origClaimsHeader!);
            string auditId = "";
            Guid? tenantId = null;
            if (claims != null)
            {
                // Audit ID is typically the user's subject, email or object ID; if not available, use "NoAuditClaim"
                auditId = claims.GetFirstKeyValue("sub", "oid") ?? "NoAuditClaim";
                // Determine tenantId from token claim; we want the client's associated tenant, NOT the standard EntraID tenant ID claim (http://schemas.microsoft.com/identity/claims/tenantid)
                tenantId = Guid.TryParse(claims.GetFirstKeyValue("userTenantId"), out Guid clientTenantId) ? clientTenantId : null;
            }
            //roles previously extracted from header and applied in middleware CustomHeaderAuthMiddleware
            List<string> rolesList = [.. httpContext.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)];

            return new RequestContext<string, Guid?>(correlationId, auditId, tenantId, rolesList);
        });
    }

    private static void AddDatabaseServices(IServiceCollection services, IConfiguration config)
    {
        // Register repositories
        services.AddScoped<ITodoRepositoryTrxn, TodoRepositoryTrxn>();
        services.AddScoped<ITodoRepositoryQuery, TodoRepositoryQuery>();

        // Register EF interceptors; used in PooledDbContextFactory singleton so cannot be scoped
        services.AddTransient<AuditInterceptor<string, Guid?>>();
        services.AddTransient<ConnectionNoLockInterceptor>();

        // Configure database contexts
        ConfigureDatabaseContexts(services, config);
        RegisterSqlAlwaysEncrypted();
    }

    private static void ConfigureDatabaseContexts(IServiceCollection services, IConfiguration config)
    {
        var trxnDBConnectionString = config.GetConnectionString("TodoDbContextTrxn");
        var queryDBConnectionString = config.GetConnectionString("TodoDbContextQuery");

        if (string.IsNullOrEmpty(trxnDBConnectionString) || trxnDBConnectionString == "UseInMemoryDatabase")
        {
            ConfigureInMemoryDatabase(services);
        }
        else
        {
            ConfigureSqlDatabase(services, trxnDBConnectionString, queryDBConnectionString);
        }
    }

    private static void ConfigureInMemoryDatabase(IServiceCollection services)
    {
        var inMemoryDatabaseRoot = new InMemoryDatabaseRoot();

        services.AddDbContext<TodoDbContextTrxn>((sp, opt) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditInterceptor<string, Guid>>();
            opt.UseInMemoryDatabase("TodoDbContext", inMemoryDatabaseRoot).AddInterceptors(auditInterceptor);
        });

        services.AddDbContext<TodoDbContextQuery>((sp, opt) =>
        {
            opt.UseInMemoryDatabase("TodoDbContext", inMemoryDatabaseRoot);
        });
    }

    private static void ConfigureSqlDatabase(IServiceCollection services, string dbConnectionStringTrxn, string? dbConnectionStringQuery)
    {
        //AzureSQL - https://learn.microsoft.com/en-us/ef/core/providers/sql-server/?tabs=dotnet-core-cli
        //consider a pooled factory - https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#dbcontext-pooling
        //sql compatibility level - https://learn.microsoft.com/en-us/sql/t-sql/statements/alter-database-transact-sql-compatibility-level?view=azuresqldb-current

        services.AddPooledDbContextFactory<TodoDbContextTrxn>((sp, options) =>
        {
            ConfigureTrxnDbContext(options, dbConnectionStringTrxn);
            var auditInterceptor = sp.GetRequiredService<AuditInterceptor<string, Guid?>>();
            options
                .UseExceptionProcessor()
                .AddInterceptors(auditInterceptor);
        });
        services.AddScoped<DbContextScopedFactory<TodoDbContextTrxn, string, Guid?>>();
        services.AddScoped(sp => sp.GetRequiredService<DbContextScopedFactory<TodoDbContextTrxn, string, Guid?>>().CreateDbContext());

        if (dbConnectionStringQuery != null)
        {
            services.AddPooledDbContextFactory<TodoDbContextQuery>((sp, options) =>
            {
                ConfigureQueryDbContext(options, dbConnectionStringQuery);
                var noLockInterceptor = sp.GetRequiredService<ConnectionNoLockInterceptor>();
                options
                    .UseExceptionProcessor()
                    .AddInterceptors(noLockInterceptor);
            });
            services.AddScoped<DbContextScopedFactory<TodoDbContextQuery, string, Guid?>>();
            services.AddScoped(sp => sp.GetRequiredService<DbContextScopedFactory<TodoDbContextQuery, string, Guid?>>().CreateDbContext());
        }
    }

    private static void ConfigureTrxnDbContext(DbContextOptionsBuilder options, string connectionString)
    {
        if (connectionString.Contains("database.windows.net"))
        {
            options.UseAzureSql(connectionString, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(170);
                //retry strategy does not support user initiated transactions 
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                //use relational null semantics 3-valued logic (true, false, null) instead of c# which may generate less efficient sql, but LINQ queries will have a different meaning
                //https://learn.microsoft.com/en-us/ef/core/querying/null-comparisons
                //sqlOptions.UseRelationalNulls(true);
            });
        }
        else
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(160);
                //retry strategy does not support user initiated transactions 
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                //use relational null semantics 3-valued logic (true, false, null) instead of c# which may generate less efficient sql, but LINQ queries will have a different meaning
                //https://learn.microsoft.com/en-us/ef/core/querying/null-comparisons
                //sqlOptions.UseRelationalNulls(true);
            });
        }
    }

    private static void ConfigureQueryDbContext(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        if (connectionString.Contains("database.windows.net"))
        {
            options.UseAzureSql(connectionString, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(170);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                //use relational null semantics 3-valued logic (true, false, null) instead of c# which may generate less efficient sql, but LINQ queries will have a different meaning
                //https://learn.microsoft.com/en-us/ef/core/querying/null-comparisons
                //sqlOptions.UseRelationalNulls(true);
                //default to split queries to avoid cartesian explosion when joining (multiple includes at the same level)
                //https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
                //sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        }
        else
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(160);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                //use relational null semantics 3-valued logic (true, false, null) instead of c# which may generate less efficient sql, but LINQ queries will have a different meaning
                //https://learn.microsoft.com/en-us/ef/core/querying/null-comparisons
                //sqlOptions.UseRelationalNulls(true);
                //default to split queries to avoid cartesian explosion when joining (multiple includes at the same level)
                //https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
                //sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        }
    }

    //SQL ALWAYS ENCRYPTED, the connection string must include "Column Encryption Setting=Enabled"
    private static void RegisterSqlAlwaysEncrypted()
    {
        if (!_keyStoreProviderRegistered)
        {
            var credential = new DefaultAzureCredential();
            SqlColumnEncryptionAzureKeyVaultProvider sqlColumnEncryptionAzureKeyVaultProvider = new(credential);

            try
            {
                SqlConnection.RegisterColumnEncryptionKeyStoreProviders(
                    customProviders: new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(capacity: 1, comparer: StringComparer.OrdinalIgnoreCase)
                    {
                        {
                            SqlColumnEncryptionAzureKeyVaultProvider.ProviderName,
                            sqlColumnEncryptionAzureKeyVaultProvider
                        }
                    });
            }
            catch
            {
                //ignore; already registered.  T
                //this is a workaround for the fact that the SqlColumnEncryptionAzureKeyVaultProvider is already registered in the web application factory registrations
                //SqlConnection does not currently have a Try register method or any way to check if a provider is already registered

            }
            _keyStoreProviderRegistered = true;
        }
    }

    private static void AddJobsApiServices(IServiceCollection services, IConfiguration config)
    {
        var jobsApiConfigSection = config.GetSection(JobsApiServiceSettings.ConfigSectionName);
        if (jobsApiConfigSection.GetChildren().Any())
        {
            services.Configure<JobsApiServiceSettings>(jobsApiConfigSection);
            services.AddScoped<IJobsApiService, JobsApiService>();
            services.AddHttpClient<IJobsApiService, JobsApiService>(client =>
            {
                client.BaseAddress = new Uri(jobsApiConfigSection.GetValue<string>("BaseUrl")!);
            })
            .AddStandardResilienceHandler();
        }
    }

    private static void AddAzureClientServices(IServiceCollection services, IConfiguration config)
    {
        //Azure Service Clients - Blob, EventGridPublisher, KeyVault, etc; enables injecting IAzureClientFactory<>
        //https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection
        //https://devblogs.microsoft.com/azure-sdk/lifetime-management-and-thread-safety-guarantees-of-azure-sdk-net-clients/
        //https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.azure.azureclientfactorybuilder?view=azure-dotnet
        //https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Microsoft.Extensions.Azure/1.0.0/index.html

        services.AddAzureClients(builder =>
        {
            // Set up any default settings
            builder.ConfigureDefaults(config.GetSection("AzureClientDefaults"));
            builder.UseCredential(new DefaultAzureCredential());

            RegisterEventGridPublisherClients(builder, config);
            RegisterAzureOpenAIClients(services, builder, config);
        });
    }

    private static void RegisterEventGridPublisherClients(AzureClientFactoryBuilder builder, IConfiguration config)
    {
        var egpConfigSection = config.GetSection("EventGridPublisher1");
        if (egpConfigSection.GetChildren().Any())
        {
            builder.AddEventGridPublisherClient(new Uri(egpConfigSection.GetValue<string>("TopicEndpoint")!),
                new AzureKeyCredential(egpConfigSection.GetValue<string>("Key")!))
            .WithName("EventGridPublisher1");
        }
    }

    private static void RegisterAzureOpenAIClients(IServiceCollection services, AzureClientFactoryBuilder builder, IConfiguration config)
    {
        var aoaiConfig = config.GetSection("AzureOpenAI");
        if (!aoaiConfig.GetChildren().Any())
        {
            return;
        }

        var endpoint = aoaiConfig.GetValue<string>("Endpoint")!;

        // Register custom client factory
        builder.AddClient<AzureOpenAIClient, AzureOpenAIClientOptions>((options) =>
        {
            AzureOpenAIClient aoaiClient;
            var key = aoaiConfig.GetValue<string?>("Key", null);
            if (!string.IsNullOrEmpty(key))
            {
                aoaiClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key), options);
            }
            else
            {
                aoaiClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential(), options);
            }
            return aoaiClient;
        }).WithName("AzureOpenAI");

        // Configure OpenAI services
        ConfigureOpenAIServices(services, aoaiConfig, endpoint);
    }

    private static void ConfigureOpenAIServices(IServiceCollection services, IConfigurationSection aoaiConfig, string endpoint)
    {
        var clientFactory = services.BuildServiceProvider().GetRequiredService<IAzureClientFactory<AzureOpenAIClient>>();
        AzureOpenAIClient aoaiClient = clientFactory.CreateClient("AzureOpenAI");

        // Default chat completion service
        services.AddAzureOpenAIChatCompletion(aoaiConfig.GetValue<string>("DefaultChatDeployment")!, aoaiClient);

        // Default text embedding service
        var textEmbeddingDeployment = aoaiConfig.GetValue<string>("DefaultTextEmbeddingDeployment")!;

        var aoiaConfig = new AzureOpenAIConfig
        {
            Auth = AuthTypes.AzureIdentity,
            APIType = APITypes.EmbeddingGeneration,
            Deployment = textEmbeddingDeployment,
            Endpoint = endpoint
        };
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only
        // Embedding data
        services.AddAzureOpenAIEmbeddingGeneration(aoiaConfig);
        // Search data
        services.AddAzureOpenAITextGeneration(new AzureOpenAIConfig
        {
            Auth = AuthTypes.AzureIdentity,
            APIType = APITypes.EmbeddingGeneration,
            Deployment = textEmbeddingDeployment,
            Endpoint = endpoint
        });
#pragma warning restore SKEXP0010

        // Kernel memory
        ConfigureKernelMemory(services, endpoint, textEmbeddingDeployment);
    }

    private static void ConfigureKernelMemory(IServiceCollection services, string endpoint, string textEmbeddingDeployment)
    {
        var memory = new KernelMemoryBuilder()
            .WithAzureOpenAITextGeneration(new AzureOpenAIConfig
            {
                Auth = AuthTypes.AzureIdentity,
                Endpoint = endpoint,
                Deployment = textEmbeddingDeployment
            })
            .WithAzureOpenAITextEmbeddingGeneration(new AzureOpenAIConfig
            {
                Auth = AuthTypes.AzureIdentity,
                Endpoint = endpoint,
                Deployment = textEmbeddingDeployment
            })
            .Build<MemoryServerless>();
        services.AddSingleton<IKernelMemory>(memory);

        // Plugins
        services.AddSingleton<JobSearchPlugin>();

        // Plugin collection
        services.AddSingleton<KernelPluginCollection>((serviceProvider) =>
            [
                KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<JobSearchPlugin>()),
            ]
        );

        // Kernels
        services.AddKeyedTransient<Kernel>("JobSearchKernel", (sp, key) =>
        {
            var kernel = new Kernel(sp, sp.GetRequiredService<KernelPluginCollection>());
            return kernel;
        });
    }

    private static void AddChaosServices(IServiceCollection services, IConfiguration config)
    {
        var configSectionChaos = config.GetSection(ChaosManagerSettings.ConfigSectionName);
        if (configSectionChaos.GetChildren().Any() && configSectionChaos.GetValue<bool>("Enabled"))
        {
            services.AddHttpContextAccessor();
            services.TryAddSingleton<IChaosManager, ChaosManager>();
            services.Configure<ChaosManagerSettings>(configSectionChaos);
        }
    }

    private static void AddBlandAIServices(IServiceCollection services, IConfiguration config)
    {
        var blandAIConfigSection = config.GetSection(BlandAISettings.ConfigSectionName);
        if (!blandAIConfigSection.GetChildren().Any())
        {
            return;
        }

        services.Configure<BlandAISettings>(blandAIConfigSection);
        services.AddScoped<IBlandAIRestClient, BlandAIRestClient>();

        services.AddHttpClient<IBlandAIRestClient, BlandAIRestClient>(client =>
        {
            client.BaseAddress = new Uri(blandAIConfigSection.GetValue<string>("BaseUrl")!);
            client.DefaultRequestHeaders.Add("Authorization", blandAIConfigSection.GetValue<string>("Key")!);
        })
        .AddStandardResilienceHandler();
    }

    private static void AddSampleApiServices(IServiceCollection services, IConfiguration config)
    {
        var sampleApiConfigSection = config.GetSection(SampleApiRestClientSettings.ConfigSectionName);
        if (!sampleApiConfigSection.GetChildren().Any())
        {
            return;
        }

        services.Configure<SampleApiRestClientSettings>(sampleApiConfigSection);


        // determines the lifetime of the handler—and thus the DefaultAzureCredential instance inside it (token caching)
        services.AddSingleton(provider =>
        {
            // Set env vars if client credentials are provided
            if (config.GetValue<string>("SampleApiRestClientSettings:ClientId") != null)
            {
                Environment.SetEnvironmentVariable("AZURE_TENANT_ID", config.GetValue<string>("SampleApiRestClientSettings:TenantId"));
                Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", config.GetValue<string>("SampleApiRestClientSettings:ClientId"));
                Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", config.GetValue<string>("SampleApiRestClientSettings:ClientSecret"));
            }
            var scopes = config.GetSection("SampleApiRestClientSettings:Scopes").Get<string[]>();
            return new SampleRestApiAuthMessageHandler(scopes!);
        });

        var httpClientBuilder = ConfigureSampleApiClient(services, config);
        ConfigureSampleApiResiliency(httpClientBuilder);
        ConfigureSampleApiChaos(httpClientBuilder, config);
    }

    private static IHttpClientBuilder ConfigureSampleApiClient(IServiceCollection services, IConfiguration config)
    {
        return services.AddHttpClient<ISampleApiRestClient, SampleApiRestClient>(provider =>
        {
            provider.BaseAddress = new Uri(config.GetValue<string>("SampleApiRestClientSettings:BaseUrl")!);
        })
        .AddHttpMessageHandler<SampleRestApiAuthMessageHandler>();
    }

    private static void ConfigureSampleApiResiliency(IHttpClientBuilder httpClientBuilder)
    {
        //https://devblogs.microsoft.com/dotnet/resilience-and-chaos-engineering/
        //Polly.Core referenced alongside Microsoft.Extensions.Http.Resilience to ensure access to the latest Polly version featuring chaos strategies.
        //Once Microsoft.Extensions.Http.Resilience incorporates the latest Polly.Core, remove Polly.Core
        //Adding standard resilience to handle the chaos, optionally configure details
        httpClientBuilder.AddStandardResilienceHandler().Configure(options =>
        {
            // Update attempt timeout to 1 second
            //options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);

            // Update circuit breaker to handle transient errors and InvalidOperationException
            options.CircuitBreaker.ShouldHandle = args => args.Outcome switch
            {
                { } outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
                { Exception: InvalidOperationException } => PredicateResult.True(),
                _ => PredicateResult.False()
            };

            // Update retry strategy to handle transient errors and InvalidOperationException
            options.Retry.ShouldHandle = args => args.Outcome switch
            {
                { } outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
                { Exception: InvalidOperationException } => PredicateResult.True(),
                _ => PredicateResult.False()
            };
        });
    }

    private static void ConfigureSampleApiChaos(IHttpClientBuilder httpClientBuilder, IConfiguration config)
    {
        //chaos - always after standard resilience handler

        var configSectionChaos = config.GetSection(ChaosManagerSettings.ConfigSectionName);
        if (configSectionChaos.GetChildren().Any() && configSectionChaos.GetValue<bool>("Enabled"))
        {
            httpClientBuilder.AddResilienceHandler("chaos", (builder, context) =>
            {
                var chaosManager = context.ServiceProvider.GetRequiredService<IChaosManager>();

                builder
                   .AddChaosLatency(new ChaosLatencyStrategyOptions
                   {
                       EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
                       InjectionRateGenerator = args => chaosManager.GetInjectionRateAsync(args.Context),
                       Latency = TimeSpan.FromSeconds(chaosManager.LatencySeconds())
                   })
                   .AddChaosFault(new ChaosFaultStrategyOptions
                   {
                       EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
                       InjectionRateGenerator = args => chaosManager.GetInjectionRateAsync(args.Context),
                       FaultGenerator = new FaultGenerator().AddException(() => chaosManager.FaultException())
                   })
                   .AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
                   {
                       EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
                       InjectionRateGenerator = args => chaosManager.GetInjectionRateAsync(args.Context),
                       OutcomeGenerator = new OutcomeGenerator<HttpResponseMessage>()
                           .AddResult(() => new HttpResponseMessage(chaosManager.OutcomHttpStatusCode()))
                   })
                   //introduce a specific behavior as chaos
                   //.AddChaosBehavior(0.001, cancellationToken => RestartRedisAsync(cancellationToken)) // Introduce a specific behavior as chaos
                   ;
            });
        }
    }

    private static void AddWeatherServices(IServiceCollection services, IConfiguration config)
    {
        var weatherServiceConfigSection = config.GetSection(WeatherServiceSettings.ConfigSectionName);
        if (weatherServiceConfigSection.GetChildren().Any())
        {
            services.Configure<WeatherServiceSettings>(weatherServiceConfigSection);
            services.AddSingleton<IWeatherService, WeatherService>();

            services.AddHttpClient<IWeatherService, WeatherService>(client =>
            {
                client.BaseAddress = new Uri(config.GetValue<string>("WeatherServiceSettings:BaseUrl")!);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Key", config.GetValue<string>("WeatherServiceSettings:Key")!);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Host", config.GetValue<string>("WeatherServiceSettings:Host")!);
            })
            .AddStandardResilienceHandler();
        }
    }

    private static void AddStartupTasks(IServiceCollection services)
    {
        services.AddTransient<IStartupTask, LoadCache>();
    }

    public static IServiceCollection RegisterBackgroundServices(this IServiceCollection services, IConfiguration config)
    {
        //background task
        services.AddChannelBackgroundTaskQueue();

        //scheduler
        var configSection = config.GetSection(CronServiceSettings.ConfigSectionName);
        if (configSection.GetChildren().Any())
        {
            services.Configure<CronJobBackgroundServiceSettings<CustomCronJob>>(configSection);
            services.AddHostedService<CronService>();
        }

        return services;
    }
}
