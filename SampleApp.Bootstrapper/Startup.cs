using Application.Contracts.Services;
using Application.Services;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleApp.Bootstrapper.Automapper;
using System;

namespace SampleApp.Bootstrapper;

public class Startup
{
    public readonly IConfiguration _config;

    public Startup(IConfiguration config)
    {
        _config = config;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        //Application Services
        services.AddTransient<ITodoService, TodoService>();
        services.Configure<TodoServiceSettings>(_config.GetSection("TodoServiceSettings"));

        //AutoMapper Configuration - map domain <-> application 
        ConfigureAutomapper.Configure(services);

        //Infrastructure Services
        services.AddTransient<ITodoRepository, TodoRepository>();

        //Database
        string connectionString = _config.GetConnectionString("TodoContext");
        if (string.IsNullOrEmpty(connectionString) || connectionString == "UseInMemoryDatabase")
        {
            //InMemory for dev; requires Microsoft.EntityFrameworkCore.InMemory
            services.AddDbContextPool<TodoContext>(opt => opt.UseInMemoryDatabase("TodoContext"));
        }
        else
        {
            services.AddDbContextPool<TodoContext>(options =>
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
