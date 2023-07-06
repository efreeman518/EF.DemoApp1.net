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
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Common;
using Package.Infrastructure.CosmosDb;
using Package.Infrastructure.OpenAI.ChatApi;
using Package.Infrastructure.Storage;
using Polly;
using Polly.Extensions.Http;
using SampleApp.BackgroundServices.Scheduler;
using SampleApp.Bootstrapper.Automapper;
using SampleApp.Bootstrapper.StartupTasks;
using SampleApp.Grpc;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;

namespace SampleApp.Bootstrapper;

public static class IServiceCollectionExtensions
{
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
            new System.Collections.Generic.List<AutoMapper.Profile>
            {
                new MappingProfile(),  //map domain <-> app 
                new GrpcMappingProfile() // map grpc <-> app 
            });

        //Infrastructure Services

        //EF-sql repositories
        services.AddScoped<ITodoRepositoryTrxn, TodoRepositoryTrxn>();
        services.AddScoped<ITodoRepositoryQuery, TodoRepositoryQuery>();

        //external SampleAppApi
        services.Configure<SampleApiRestClientSettings>(config.GetSection(SampleApiRestClientSettings.ConfigSectionName));
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
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
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

        //OpenAI chat service
        services.AddScoped<IChatService, ChatService>();
        services.Configure<ChatServiceSettings>(config.GetSection(ChatServiceSettings.ConfigSectionName));

        //external weather service
        services.Configure<WeatherServiceSettings>(config.GetSection(WeatherServiceSettings.ConfigSectionName));
        services.AddScoped<IWeatherService, WeatherService>();
        services.AddHttpClient<IWeatherService, WeatherService>(client =>
        {
            client.BaseAddress = new Uri(config.GetValue<string>("WeatherServiceSettings:BaseUrl")!);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Key", config.GetValue<string>("WeatherServiceSettings:Key")!);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Host", config.GetValue<string>("WeatherServiceSettings:Host")!);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

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
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    })
                );

            services.AddDbContextPool<TodoDbContextQuery>(options =>
                options.UseSqlServer(connectionString,
                    //retry strategy does not support user initiated transactions 
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    })
                );
        }

        //CosmosDb - CosmosClient is thread-safe. Its recommended to maintain a single instance of CosmosClient per lifetime of the application which enables efficient connection management and performance.
        connectionString = config.GetConnectionString("CosmosDB");
        if (string.IsNullOrEmpty(connectionString))
        {
            services.AddSingleton(provider =>
            {
                return new CosmosDbRepositorySettings
                {
                    CosmosClient = new CosmosClient(config.GetConnectionString("CosmosDB")),
                    DbId = config.GetValue<string>("CosmosDbId")
                };
            });
            services.AddScoped<CosmosDbRepository>();
        }

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

            //Ideally use ServiceUri (w/DefaultAzureCredential)
            builder.AddBlobServiceClient(config.GetSection("ConnectionStrings:BlobStorage")).WithName("AzureBlobStorageAccount1");

            //Ideally use TopicEndpoint Uri (w/DefaultAzureCredential)
            builder.AddEventGridPublisherClient(new Uri(config.GetValue<string>("EventGridPublisher1:TopicEndpoint")!),
                new AzureKeyCredential(config.GetValue<string>("EventGridPublisher1:Key")!))
                .WithName("EventGridPublisher1");
        });

        //BlobStorage
        services.AddSingleton<IAzureBlobStorageManager, AzureBlobStorageManager>();
        services.Configure<AzureBlobStorageManagerSettings>(config.GetSection(AzureBlobStorageManagerSettings.ConfigSectionName));

        //StartupTasks - executes once at startup
        services.AddTransient<IStartupTask, LoadCache>();

        //IRequestContext 
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

            var user = httpContext?.User;

            //Get auditId from token claim /header
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

        return services;
    }

    public static IServiceCollection RegisterBackgroundServices(this IServiceCollection services, IConfiguration config)
    {
        //background task
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<BackgroundTaskService>();

        //scheduler
        services.Configure<CronJobBackgroundServiceSettings<CustomCronJob>>(config.GetSection(CronServiceSettings.ConfigSectionName));
        services.AddHostedService<CronService>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int numRetries = 5, int secDelay = 2) //, HttpStatusCode[]? retryHttpStatusCodes = null)
    {
        Random jitterer = new();
        return HttpPolicyExtensions
            .HandleTransientHttpError() //known transient errors
                                        //.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound) // other errors to consider transient (retry-able)
            .WaitAndRetryAsync(numRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(secDelay, retryAttempt))
                + TimeSpan.FromMilliseconds(jitterer.Next(0, 100))
            );
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int numConsecutiveFaults = 10, int secondsToWait = 30)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(numConsecutiveFaults, TimeSpan.FromSeconds(secondsToWait));
    }
}
