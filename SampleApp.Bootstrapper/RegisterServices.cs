using Application.Contracts.Services;
using Application.Services;
using CorrelationId.Abstractions;
using Infrastructure.Data;
using Infrastructure.RapidApi.WeatherApi;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

        //AutoMapper Configuration - map domain <-> application 
        ConfigureAutomapper.Configure(services);

        //Infrastructure Services

        //EF-sql repositories
        services.AddScoped<ITodoRepositoryTrxn, TodoRepositoryTrxn>();
        services.AddScoped<ITodoRepositoryQuery, TodoRepositoryQuery>();

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
        //integration testing breaks since there is no header to propagate
        //apply to internal service proxies as needed; just a sample, doesn't apply to RapidAPI
        //.AddHeaderPropagation() 
        //.AddCorrelationIdForwarding()

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
                return new RequestContext(correlationId, $"BackgroundService-{correlationId}");
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

            return new RequestContext(correlationContext!.CorrelationId, auditId);
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
