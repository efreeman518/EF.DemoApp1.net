using Application.Contracts.Services;
using Application.Services;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleApp.Bootstrapper.Automapper;
using System;

namespace SampleApp.Bootstrapper;

public class Startup
{
    public readonly IConfiguration _config;

    //multiple in memory DbContexts use the same DB
    private readonly InMemoryDatabaseRoot InMemoryDatabaseRoot = new();

    public Startup(IConfiguration config)
    {
        _config = config;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        //Application Services
        services.AddTransient<ITodoService, TodoService>();
        services.Configure<TodoServiceSettings>(_config.GetSection(TodoServiceSettings.ConfigSectionName));

        //AutoMapper Configuration - map domain <-> application 
        ConfigureAutomapper.Configure(services);

        //Infrastructure Services
        services.AddTransient<ITodoRepositoryTrxn, TodoRepositoryTrxn>();
        services.AddTransient<ITodoRepositoryQuery, TodoRepositoryQuery>();

        //Database - transaction
        string? connectionString = _config.GetConnectionString("TodoDbContextTrxn");
        if (string.IsNullOrEmpty(connectionString) || connectionString == "UseInMemoryDatabase")
        {
            //InMemory for dev; requires Microsoft.EntityFrameworkCore.InMemory
            services.AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<TodoDbContextTrxn>((sp, opt) => opt.UseInternalServiceProvider(sp).UseInMemoryDatabase("TodoDbContext", InMemoryDatabaseRoot));
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
        }

        //Database - query
        connectionString = _config.GetConnectionString("TodoDbContextQuery");
        if (string.IsNullOrEmpty(connectionString) || connectionString == "UseInMemoryDatabase")
        {
            //InMemory for dev; requires Microsoft.EntityFrameworkCore.InMemory
            services.AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<TodoDbContextQuery>((sp, opt) => opt.UseInternalServiceProvider(sp).UseInMemoryDatabase("TodoDbContext", InMemoryDatabaseRoot));

        }
        else
        {
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

    }
}
