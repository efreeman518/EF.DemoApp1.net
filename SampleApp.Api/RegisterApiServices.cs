using Application.Contracts.Model;
using Asp.Versioning;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Identity.Web;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Package.Infrastructure.AspNetCore.Filters;
using Package.Infrastructure.AspNetCore.HealthChecks;
using Package.Infrastructure.Auth.Handlers;
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
    /// <param name="loggerStartup"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterApiServices(this IServiceCollection services, IConfiguration config, ILogger loggerStartup, string serviceName)
    {
        //Application Insights telemetry for http services (for logging telemetry directly to AI)
        var appInsightsConnectionString = config["ApplicationInsights:ConnectionString"];

        services.AddOpenTelemetry()
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            })
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(builder =>
            {
                builder
                    //.SetSampler(new TraceIdRatioBasedSampler(0.1)) // Sample 10% of traces
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Microsoft.EntityFrameworkCore"); //capture the sql
                    //.AddAzureMonitorTraceExporter(options =>
                    //{
                    //    options.ConnectionString = appInsightsConnectionString;
                    //});
            })
            .WithMetrics(builder =>
            {
                builder
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation();
                    //.AddAzureMonitorMetricExporter(options =>
                    //{
                    //    options.ConnectionString = appInsightsConnectionString;
                    //});
            });

        //api versioning
        var apiVersioningBuilder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true; //response header with supported versions
            options.ApiVersionReader = new UrlSegmentApiVersionReader(); // /v1.1/context/method, can combine multiple versioning approaches
        });

        string corsConfigSectionName = "Cors";
        var corsConfigSection = config.GetSection(corsConfigSectionName);
        if (corsConfigSection.GetChildren().Any())
        {
            var policyName = corsConfigSection.GetValue<string>("PolicyName")!;
            loggerStartup.LogInformation("Configure CORS - {PolicyName}", policyName);
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

        //Auth
        string authConfigSectionName = "Api1_EntraID";
        var configSection = config.GetSection(authConfigSectionName);
        if (configSection.GetChildren().Any())
        {
            //https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-verification-scope-app-roles?tabs=aspnetcore
            //https://andrewlock.net/setting-global-authorization-policies-using-the-defaultpolicy-and-the-fallbackpolicy-in-aspnet-core-3/
            //https://learn.microsoft.com/en-us/entra/external-id/customers/tutorial-protect-web-api-dotnet-core-build-app

            loggerStartup.LogInformation("Configure auth - {ConfigSectionName}", authConfigSectionName);

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddMicrosoftIdentityWebApi(config.GetSection(authConfigSectionName));

            //https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-api-call-api-app-configuration?tabs=aspnetcore
            //.EnableTokenAcquisitionToCallDownstreamApi() 
            //.AddInMemoryTokenCaches()

            services.AddSingleton<IAuthorizationHandler, RolesOrScopesAuthorizationHandler>();

            //simple role- or claim-based authorization
            //services.AddAuthorization();

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
                })
             ;
        }

        //global unhandled exception handler
        services.AddExceptionHandler<DefaultExceptionHandler>();

        //this should be deprecated with net10 minimal api validation
        //if needed
        //services.AddScoped<IValidatorDiscovery, ValidatorDiscovery>();
        services.AddTransient<IValidator<TodoItemDto>, TodoItemDtoValidator>();

        //controllers?
        //services.AddControllers();

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

        //Add gRPC framework services
        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaxReceiveMessageSize = 100000; //bytes
            options.Interceptors.Add<ServiceErrorInterceptor>();
        });
        services.AddScoped<ServiceErrorInterceptor>();

        services.AddRouting(options => options.LowercaseUrls = true);

        if (config.GetValue("OpenApiSettings:Enable", false))
        {
            //.net9
            //https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-9.0&tabs=visual-studio%2Cminimal-apis
            services
                .AddOpenApi("v1")
                .AddOpenApi("v1.1");

            apiVersioningBuilder.AddApiExplorer(o =>
            {
                // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                // note: the specified format code will format the version as "'v'major[.minor][-status]"
                o.GroupNameFormat = "'v'VVV";

                // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                o.SubstituteApiVersionInUrl = true;
            });
        }

        //ChatGPT plugin
        if (config.GetValue("ChatGPT_Plugin:Enable", false))
        {
            services.AddCors(options =>
            {
                options.AddPolicy("ChatGPT", policy =>
                {
                    policy.WithOrigins("https://chat.openai.com", config.GetValue<string>("ChatGPT_Plugin:Url")!).AllowAnyHeader().AllowAnyMethod();
                });
            });
        }

        //HealthChecks - having infrastructure references
        //search nuget aspnetcore.healthchecks - many prebuilt health checks 
        //tag full will run when hitting health/full

        services.AddHealthChecks()
            .AddMemoryHealthCheck("memory", tags: healthCheckTagsFullMem, thresholdInBytes: config.GetValue<long>("MemoryHealthCheckBytesThreshold", 1024L * 1024L * 1024L))
            .AddDbContextCheck<TodoDbContextTrxn>("TodoDbContextTrxn", tags: healthCheckTagsFullDb)
            .AddDbContextCheck<TodoDbContextQuery>("TodoDbContextQuery", tags: healthCheckTagsFullDb)
            .AddCheck<WeatherServiceHealthCheck>("External Service", tags: healthCheckTagsFullExt);

        //for http clients previously registered in infrastructure services, add header propagation here since it only applies at runtime when an http context is present
        services.AddHeaderPropagation(options =>
        {
            options.Headers.Add("X-Correlation-ID");
        });
        //generate correlation ID if not present
        services.AddHttpContextAccessor();
        services.AddTransient<IStartupFilter, CorrelationIdStartupFilter>();

        return services;
    }
}
