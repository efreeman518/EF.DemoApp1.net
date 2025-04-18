﻿using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Package.Infrastructure.Auth.Handlers;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;

namespace SampleApp.Gateway;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration config, ILogger loggerStartup)
    {
        //Application Insights telemetry for http services (for logging telemetry directly to AI)
        services.AddOpenTelemetry().UseAzureMonitor(options =>
        {
            options.ConnectionString = config.GetValue<string>("ApplicationInsights:ConnectionString");
        });

        //api versioning
        //var apiVersioningBuilder = services.AddApiVersioning(options =>
        //{
        //    options.DefaultApiVersion = new ApiVersion(1, 1);
        //    options.AssumeDefaultVersionWhenUnspecified = true;
        //    options.ReportApiVersions = true; //response header with supported versions
        //    options.ApiVersionReader = new UrlSegmentApiVersionReader(); // /v1.1/context/method, can combine multiple versioning approaches
        //});

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://localhost:7124") // Adjust to your Blazor app's address
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); //does not work with AllowAnyOrigin()
                                         //.SetIsOriginAllowed(origin => true) // replaces .AllowAnyOrigin() and allows any origin with AllowAnyOrigin()
            });
        });

        //Auth
        string configSectionName = "Gateway-AzureAdB2C"; //AzureAdB2C / EntraID
        var configSection = config.GetSection(configSectionName);
        if (configSection.GetChildren().Any())
        {
            //https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-verification-scope-app-roles?tabs=aspnetcore
            //https://andrewlock.net/setting-global-authorization-policies-using-the-defaultpolicy-and-the-fallbackpolicy-in-aspnet-core-3/
            //https://learn.microsoft.com/en-us/entra/external-id/customers/tutorial-protect-web-api-dotnet-core-build-app

            loggerStartup.LogInformation("Configure auth - {ConfigSectionName}", configSectionName);

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddMicrosoftIdentityWebApi(config.GetSection(configSectionName));

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
        //services.AddExceptionHandler<DefaultExceptionHandler>();

        //services.AddRouting(options => options.LowercaseUrls = true);

        //DefaultAzureCredential checks env vars first, then checks other - managed identity, etc
        //so if we need to use client/secret (client AAD App Reg), set the env vars
        if (config.GetValue<string>("SampleApiRestClientSettings:ClientId") != null)
        {
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", config.GetValue<string>("SampleApiRestClientSettings:TenantId"));
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", config.GetValue<string>("SampleApiRestClientSettings:ClientId"));
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", config.GetValue<string>("SampleApiRestClientSettings:ClientSecret"));
        }

        services.AddSingleton<TokenService>(); // Add TokenService
        services.AddReverseProxy()
            .LoadFromConfig(config.GetSection("ReverseProxy"))
            .AddTransforms(context =>
            {
                var tokenService = context.Services.GetRequiredService<TokenService>();
                var clusterId = context.Cluster?.ClusterId;
                if (string.IsNullOrEmpty(clusterId)) return;


                //if (!string.IsNullOrEmpty(token))
                //{
                context.AddRequestTransform(async context =>
                {
                    var token = await tokenService.GetAccessTokenAsync(clusterId);

                    // Remove the existing Authorization header (if any)
                    context.ProxyRequest.Headers.Authorization = null;
                    context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    //return ValueTask.CompletedTask;
                });
                //}
            });


        //if (config.GetValue("OpenApiSettings:Enable", false))
        //{
        //    //.net9
        //    //https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-9.0&tabs=visual-studio%2Cminimal-apis
        //    services
        //        .AddOpenApi("v1")
        //        .AddOpenApi("v1.1");

        //    apiVersioningBuilder.AddApiExplorer(o =>
        //    {
        //        // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
        //        // note: the specified format code will format the version as "'v'major[.minor][-status]"
        //        o.GroupNameFormat = "'v'VVV";

        //        // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
        //        // can also be used to control the format of the API version in route templates
        //        o.SubstituteApiVersionInUrl = true;
        //    });
        //}

        //HealthChecks - having infrastructure references
        //search nuget aspnetcore.healthchecks - many prebuilt health checks 
        //tag full will run when hitting health/full

        services.AddHealthChecks();
        //    .AddMemoryHealthCheck("memory", tags: ["full", "memory"], thresholdInBytes: config.GetValue<long>("MemoryHealthCheckBytesThreshold", 1024L * 1024L * 1024L));
        //.AddCheck<WeatherServiceHealthCheck>("External Service", tags: healthCheckTagsFullExt);

        //register http clients to backend services


        return services;
    }
}
