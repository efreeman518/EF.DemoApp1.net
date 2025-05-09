﻿using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Package.Infrastructure.AspNetCore.Filters;
using Package.Infrastructure.Auth.Handlers;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace SampleApp.Gateway;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration config, ILogger loggerStartup)
    {
        ConfigureAzureAppConfiguration(services, config);
        ConfigureTelemetry(services, config);
        ConfigureCors(services, config, loggerStartup);
        ConfigureAuthentication(services, config, loggerStartup);
        ConfigureReverseProxy(services, config);
        ConfigureCorrelationTracking(services);
        ConfigureHealthChecks(services);

        return services;
    }

    private static void ConfigureAzureAppConfiguration(IServiceCollection services, IConfiguration config)
    {
        // Enable config reloading at runtime using Sentinel along with app.UseAzureAppConfiguration();
        if (config.GetValue<string>("AzureAppConfig:Endpoint") != null)
        {
            services.AddAzureAppConfiguration();
        }
    }

    private static void ConfigureTelemetry(IServiceCollection services, IConfiguration config)
    {
        // Application Insights telemetry for http services (for logging telemetry directly to AI)
        var appInsightsConnectionString = config["ApplicationInsights:ConnectionString"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("SampleApi"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Microsoft.EntityFrameworkCore") // Capture the SQL
                    .AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = appInsightsConnectionString;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddAzureMonitorMetricExporter(options =>
                    {
                        options.ConnectionString = appInsightsConnectionString;
                    });
            });
    }

    private static void ConfigureCors(IServiceCollection services, IConfiguration config, ILogger loggerStartup)
    {
        string corsConfigSectionName = "GatewayCors";
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
                        .AllowCredentials(); // Does not work with AllowAnyOrigin()
                });
            });
        }
    }

    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration config, ILogger loggerStartup)
    {
        string authConfigSectionName = "Gateway_AzureAdB2C"; // AzureAdB2C / EntraID
        var configSection = config.GetSection(authConfigSectionName);

        if (!configSection.GetChildren().Any())
        {
            return;
        }

        loggerStartup.LogInformation("Configure auth - {ConfigSectionName}", authConfigSectionName);

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

    private static void ConfigureReverseProxy(IServiceCollection services, IConfiguration config)
    {
        SetupAzureCredentials(config);

        services.AddSingleton<TokenService>(); // Add TokenService

        services.AddReverseProxy()
            .LoadFromConfig(config.GetSection("ReverseProxy"))
            .AddTransforms(ConfigureProxyTransforms);
    }

    private static void SetupAzureCredentials(IConfiguration config)
    {
        // DefaultAzureCredential checks env vars first, then checks other - managed identity, etc
        // So if we need to use client/secret (client AAD App Reg), set the env vars
        if (config.GetValue<string>("SampleApiRestClientSettings:ClientId") != null)
        {
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", config.GetValue<string>("SampleApiRestClientSettings:TenantId"));
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", config.GetValue<string>("SampleApiRestClientSettings:ClientId"));
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", config.GetValue<string>("SampleApiRestClientSettings:ClientSecret"));
        }
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

    private static void ConfigureCorrelationTracking(IServiceCollection services)
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

    private static void ConfigureHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks();
        // Additional health checks can be added here:
        // .AddMemoryHealthCheck("memory", tags: ["full", "memory"], thresholdInBytes: config.GetValue<long>("MemoryHealthCheckBytesThreshold", 1024L * 1024L * 1024L));
        // .AddCheck<WeatherServiceHealthCheck>("External Service", tags: healthCheckTagsFullExt);
    }
}
