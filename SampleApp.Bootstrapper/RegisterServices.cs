using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Application.Contracts.Services;
using Application.Services;
using Application.Services.Validators;
using Azure;
using Azure.Identity;
using CorrelationId.Abstractions;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.RapidApi.WeatherApi;
using Infrastructure.Repositories;
using Infrastructure.SampleApi;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Common;
using Package.Infrastructure.OpenAI.ChatApi;
using SampleApp.BackgroundServices.Scheduler;
using SampleApp.Bootstrapper.Automapper;
using SampleApp.Bootstrapper.StartupTasks;
using SampleApp.Grpc;
using System.Security.Claims;

namespace SampleApp.Bootstrapper;

public static class IServiceCollectionExtensions
{
    //can only be registered once
    private static bool _keyStoreProviderRegistered = false;

    /// <summary>
    /// Register/configure domain services in the container
    /// </summary>
    public static IServiceCollection RegisterDomainServices(this IServiceCollection services, IConfiguration config)
    {
        _ = services.GetHashCode();
        _ = config.GetHashCode();
        return services;
    }

    /// <summary>
    /// Register/configure services in the container
    /// </summary>
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITodoService, TodoService>();
        services.Configure<TodoServiceSettings>(config.GetSection(TodoServiceSettings.ConfigSectionName));

        services.AddScoped<IValidationHelper, ValidationHelper>();
        services.AddScoped<IValidator<TodoItemDto>, TodoItemValidator>();

        return services;
    }

    public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        //this middleware will check the Azure App Config Sentinel for a change which triggers reloading the configuration
        //middleware triggers on http request, not background service scope
        if (config.GetValue<string>("AzureAppConfig:Endpoint") != null)
        {
            services.AddAzureAppConfiguration();
        }

        //LazyCache.AspNetCore, lightweight wrapper around memorycache; prevent race conditions when multiple threads attempt to refresh empty cache item
        services.AddLazyCache();

        //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-6.0
        string? connectionString = config.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = "redis1";
            });
        }
        else
        {
            services.AddDistributedMemoryCache(); //local server only, not distributed. Helps with tests
        }

        //AutoMapper Configuration
        ConfigureAutomapper.Configure(services,
            new List<AutoMapper.Profile>
            {
                new MappingProfile(),  //map domain <-> app 
                new GrpcMappingProfile() // map grpc <-> app 
            });

        //IRequestContext - injected into repositories
        services.AddScoped<IRequestContext>(provider =>
        {
            var httpContext = provider.GetService<IHttpContextAccessor>()?.HttpContext;
            var correlationContext = provider.GetService<ICorrelationContextAccessor>()?.CorrelationContext;

            //Background services will not have an http context
            if (httpContext == null)
            {
                var correlationId = Guid.NewGuid().ToString();
                return new Package.Infrastructure.Common.RequestContext(correlationId, $"BackgroundService-{correlationId}");
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

            return new Package.Infrastructure.Common.RequestContext(correlationContext!.CorrelationId, auditId);
        });

        //Infrastructure Services

        //DB config change token
        //services.AddSingleton<IDatabaseConfigurationChangeToken, DatabaseChangeToken>();

        //EF-sql repositories
        services.AddScoped<ITodoRepositoryTrxn, TodoRepositoryTrxn>();
        services.AddScoped<ITodoRepositoryQuery, TodoRepositoryQuery>();

        //Database 
        connectionString = config.GetConnectionString("TodoDbContextTrxn");
        if (string.IsNullOrEmpty(connectionString) || connectionString == "UseInMemoryDatabase")
        {
            //multiple in memory DbContexts use the same DB
            //InMemory for dev; requires Microsoft.EntityFrameworkCore.InMemory
            var inMemoryDatabaseRoot = new InMemoryDatabaseRoot();

            services.AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<TodoDbContextTrxn>((sp, opt) => opt.UseInternalServiceProvider(sp).UseInMemoryDatabase("TodoDbContext", inMemoryDatabaseRoot));

            services.AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<TodoDbContextQuery>((sp, opt) => opt.UseInternalServiceProvider(sp).UseInMemoryDatabase("TodoDbContext", inMemoryDatabaseRoot));
        }
        else
        {
            services.AddDbContextPool<TodoDbContextTrxn>(options =>
                options.UseSqlServer(connectionString,
                    //retry strategy does not support user initiated transactions 
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                        //use relational null semantics 3-valued logic (true, false, null) instead of c# which may generate less efficient sql, but LINQ queries will have a different meaning
                        //https://learn.microsoft.com/en-us/ef/core/querying/null-comparisons
                        //sqlOptions.UseRelationalNulls(true);
                    })
                );

            connectionString = config.GetConnectionString("TodoDbContextQuery");
            services.AddDbContextPool<TodoDbContextQuery>(options =>
                options.UseSqlServer(connectionString,
                    //retry strategy does not support user initiated transactions 
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    })
                );

            if (!_keyStoreProviderRegistered)
            {
                //sql always encrypted support; connection string must include "Column Encryption Setting=Enabled"
                var credential = new DefaultAzureCredential();
                SqlColumnEncryptionAzureKeyVaultProvider sqlColumnEncryptionAzureKeyVaultProvider = new(credential);
                SqlConnection.RegisterColumnEncryptionKeyStoreProviders(customProviders: new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(capacity: 1, comparer: StringComparer.OrdinalIgnoreCase)
                 {
                     {
                         SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, sqlColumnEncryptionAzureKeyVaultProvider
                     }
                 });
                _keyStoreProviderRegistered = true;
            }
        }

        IConfigurationSection configSection;

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

            configSection = config.GetSection("EventGridPublisher1");
            if (configSection.Exists())
            {
                //Ideally use TopicEndpoint Uri (w/DefaultAzureCredential)
                builder.AddEventGridPublisherClient(new Uri(configSection.GetValue<string>("TopicEndpoint")!),
                    new AzureKeyCredential(configSection.GetValue<string>("Key")!))
                .WithName("EventGridPublisher1");
            }
        });

        //external SampleAppApi
        configSection = config.GetSection(SampleApiRestClientSettings.ConfigSectionName);
        if (configSection.Exists())
        {
            services.Configure<SampleApiRestClientSettings>(configSection);
            //check auth configured
            var scopes = config.GetSection("SampleApiRestClientSettings:Scopes").Get<string[]>();
            if (scopes != null)
            {
                services.AddScoped(provider =>
                {
                    //DefaultAzureCredential checks env vars first, then checks other - managed identity, etc
                    //so if we need a 'client' AAD App Reg, set the env vars
                    if (config.GetValue<string>("SampleApiRestClientSettings:ClientId") != null)
                    {
                        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", config.GetValue<string>("SampleApiRestClientSettings:TenantId"));
                        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", config.GetValue<string>("SampleApiRestClientSettings:ClientId"));
                        Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", config.GetValue<string>("SampleApiRestClientSettings:ClientSecret"));
                    }
                    return new SampleRestApiAuthMessageHandler(scopes);
                });
            }
            var httpClientBuilder = services.AddHttpClient<ISampleApiRestClient, SampleApiRestClient>(options =>
            {
                options.BaseAddress = new Uri(config.GetValue<string>("SampleApiRestClientSettings:BaseUrl")!); //HttpClient will get injected
            })
            .AddPolicyHandler(PollyRetry.GetHttpRetryPolicy())
            .AddPolicyHandler(PollyRetry.GetHttpCircuitBreakerPolicy());
            //TODO - move this to register Api services
            //integration testing breaks since there is no existing http request, so no headers to propagate
            //'app.UseHeaderPropagation()' required. Header propagation can only be used within the context of an HTTP request, not a test.
            //.AddHeaderPropagation(options =>
            //{
            //    options.Headers.Add("x-request-id");
            //    options.Headers.Add("x-correlation-id");
            //}); 
            //.AddCorrelationIdForwarding();

            //auth is configured
            if (scopes != null)
            {
                httpClientBuilder.AddHttpMessageHandler<SampleRestApiAuthMessageHandler>();
            }
        }

        //OpenAI chat service
        configSection = config.GetSection(ChatServiceSettings.ConfigSectionName);
        if (configSection.Exists())
        {
            services.AddScoped<IChatService, ChatService>();
            services.Configure<ChatServiceSettings>(config.GetSection(ChatServiceSettings.ConfigSectionName));
        }

        //external weather service
        configSection = config.GetSection(WeatherServiceSettings.ConfigSectionName);
        if (configSection.Exists())
        {
            services.Configure<WeatherServiceSettings>(configSection);
            services.AddScoped<IWeatherService, WeatherService>();
            services.AddHttpClient<IWeatherService, WeatherService>(client =>
            {
                client.BaseAddress = new Uri(config.GetValue<string>("WeatherServiceSettings:BaseUrl")!);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Key", config.GetValue<string>("WeatherServiceSettings:Key")!);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Host", config.GetValue<string>("WeatherServiceSettings:Host")!);
            })
            .AddPolicyHandler(PollyRetry.GetHttpRetryPolicy())
            .AddPolicyHandler(PollyRetry.GetHttpCircuitBreakerPolicy());
        }

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
