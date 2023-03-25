using Application.Contracts.Services;
using Application.Services;
using Infrastructure.BackgroundServices;
using Infrastructure.Data;
using Infrastructure.RapidApi.WeatherApi;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleApp.Bootstrapper.Automapper;
using SampleApp.Bootstrapper.HealthChecks;
using SampleApp.Bootstrapper.StartupTasks;
using System;
using System.Net.Http;
using System.Net;
using Polly;
using Polly.Extensions.Http;
using System.Linq;

namespace SampleApp.Bootstrapper;

public class Startup
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _config;

    //multiple in memory DbContexts use the same DB
    private readonly InMemoryDatabaseRoot InMemoryDatabaseRoot = new();

    public Startup(IServiceCollection services, IConfiguration config)
    {
        _services = services;
        _config = config;
    }

    /// <summary>
    /// Register/configure services in the container
    /// </summary>
    public void ConfigureServices()
    {
        ConfigureInfrastructureServices();
        ConfigureApplicationServices();
    }

    /// <summary>
    /// Register/configure services in the container
    /// </summary>
    public void ConfigureApplicationServices()
    {
        //application Services
        _services.AddScoped<ITodoService, TodoService>();
        _services.Configure<TodoServiceSettings>(_config.GetSection(TodoServiceSettings.ConfigSectionName));

        //domain services
    }

    public void ConfigureInfrastructureServices()
    {
        //LazyCache.AspNetCore, lightweight wrapper around memorycache; prevent race conditions when multiple threads attempt to refresh empty cache item
        _services.AddLazyCache();

        //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-6.0
        string? connectionString = _config.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(connectionString))
        {
            _services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = "redis1";
            });
        }
        else
        {
            _services.AddDistributedMemoryCache(); //local server only, not distributed. Helps with tests
        }

        //AutoMapper Configuration - map domain <-> application 
        ConfigureAutomapper.Configure(_services);

        //Infrastructure Services
        _services.AddTransient<ITodoRepositoryTrxn, TodoRepositoryTrxn>();
        _services.AddTransient<ITodoRepositoryQuery, TodoRepositoryQuery>();

        _services.Configure<WeatherServiceSettings>(_config.GetSection(WeatherServiceSettings.ConfigSectionName));

        //external weather service
        _services.AddScoped<IWeatherService, WeatherService>();
        _services.AddHttpClient<IWeatherService, WeatherService>(client =>
        {
            client.BaseAddress = new Uri(_config.GetValue<string>("WeatherSettings:BaseUrl")!);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Key", _config.GetValue<string>("WeatherSettings:Key")!);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Host", _config.GetValue<string>("WeatherSettings:Host")!);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        //Database - transaction
        connectionString = _config.GetConnectionString("TodoDbContextTrxn");
        if (string.IsNullOrEmpty(connectionString) || connectionString == "UseInMemoryDatabase")
        {
            //InMemory for dev; requires Microsoft.EntityFrameworkCore.InMemory
            _services.AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<TodoDbContextTrxn>((sp, opt) => opt.UseInternalServiceProvider(sp).UseInMemoryDatabase("TodoDbContext", InMemoryDatabaseRoot));
        }
        else
        {
            _services.AddDbContextPool<TodoDbContextTrxn>(options =>
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

        //Database - query
        connectionString = _config.GetConnectionString("TodoDbContextQuery");
        if (string.IsNullOrEmpty(connectionString) || connectionString == "UseInMemoryDatabase")
        {
            //InMemory for dev; requires Microsoft.EntityFrameworkCore.InMemory
            _services.AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<TodoDbContextQuery>((sp, opt) => opt.UseInternalServiceProvider(sp).UseInMemoryDatabase("TodoDbContext", InMemoryDatabaseRoot));

        }
        else
        {
            _services.AddDbContextPool<TodoDbContextQuery>(options =>
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

        //infrastructure service
        _services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
    }

    /// <summary>
    /// Used at runtime for http services; not used for Workers/Functions/Tests
    /// </summary>
    public void ConfigureRuntimeServices()
    {
        //HealthChecks - having infrastructure references
        //tag full will run when hitting health/full
        _services.AddHealthChecks()
            .AddMemoryHealthCheck("memory", tags: new[] { "full", "memory" }, thresholdInBytes: _config.GetValue<long>("MemoryHealthCheckBytesThreshold", 1024L * 1024L * 1024L))
            .AddDbContextCheck<TodoDbContextTrxn>("TodoDbContextTrxn", tags: new[] { "full", "db" })
            .AddDbContextCheck<TodoDbContextQuery>("TodoDbContextQuery", tags: new[] { "full", "db" })
            .AddCheck<ExternalServiceHealthCheck>("External Service", tags: new[] { "full", "extservice" });

        //background services - infrastructure
        _services.AddHostedService<BackgroundTaskService>();

        //StartupTasks - executes once at startup
        _services.AddTransient<IStartupTask, LoadCache>(); 
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int numRetries = 5, int secDelay = 2, HttpStatusCode[]? retryHttpStatusCodes = null)
    {
        Random jitterer = new Random();
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
