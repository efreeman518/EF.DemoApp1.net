using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Package.Infrastructure.AspNetCore.Filters;
using Package.Infrastructure.Auth.Handlers;
using Package.Infrastructure.Common.Extensions;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace SampleApp.Gateway;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration config, ILogger logger)
    {
        AddAzureAppConfiguration(services, config);
        AddCors(services, config, logger);
        AddAuthentication(services, config, logger);
        AddReverseProxy(services, config);
        AddCorrelationTracking(services);
        AddHealthChecks(services);

        return services;
    }

    private static void AddAzureAppConfiguration(IServiceCollection services, IConfiguration config)
    {
        // Enable config reloading at runtime using Sentinel along with app.UseAzureAppConfiguration();
        if (config.GetValue<string>("AzureAppConfig:Endpoint") != null)
        {
            services.AddAzureAppConfiguration();
        }
    }
    private static void AddCors(IServiceCollection services, IConfiguration config, ILogger logger)
    {
        string corsConfigSectionName = "GatewayCors";
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
                        .AllowCredentials(); // Does not work with AllowAnyOrigin()
                });
            });
        }
    }
    private static void AddAuthentication(IServiceCollection services, IConfiguration config, ILogger logger)
    {
        string authConfigSectionName = "Gateway_AzureAdB2C"; // AzureAdB2C / EntraID
        var configSection = config.GetSection(authConfigSectionName);

        if (!configSection.GetChildren().Any())
        {
            logger.LogInformation("No Auth Config ({ConfigSectionName}) Found; Auth will not be configured.", authConfigSectionName);
            services.AddAuthentication();
            return;
        }

        logger.LogInformation("Configure auth - {ConfigSectionName}", authConfigSectionName);

        //debug
        logger.LogInformation("Auth Config: {ConfigSection}", configSection.SerializeToJson());

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddMicrosoftIdentityWebApi(config.GetSection(authConfigSectionName));

        services.AddSingleton<IAuthorizationHandler, RolesOrScopesAuthorizationHandler>();

        ConfigureAuthorizationPolicies(services, configSection);
    }

    private static void ConfigureAuthorizationPolicies(IServiceCollection services, IConfigurationSection configSection)
    {
        services.AddAuthorizationBuilder()
            // Fallback Policy for all endpoints that do not have any authorization defined, except explicit [AllowAnonymous] endpoints
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            // Default Policy for all endpoints that require authorization ([Authorize] authenticated user) already but no other specifics
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            // Define specific roles/scopes policies
            .AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"))
            .AddPolicy("SomeRolePolicy1", policy => policy.RequireRole("SomeAccess1"))
            .AddPolicy("SomeScopePolicy1", policy => policy.RequireScope("SomeScope1"))
            .AddPolicy("ScopeOrRolePolicy1", policy => policy.AddRequirements(new RolesOrScopesRequirement(["SomeAccess1"], ["SomeScope1"])))
            .AddPolicy("ScopeOrRolePolicy2", policy =>
            {
                var defaultRoles = configSection.GetSection("AppPermissions:Default").Get<string[]>();
                var defaultScopes = configSection.GetSection("Scopes:Default").Get<string[]>();
                policy.AddRequirements(new RolesOrScopesRequirement(defaultRoles, defaultScopes));
            });
    }

    private static void AddReverseProxy(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<TokenService>(); // Add TokenService

        services.AddReverseProxy()
            .LoadFromConfig(config.GetSection("ReverseProxy"))
            .AddTransforms(ConfigureProxyTransforms)
            .AddServiceDiscoveryDestinationResolver(); //support aspire service discovery for cluster destination address resolution
    }

    private static void ConfigureProxyTransforms(TransformBuilderContext context)
    {
        var tokenService = context.Services.GetRequiredService<TokenService>();
        var clusterId = context.Cluster?.ClusterId;
        if (string.IsNullOrEmpty(clusterId)) return;

        context.AddRequestTransform(async context =>
        {
            // Add token auth header
            var token = await tokenService.GetAccessTokenAsync(clusterId);
            context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Add correlation id header if present in request
            var httpContext = context.HttpContext;
            if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
            {
                context.ProxyRequest.Headers.TryAddWithoutValidation("X-Correlation-ID", (string?)correlationId);
            }
        });
    }

    private static void AddCorrelationTracking(IServiceCollection services)
    {
        // Propagate headers to downstream services (e.g. correlation id)
        services.AddHeaderPropagation(options =>
        {
            options.Headers.Add("X-Correlation-ID");
        });

        // Generate correlation ID if not present
        services.AddHttpContextAccessor();
        services.AddTransient<IStartupFilter, CorrelationIdStartupFilter>();
    }

    private static void AddHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks();
        // Additional health checks can be added here:
        // .AddMemoryHealthCheck("memory", tags: ["full", "memory"], thresholdInBytes: config.GetValue<long>("MemoryHealthCheckBytesThreshold", 1024L * 1024L * 1024L));
        // .AddCheck<WeatherServiceHealthCheck>("External Service", tags: healthCheckTagsFullExt);
    }
}
