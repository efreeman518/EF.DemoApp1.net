using Application.Contracts.Interfaces;
using Application.Contracts.Services;
using Application.MessageHandlers;
using Application.Services;
using Application.Services.JobAssistant;
using Application.Services.JobChat;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Assistants;
using Azure.Identity;
using CorrelationId.Abstractions;
using CorrelationId.HttpClient;
using EntityFramework.Exceptions.SqlServer;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Data.Interceptors;
using Infrastructure.JobsApi;
using Infrastructure.RapidApi.WeatherApi;
using Infrastructure.Repositories;
using Infrastructure.SampleApi;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Package.Infrastructure.AspNetCore.Chaos;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.BackgroundServices.InternalMessageBroker;
using Package.Infrastructure.Cache;
using Package.Infrastructure.Common.Contracts;
using Polly;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;
using SampleApp.BackgroundServices.Scheduler;
using SampleApp.Bootstrapper.StartupTasks;
using StackExchange.Redis;
using System.Security.Claims;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace SampleApp.Bootstrapper;

public static class IServiceCollectionExtensions
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
        services.AddScoped<ITodoService, TodoService>();
        services.Configure<TodoServiceSettings>(config.GetSection(TodoServiceSettings.ConfigSectionName));

        services.AddScoped<IJobChatOrchestrator, JobChatOrchestrator>();
        services.Configure<JobChatOrchestratorSettings>(config.GetSection(JobChatOrchestratorSettings.ConfigSectionName));

        services.AddScoped<IJobAssistantOrchestrator, JobAssistantOrchestrator>();
        services.Configure<JobAssistantOrchestratorSettings>(config.GetSection(JobAssistantOrchestratorSettings.ConfigSectionName));

        return services;
    }

    /// <summary>
    /// Register/configure infrastructure services for DI
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="hasHttpContext">Prevent HttpContext dependent registrations if there is none (Integration Tests)</param>
    /// <param name="localEnviroment">Some Azure SDKs will throw when using DefaultAzureCredential() when there is no managed identity</param>
    /// <returns></returns>
    public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services, IConfiguration config, bool hasHttpContext = false)
    {
        //this middleware will check the Azure App Config Sentinel for a change which triggers reloading the configuration
        //middleware triggers on http request, not background service scope
        if (config.GetValue<string>("AzureAppConfig:Endpoint") != null)
        {
            services.AddAzureAppConfiguration();
        }

        //not needed when using FusionCache or .net HybridCache
        //LazyCache.AspNetCore, lightweight wrapper around memorycache; prevent race conditions when multiple threads attempt to refresh empty cache item
        //https://github.com/alastairtree/LazyCache
        //services.AddLazyCache();

        //FusionCache - https://www.nuget.org/packages/ZiggyCreatures.FusionCache
        List<CacheSettings> cacheSettings = [];
        config.GetSection("CacheSettings").Bind(cacheSettings);

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
                config.GetSection(cacheInstance.RedisConfigurationSection).Bind(redisConfigFusion);
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
                    var options = new DefaultAzureCredentialOptions();
                    //if (localEnviroment)
                    //{
                    //    //running local causes errors ManagedIdentityCredential.GetToken was unable to retrieve an access token. Scopes: [ https://cognitiveservices.azure.com/.default ]
                    //    //Azure.RequestFailedException: A socket operation was attempted to an unreachable network. (169.254.169.254:80)\r\n ---> System.Net.Http.HttpRequestException: A socket operation was attempted to an unreachable network. (169.254.169.254:80)
                    //    options.ExcludeManagedIdentityCredential = true;
                    //}
                    //configure redis with managed identity
                    _ = Task.Run(() => redisConfigurationOptions.ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential(options))).Result;
                }

                //var redisFCConnectionString = config.GetConnectionString(cacheInstance.RedisConfigurationSection);
                //var redisConfigurationOptions = ConfigurationOptions.Parse(redisFCConnectionString!);
                //    .ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential()); //configure redis with managed identity


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

        //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed
        //var redisConnectionString = config.GetConnectionString("Redis1");

        var redisConfigSection = "Redis1Configuration";
        var configSectionRedis = config.GetSection(redisConfigSection);
        if (configSectionRedis != null)
        {
            RedisConfiguration redisConfig = new();
            configSectionRedis.Bind(redisConfig);
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

            if (!string.IsNullOrEmpty(redisConfig.Password))
            {
                redisConfigurationOptions.Password = redisConfig.Password;
            }
            else
            {
                var options = new DefaultAzureCredentialOptions();
                //if (localEnviroment)
                //{
                //    //running local causes errors ManagedIdentityCredential.GetToken was unable to retrieve an access token. Scopes: [ https://cognitiveservices.azure.com/.default ]
                //    //Azure.RequestFailedException: A socket operation was attempted to an unreachable network. (169.254.169.254:80)\r\n ---> System.Net.Http.HttpRequestException: A socket operation was attempted to an unreachable network. (169.254.169.254:80)
                //    options.ExcludeManagedIdentityCredential = true;
                //}
                //configure redis with managed identity
                _ = Task.Run(() => redisConfigurationOptions.ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential(options))).Result;
            }

            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = redisConfigurationOptions;
                //options.Configuration = redisConnectionString;
                //options.InstanceName = "redis1";
            });
        }
        else
        {
            services.AddDistributedMemoryCache(); //local server only, not distributed. Helps with tests
        }

        //IRequestContext - injected into repositories, cache managers, etc
        services.AddScoped<IRequestContext<string>>(provider =>
        {
            var httpContext = provider.GetService<IHttpContextAccessor>()?.HttpContext;

            //https://github.com/stevejgordon/CorrelationId/wiki
            var correlationId = provider.GetService<ICorrelationContextAccessor>()?.CorrelationContext?.CorrelationId
                ?? Guid.NewGuid().ToString();

            //Background services will not have an http context
            if (httpContext == null)
            {
                return new RequestContext<string>(correlationId, $"BackgroundService-{correlationId}");
            }

            var user = httpContext.User;

            //Get auditId from token claim or header
            string? auditId =
                //AAD from user
                user?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                //AAD ObjectId from user or client AAD enterprise app [ServicePrincipal Id / Object Id]:
                ?? user?.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                //AppId for the AAD Ent App/App Reg (client) whether its the client/secret or user with permissions on the Ent App
                ?? user?.Claims.FirstOrDefault(c => c.Type == "appid")?.Value
                //TODO: Remove this default or specify a system audit identity (for background services)
                ?? "NoAuthImplemented"
                ;

            //determine tenantId from token claim or header
            string? tenantId =
                //AAD from user
                user?.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

            return new RequestContext<string>(correlationId, auditId, tenantId);
        });

        //Infrastructure Services
        services.AddSingleton<IInternalBroker, InternalBroker>();
        services.AddSingleton<IMessageHandler<AuditEntry>, AuditHandler>();

        //https://learn.microsoft.com/en-us/aspnet/core/fundamentals/change-tokens?view=aspnetcore-8.0
        //services.AddSingleton<IDatabaseConfigurationChangeToken, DatabaseChangeToken>();

        //EF-sql repositories
        services.AddScoped<ITodoRepositoryTrxn, TodoRepositoryTrxn>();
        services.AddScoped<ITodoRepositoryQuery, TodoRepositoryQuery>();

        services.AddScoped<AuditInterceptor>();

        //Database 
        var trxnDBconnectionString = config.GetConnectionString("TodoDbContextTrxn");
        if (string.IsNullOrEmpty(trxnDBconnectionString) || trxnDBconnectionString == "UseInMemoryDatabase")
        {
            //multiple in memory DbContexts use the same DB
            //InMemory for dev; requires Microsoft.EntityFrameworkCore.InMemory
            var inMemoryDatabaseRoot = new InMemoryDatabaseRoot();

            services
                .AddDbContext<TodoDbContextTrxn>((sp, opt) =>
                {
                    var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
                    opt.UseInMemoryDatabase("TodoDbContext", inMemoryDatabaseRoot).AddInterceptors(auditInterceptor);
                });

            services
                .AddDbContext<TodoDbContextQuery>((sp, opt) =>
                    opt.UseInMemoryDatabase("TodoDbContext", inMemoryDatabaseRoot));
        }
        else
        {
            //consider a pooled factory - https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#dbcontext-pooling

            //sp is not scoped for the DbContextPool, so we can't use it to get the auditInterceptor
            services.AddDbContext<TodoDbContextTrxn>((sp, options) =>
            {
                var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
                options.UseSqlServer(trxnDBconnectionString,
                    //retry strategy does not support user initiated transactions 
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                        //use relational null semantics 3-valued logic (true, false, null) instead of c# which may generate less efficient sql, but LINQ queries will have a different meaning
                        //https://learn.microsoft.com/en-us/ef/core/querying/null-comparisons
                        //sqlOptions.UseRelationalNulls(true);
                    })
                    .UseExceptionProcessor() //useable exceptions - https://github.com/Giorgi/EntityFramework.Exceptions
                    .AddInterceptors(auditInterceptor);
            });

            var queryDBconnectionString = config.GetConnectionString("TodoDbContextQuery");
            services.AddDbContext<TodoDbContextQuery>(options =>
                options.UseSqlServer(queryDBconnectionString,
                    //retry strategy does not support user initiated transactions 
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                        //use relational null semantics 3-valued logic (true, false, null) instead of c# which may generate less efficient sql, but LINQ queries will have a different meaning
                        //https://learn.microsoft.com/en-us/ef/core/querying/null-comparisons
                        //sqlOptions.UseRelationalNulls(true);
                        //default to split queries to avoid cartesian explosion when joining (multiple includes at the same level)
                        //https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
                        //sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    })
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                );

            //SQL ALWAYS ENCRYPTED, the connection string must include "Column Encryption Setting=Enabled"
            if (!_keyStoreProviderRegistered)
            {
                var credential = new DefaultAzureCredential();
                SqlColumnEncryptionAzureKeyVaultProvider sqlColumnEncryptionAzureKeyVaultProvider = new(credential);

                try
                {
                    SqlConnection.RegisterColumnEncryptionKeyStoreProviders(customProviders: new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(capacity: 1, comparer: StringComparer.OrdinalIgnoreCase)
                    {
                        {
                            SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, sqlColumnEncryptionAzureKeyVaultProvider
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

            //used in AuditInterceptor to hold the audit entries for the current transaction
            //services.AddKeyedScoped<List<AuditEntry>>("Audit", (_, _) => []);
        }

        //IConfigurationSection configSection;

        //Azure Service Clients - Blob, EventGridPublisher, KeyVault, etc; enables injecting IAzureClientFactory<>
        //https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection
        //https://devblogs.microsoft.com/azure-sdk/lifetime-management-and-thread-safety-guarantees-of-azure-sdk-net-clients/
        //https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.azure.azureclientfactorybuilder?view=azure-dotnet
        //https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Microsoft.Extensions.Azure/1.0.0/index.html
        services.AddAzureClients(builder =>
            {
                // Set up any default settings
                builder.ConfigureDefaults(config.GetSection("AzureClientDefaults"));
                // Use DefaultAzureCredential by default
                builder.UseCredential(new DefaultAzureCredential());

                var egpConfigSection = config.GetSection("EventGridPublisher1");
                if (egpConfigSection.Exists())
                {
                    //Ideally use TopicEndpoint Uri (w/DefaultAzureCredential)
                    builder.AddEventGridPublisherClient(new Uri(egpConfigSection.GetValue<string>("TopicEndpoint")!),
                        new AzureKeyCredential(egpConfigSection.GetValue<string>("Key")!))
                    .WithName("EventGridPublisher1");
                }

                //Azure OpenAI
                var azureOpenIAConfigSection = config.GetSection("AzureOpenAI");
                if (azureOpenIAConfigSection.Exists())
                {
                    // Register a custom client factory since this client does not currently have a service registration method
                    builder.AddClient<AzureOpenAIClient, AzureOpenAIClientOptions>((options, _, _) =>
                    {
                        var key = azureOpenIAConfigSection.GetValue<string?>("Key", null);
                        if (!string.IsNullOrEmpty(key))
                        {
                            return new AzureOpenAIClient(new Uri(azureOpenIAConfigSection.GetValue<string>("Url")!), new AzureKeyCredential(key), options);
                        }
                        //this throws internally when running local (no network for managed identity check) but subsequent checks succeed; could avoid with defaultAzCredOptions.ExcludeManagedIdentityCredential = true;
                        return new AzureOpenAIClient(new Uri(azureOpenIAConfigSection.GetValue<string>("Url")!), new DefaultAzureCredential(), options);
                    }).WithName("AzureOpenAI");

                    //Experimental AssistantsClient (client factory does not currently support this client)
                    services.AddScoped<AssistantsClient>(provider =>
                    {
                        var key = azureOpenIAConfigSection.GetValue<string?>("Key", null);
                        if (!string.IsNullOrEmpty(key))
                        {
                            return new AssistantsClient(new Uri(azureOpenIAConfigSection.GetValue<string>("Url")!), new AzureKeyCredential(key));
                        }
                        return new AssistantsClient(new Uri(azureOpenIAConfigSection.GetValue<string>("Url")!), new DefaultAzureCredential());
                    });
                }

                ////Azure OpenAI
                //var jobChatConfigSection = config.GetSection(JobChatSettings.ConfigSectionName);
                //if (jobChatConfigSection.Exists())
                //{
                //    // Register a custom client factory since this client does not currently have a service registration method
                //    builder.AddClient<AzureOpenAIClient, AzureOpenAIClientOptions>((options, _, _) =>
                //    {
                //        var key = jobChatConfigSection.GetValue<string?>("Key", null);
                //        if (!string.IsNullOrEmpty(key))
                //        {
                //            return new AzureOpenAIClient(new Uri(jobChatConfigSection.GetValue<string>("Url")!), new AzureKeyCredential(key), options);
                //        }

                //        //chat IAM role Cognitive Services OpenAI User
                //        //this throws internally when running local (no network for managed identity check) but subsequent checks succeed; could avoid with defaultAzCredOptions.ExcludeManagedIdentityCredential = true;
                //        return new AzureOpenAIClient(new Uri(jobChatConfigSection.GetValue<string>("Url")!), new DefaultAzureCredential(), options);
                //    });

                //    //AzureOpenAI chat service wrapper (not an Azure Client but a wrapper that uses it)
                //    services.AddTransient<IJobChatService, JobChatService>();
                //    services.Configure<JobChatSettings>(jobChatConfigSection);
                //}
            });

        //Chaos - https://medium.com/@tauraigombera/chaos-engineering-with-net-e3a194426940
        var configSectionChaos = config.GetSection(ChaosManagerSettings.ConfigSectionName);
        if (configSectionChaos.Exists() && configSectionChaos.GetValue<bool>("Enabled"))
        {
            services.AddHttpContextAccessor(); //only needed to inject ChaosManager to check query string for chaos
            services.TryAddSingleton<IChaosManager, ChaosManager>();
            services.Configure<ChaosManagerSettings>(configSectionChaos);
        }

        //external SampleAppApi
        var sampleApiconfigSection = config.GetSection(SampleApiRestClientSettings.ConfigSectionName);
        if (sampleApiconfigSection.Exists())
        {
            services.Configure<SampleApiRestClientSettings>(sampleApiconfigSection);

            services.AddScoped(provider =>
            {
                //DefaultAzureCredential checks env vars first, then checks other - managed identity, etc
                //so if we need a 'client' Entra App Reg, set the env vars
                if (config.GetValue<string>("SampleApiRestClientSettings:ClientId") != null)
                {
                    Environment.SetEnvironmentVariable("AZURE_TENANT_ID", config.GetValue<string>("SampleApiRestClientSettings:TenantId"));
                    Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", config.GetValue<string>("SampleApiRestClientSettings:ClientId"));
                    Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", config.GetValue<string>("SampleApiRestClientSettings:ClientSecret"));
                }
                var scopes = config.GetSection("SampleApiRestClientSettings:Scopes").Get<string[]>();
                return new SampleRestApiAuthMessageHandler(scopes!);
            });

            var httpClientBuilder = services.AddHttpClient<ISampleApiRestClient, SampleApiRestClient>(provider =>
            {
                provider.BaseAddress = new Uri(config.GetValue<string>("SampleApiRestClientSettings:BaseUrl")!); //HttpClient will get injected
            })
            .AddHttpMessageHandler<SampleRestApiAuthMessageHandler>(); //SendAysnc pipeline gets/caches access token

            //resiliency
            //.AddPolicyHandler(PollyRetry.GetHttpRetryPolicy())
            //.AddPolicyHandler(PollyRetry.GetHttpCircuitBreakerPolicy());
            //Microsoft.Extensions.Http.Resilience - https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience?tabs=dotnet-cli

            //https://devblogs.microsoft.com/dotnet/resilience-and-chaos-engineering/
            //Polly.Core referenced alongside Microsoft.Extensions.Http.Resilience to ensure access to the latest Polly version featuring chaos strategies.
            //Once Microsoft.Extensions.Http.Resilience incorporates the latest Polly.Core, remove Polly.Core
            //Adding standard resilience to handle the chaos, optinally configure details
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

            //chaos - always after standard resilience handler
            if (configSectionChaos.Exists() && configSectionChaos.GetValue<bool>("Enabled"))
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
                           OutcomeGenerator = new OutcomeGenerator<HttpResponseMessage>().AddResult(() => new HttpResponseMessage(chaosManager.OutcomHttpStatusCode()))
                       })
                       //introduce a specific behavior as chaos
                       //.AddChaosBehavior(0.001, cancellationToken => RestartRedisAsync(cancellationToken)) // Introduce a specific behavior as chaos
                       ;
                });
            }


            //needs to be running in an HttpContext; otherwise no headers to propagate (breaks integration tests)
            if (hasHttpContext)
            {
                httpClientBuilder.AddCorrelationIdForwarding();
                httpClientBuilder.AddHeaderPropagation();
            }
        }

        //OpenAI chat service
        //configSection = config.GetSection(ChatServiceSettings.ConfigSectionName);
        //if (configSection.Exists())
        //{
        //    services.AddScoped<IChatService, ChatService>();
        //    services.Configure<ChatServiceSettings>(config.GetSection(ChatServiceSettings.ConfigSectionName));
        //}

        //external weather service
        var weatherServiceConfigSection = config.GetSection(WeatherServiceSettings.ConfigSectionName);
        if (weatherServiceConfigSection.Exists())
        {
            services.Configure<WeatherServiceSettings>(weatherServiceConfigSection);
            services.AddScoped<IWeatherService, WeatherService>();

            services.AddHttpClient<IWeatherService, WeatherService>(client =>
            {
                client.BaseAddress = new Uri(config.GetValue<string>("WeatherServiceSettings:BaseUrl")!);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Key", config.GetValue<string>("WeatherServiceSettings:Key")!);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Host", config.GetValue<string>("WeatherServiceSettings:Host")!);
            })
            //resiliency
            //.AddPolicyHandler(PollyRetry.GetHttpRetryPolicy())
            //.AddPolicyHandler(PollyRetry.GetHttpCircuitBreakerPolicy());
            //Microsoft.Extensions.Http.Resilience - https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience?tabs=dotnet-cli
            .AddStandardResilienceHandler();
        }

        #region Jobs

        //jobs api service
        var jobsApiConfigSection = config.GetSection(JobsApiServiceSettings.ConfigSectionName);
        if (jobsApiConfigSection.Exists())
        {
            services.Configure<JobsApiServiceSettings>(jobsApiConfigSection);
            services.AddScoped<IJobsApiService, JobsApiService>();
            services.AddHttpClient<IJobsApiService, JobsApiService>(client =>
            {
                client.BaseAddress = new Uri(jobsApiConfigSection.GetValue<string>("BaseUrl")!);
            })
            //Microsoft.Extensions.Http.Resilience - https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience?tabs=dotnet-cli
            .AddStandardResilienceHandler();
        }

        var jobChatConfigSection = config.GetSection(JobChatSettings.ConfigSectionName);
        if (jobChatConfigSection.Exists())
        {
            //AzureOpenAI chat service wrapper (not an Azure Client but a wrapper that uses it)
            services.AddTransient<IJobChatService, JobChatService>();
            services.Configure<JobChatSettings>(jobChatConfigSection);
        }

        var jobAssistantConfigSection = config.GetSection(JobAssistantSettings.ConfigSectionName);
        if (jobAssistantConfigSection.Exists())
        {
            //AzureOpenAI assistant service wrapper (not an Azure Client but a wrapper that uses it)
            services.AddTransient<IJobAssistantService, JobAssistantService>();
            services.Configure<JobAssistantSettings>(jobAssistantConfigSection);
        }

        #endregion

        //StartupTasks - executes once at startup
        services.AddTransient<IStartupTask, LoadCache>();

        return services;
    }

    public static IServiceCollection RegisterBackgroundServices(this IServiceCollection services, IConfiguration config)
    {
        //background task
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<BackgroundTaskService>();

        //scheduler
        var configSection = config.GetSection(CronServiceSettings.ConfigSectionName);
        if (configSection.Exists())
        {
            services.Configure<CronJobBackgroundServiceSettings<CustomCronJob>>(configSection);
            services.AddHostedService<CronService>();
        }

        return services;
    }
}
