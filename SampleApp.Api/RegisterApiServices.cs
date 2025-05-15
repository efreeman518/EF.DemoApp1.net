using Application.Contracts.Model;
using Asp.Versioning;
using FluentValidation;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Identity.Web;
using Package.Infrastructure.AspNetCore.Filters;
using Package.Infrastructure.AspNetCore.HealthChecks;
using Package.Infrastructure.Auth.Handlers;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Grpc;
using SampleApp.Api.ExceptionHandlers;
using SampleApp.Bootstrapper.HealthChecks;
using SampleApp.Support.Validators;

namespace SampleApp.Api;

internal static class IServiceCollectionExtensions
{
    internal static readonly string[] healthCheckTagsFullMem = ["full", "memory"];
    internal static readonly string[] healthCheckTagsFullDb = ["full", "db"];
    internal static readonly string[] healthCheckTagsFullExt = ["full", "extservice"];

    /// <summary>
    /// Used at runtime for http services; not used for Workers/Functions/Tests
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterApiServices(this IServiceCollection services, IConfiguration config, ILogger logger, string serviceName)
    {
        AddCorsPolicy(services, config, logger);
        AddAuthentication(services, config, logger);
        AddErrorHandlingAndValidation(services);
        AddGrpcServices(services);
        services.AddRouting(options => options.LowercaseUrls = true);
        var apiVersioningBuilder = AddApiVersioning(services);
        AddOpenApiSupport(services, config, apiVersioningBuilder);
        AddChatGptPlugin(services, config);
        AddHealthChecks(services, config);
        AddCorrelationTracking(services);
        //services.AddControllers();

        return services;
    }

    private static IApiVersioningBuilder AddApiVersioning(IServiceCollection services)
    {
        return services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
    }

    private static void AddCorsPolicy(IServiceCollection services, IConfiguration config, ILogger logger)
    {
        string corsConfigSectionName = "Cors";
        var corsConfigSection = config.GetSection(corsConfigSectionName);
        if (corsConfigSection.GetChildren().Any())
        {
            var policyName = corsConfigSection.GetValue<string>("PolicyName")!;
            logger.LogInformation("Configure CORS - {PolicyName}", policyName);
            services.AddCors(options =>
            {
                options.AddPolicy(policyName, builder =>
                {
                    var origins = corsConfigSection.GetSection("AllowedOrigins").Get<string[]>();
                    builder.WithOrigins(origins!)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials(); //does not work with AllowAnyOrigin()
                });
            });
        }
    }

    private static void AddAuthentication(IServiceCollection services, IConfiguration config, ILogger logger)
    {
        string authConfigSectionName = "Api1_EntraID";
        var configSection = config.GetSection(authConfigSectionName);
        if(!configSection.GetChildren().Any())
        {
            logger.LogInformation("No Auth Config ({ConfigSectionName}) Found; Auth will not be configured.", authConfigSectionName);
            services.AddAuthentication();
            return;
        }
        logger.LogInformation("Configure auth - {ConfigSectionName}", authConfigSectionName);

        //debug
        var authConfig = configSection.GetChildren().ToDictionary(cs => cs.Key, cs => cs.Value);
        logger.LogInformation("Auth Config: {ConfigSection}", authConfig.SerializeToJson());

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddMicrosoftIdentityWebApi(config.GetSection(authConfigSectionName));

        services.AddSingleton<IAuthorizationHandler, RolesOrScopesAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            //require authenticated user globally, except explicit [AllowAnonymous] endpoints  
            //Fallback Policy for all endpoints that do not have any authorization defined, except explicit [AllowAnonymous] endpoints
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            //Default Policy for all endpoints that require authorization ([Authorize] authenticated user) already but no other specifics
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            //define policies based on specific roles/scopes
            .AddPolicy("StandardAccessPolicy1", policy => policy.RequireRole("StandardAccess"))
            .AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"))
            .AddPolicy("SomeScopePolicy1", policy => policy.RequireScope("SomeScope1"))
            .AddPolicy("ScopeOrRolePolicy1", policy => policy.AddRequirements(new RolesOrScopesRequirement(["SomeAccess1"], ["SomeScope1"])))
            .AddPolicy("ScopeOrRolePolicy2", policy =>
            {
                var defaultRoles = configSection.GetSection("AppPermissions:Default").Get<string[]>();
                var defaultScopes = configSection.GetSection("Scopes:Default").Get<string[]>();
                policy.AddRequirements(new RolesOrScopesRequirement(defaultRoles, defaultScopes));
            });
    }

    private static void AddErrorHandlingAndValidation(IServiceCollection services)
    {
        services.AddExceptionHandler<DefaultExceptionHandler>();

        //this should be deprecated with net10 minimal api validation
        services.AddTransient<IValidator<TodoItemDto>, TodoItemDtoValidator>();

        //convenient for model validation; built in IHostEnvironmentExtensions.BuildProblemDetailsResponse
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = (context) =>
            {
                context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
                var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
                context.ProblemDetails.Extensions.TryAdd("activityId", activity?.Id);
            };
        });
    }

    private static void AddGrpcServices(IServiceCollection services)
    {
        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaxReceiveMessageSize = 100000;
            options.Interceptors.Add<ServiceErrorInterceptor>();
        });
        services.AddScoped<ServiceErrorInterceptor>();
    }

    private static void AddOpenApiSupport(IServiceCollection services, IConfiguration config, IApiVersioningBuilder apiVersioningBuilder)
    {
        if (!config.GetValue("OpenApiSettings:Enable", false))
        {
            return;
        }

        //https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-9.0&tabs=visual-studio%2Cminimal-apis
        services
            .AddOpenApi("v1")
            .AddOpenApi("v1.1");

        apiVersioningBuilder.AddApiExplorer(o =>
        {
            o.GroupNameFormat = "'v'VVV";
            o.SubstituteApiVersionInUrl = true;
        });
    }

    private static void AddChatGptPlugin(IServiceCollection services, IConfiguration config)
    {
        if (config.GetValue("ChatGPT_Plugin:Enable", false))
        {
            services.AddCors(options =>
            {
                options.AddPolicy("ChatGPT", policy =>
                {
                    policy.WithOrigins("https://chat.openai.com", config.GetValue<string>("ChatGPT_Plugin:Url")!)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }
    }

    //HealthChecks - having infrastructure references
    //search nuget aspnetcore.healthchecks - many prebuilt health checks 
    //tag full will run when hitting health/full
    private static void AddHealthChecks(IServiceCollection services, IConfiguration config)
    {
        services.AddHealthChecks()
            .AddMemoryHealthCheck(
                "memory",
                tags: healthCheckTagsFullMem,
                thresholdInBytes: config.GetValue<long>("MemoryHealthCheckBytesThreshold", 1024L * 1024L * 1024L))
            .AddDbContextCheck<TodoDbContextTrxn>("TodoDbContextTrxn", tags: healthCheckTagsFullDb)
            .AddDbContextCheck<TodoDbContextQuery>("TodoDbContextQuery", tags: healthCheckTagsFullDb)
            .AddCheck<WeatherServiceHealthCheck>("External Service", tags: healthCheckTagsFullExt);
    }

    private static void AddCorrelationTracking(IServiceCollection services)
    {
        services.AddHeaderPropagation(options =>
        {
            options.Headers.Add("X-Correlation-ID");
        });
        services.AddHttpContextAccessor();
        services.AddTransient<IStartupFilter, CorrelationIdStartupFilter>();
    }
}
