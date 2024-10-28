using Application.Contracts.Model;
using Asp.Versioning;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using CorrelationId.DependencyInjection;
using FluentValidation;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
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
    /// <param name="logger"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterApiServices(this IServiceCollection services, IConfiguration config, ILogger logger)
    {
        //Application Insights telemetry for http services (for logging telemetry directly to AI)
        services.AddOpenTelemetry().UseAzureMonitor(options =>
        {
            options.ConnectionString = config.GetValue<string>("ApplicationInsights:ConnectionString");
        });

        //api versioning
        var apiVersioningBulder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true; //response header with supported versions
            options.ApiVersionReader = new UrlSegmentApiVersionReader(); // /v1.1/context/method, can combine multiple versioning approaches
        });

        //header propagation - implement here since integration testing breaks when there is no existing http request, so no headers to propagate
        services.AddHeaderPropagation();
        //.AddHeaderPropagation(options =>
        //{
        //    options.Headers.Add("x-request-id");
        //    options.Headers.Add("x-correlation-id");
        //    options.Headers.Add("x-username-etc");
        //}); 
        //.AddCorrelationIdForwarding();

        //https://github.com/stevejgordon/CorrelationId/wiki
        services.AddDefaultCorrelationId(options =>
        {
            options.AddToLoggingScope = true;
            options.UpdateTraceIdentifier = true; //ASP.NET Core TraceIdentifier 
        });

        services.AddCors(opt =>
        {
            opt.AddPolicy(name: "AllowSpecific", options =>
            {
                options.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        //Auth
        string configSectionName = "AzureAd";
        var configSection = config.GetSection(configSectionName);
        if (configSection.Exists())
        {
            //https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-verification-scope-app-roles?tabs=aspnetcore
            //https://andrewlock.net/setting-global-authorization-policies-using-the-defaultpolicy-and-the-fallbackpolicy-in-aspnet-core-3/
            //https://learn.microsoft.com/en-us/entra/external-id/customers/tutorial-protect-web-api-dotnet-core-build-app

            logger.LogInformation("Configure auth - {ConfigSectionName}", configSectionName);

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddMicrosoftIdentityWebApi(config);

            //https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-api-call-api-app-configuration?tabs=aspnetcore
            //.EnableTokenAcquisitionToCallDownstreamApi() 
            //.AddInMemoryTokenCaches()

            services.AddSingleton<IAuthorizationHandler, RolesOrScopesAuthorizationHandler>();

            services.AddAuthorization();

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
                //define specific roles/scopes policies
                .AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"))
                .AddPolicy("SomeRolePolicy1", policy => policy.RequireRole("SomeAccess1"))
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

        //if needed
        //services.AddScoped<IValidatorDiscovery, ValidatorDiscovery>();
        services.AddScoped<IValidator<TodoItemDto>, TodoItemDtoValidator>();

        services.AddControllers();

        //convenient for model validation; built in IHostEnvironmentExtensions.BuildProblemDetailsResponse
        services.AddProblemDetails();

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

            //services.AddEndpointsApiExplorer(); 

            apiVersioningBulder.AddApiExplorer(o =>
            {
                // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                // note: the specified format code will format the version as "'v'major[.minor][-status]"
                o.GroupNameFormat = "'v'VVV";

                // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                o.SubstituteApiVersionInUrl = true;
            });
            // this enables binding ApiVersion as a endpoint callback parameter. if you don't use it, then remove this configuration.
            //.EnableApiVersionBinding();
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

        //Todo - for http clients previously registered in infrastructure services, add header propagation here since it only applies at runtime when an http context is present

        return services;
    }
}
