using Asp.Versioning;
using Asp.Versioning.Builder;
using LazyCache;
using Microsoft.Extensions.FileProviders;
using Package.Infrastructure.AspNetCore.HealthChecks;
using Package.Infrastructure.Auth.Tokens;
using SampleApp.Api.Endpoints;
using SampleApp.Api.Middleware;
using SampleApp.Grpc;
using Scalar.AspNetCore;

namespace SampleApp.Api;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        var config = app.Configuration;

        // Configure middleware and services
        SetupConfiguration(app, config);
        SetupStaticFiles(app, config);
        SetupSecurityMiddleware(app, config);
        SetupOpenApi(app, config);

        // Configure endpoints
        SetupBasicEndpoints(app);
        SetupApiVersionedEndpoints(app);

        SetupExceptionHandler(app);

        return app;
    }

    private static void SetupExceptionHandler(WebApplication app)
    {
        app.UseExceptionHandler();
    }

    private static void SetupConfiguration(WebApplication app, IConfiguration config)
    {
        if (config.GetValue<string>("AzureAppConfig:Endpoint") != null)
        {
            //middleware monitors the Azure AppConfig sentinel - a change triggers configuration refresh.
            //middleware triggers on http request, not background service scope
            app.UseAzureAppConfiguration();
        }
    }

    private static void SetupStaticFiles(WebApplication app, IConfiguration config)
    {
        // Configure ChatGPT plugin
        if (config.GetValue("ChatGPT_Plugin:Enable", false))
        {
            app.UseCors("ChatGPT");
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), ".well-known")),
                RequestPath = "/.well-known"
            });
        }

        // ACA (internal to the container env) doesn't like, but ok for other hosting (local, App Service, etc.)
        if (config.GetValue("EnforceHttpsRedirection", false))
        {
            app.UseHttpsRedirection();
        }

        //serve sample html/js UI
        //app.UseDefaultFiles(); //default serve files from wwwroot
        //app.UseStaticFiles(); //Serve files from wwwroot
    }

    private static void SetupSecurityMiddleware(WebApplication app, IConfiguration config)
    {
        // Configure CORS
        string corsConfigSectionName = "Cors";
        var corsConfigSection = config.GetSection(corsConfigSectionName);
        if (corsConfigSection.GetChildren().Any())
        {
            var policyName = corsConfigSection.GetValue<string>("PolicyName")!;
            app.UseCors(policyName);
        }

        // Basic security middleware
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCustomHeaderAuth(); //extract roles from gateway request & assign to current request 
    }

    private static void SetupOpenApi(WebApplication app, IConfiguration config)
    {
        if (!config.GetValue("OpenApiSettings:Enable", false))
        {
            return;
        }

        // Auth token endpoint for OpenAPI UI
        var resourceId = config.GetValue<string>("SampleApiRestClientSettings:ResourceId");
        app.MapGet("/getauthtoken", async (HttpContext context, string resourceId, string scope) =>
        {
            var tokenProvider = new AzureDefaultCredTokenProvider(new CachingService());
            return await tokenProvider.GetAccessTokenAsync(resourceId, scope);
        })
        .AllowAnonymous()
        .WithName("GetAuthToken")
        .WithOpenApi(generatedOperation =>
        {
            var parameter = generatedOperation.Parameters[0];
            parameter.Description = $"External service resourceId {resourceId}";
            parameter = generatedOperation.Parameters[1];
            parameter.Description = $"External service scope .default";
            return generatedOperation;
        })
        .WithTags("_Top")
        .WithDescription("Retrieve a token for the resource using DefaultAzureCredential");

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("SampleApp API")
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.AsyncHttp);
        });
    }

    private static void SetupBasicEndpoints(WebApplication app)
    {
        //aspire
        app.MapDefaultEndpoints();

        app.MapGet("/", () => Results.Json(new { message = "API is running" }));
        app.MapHealthChecks();

        //grpc endpoints
        app.MapGrpcService<TodoGrpcService>();
    }

    private static void SetupApiVersionedEndpoints(WebApplication app)
    {
        var apiVersionSet = CreateApiVersionSet(app);
        var includeErrorDetails = !app.Environment.IsProduction();

        MapApiGroups(app, apiVersionSet, includeErrorDetails);
    }

    private static ApiVersionSet CreateApiVersionSet(WebApplication app)
    {
        return app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .HasApiVersion(new ApiVersion(1, 1))
            .ReportApiVersions()
            .Build();
    }

    private static void MapApiGroups(WebApplication app, ApiVersionSet apiVersionSet, bool includeErrorDetails)
    {
        // TodoItems endpoints
        app.MapGroup("api1/v{apiVersion:apiVersion}/todoitems")
            .WithApiVersionSet(apiVersionSet)//.RequireAuthorization("StandardAccessPolicy1")
            .MapTodoItemEndpoints(includeErrorDetails);

        // SK Chat endpoints
        app.MapGroup("api1/v{apiVersion:apiVersion}/skchat")
            .WithApiVersionSet(apiVersionSet)
            .MapChatSKEndpoints(includeErrorDetails);

        // Chat endpoints
        app.MapGroup("api1/v{apiVersion:apiVersion}/chat")
            .WithApiVersionSet(apiVersionSet)
            .MapChatEndpoints(includeErrorDetails);

        // Assistant endpoints
        app.MapGroup("api1/v{apiVersion:apiVersion}/assistant")
            .WithApiVersionSet(apiVersionSet)
            .MapAssistantEndpoints(includeErrorDetails);

        // Event Grid endpoints
        app.MapGroup("api1/v{apiVersion:apiVersion}/eventgrid")
            .WithApiVersionSet(apiVersionSet)
            .MapEventGridEndpoints();

        // External endpoints
        app.MapGroup("api1/v{apiVersion:apiVersion}/external")
            .WithApiVersionSet(apiVersionSet)
            .MapExternalEndpoints(includeErrorDetails);

        // BlandAI endpoints
        app.MapGroup("api1/v{apiVersion:apiVersion}/blandai")
            .WithApiVersionSet(apiVersionSet)
            .MapBlandAIEndpoints();
    }

    private static WebApplication MapHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/full", HealthCheckHelper.BuildHealthCheckOptions("full"));
        app.MapHealthChecks("/health/db", HealthCheckHelper.BuildHealthCheckOptions("db"));
        app.MapHealthChecks("/health/memory", HealthCheckHelper.BuildHealthCheckOptions("memory"));
        app.MapHealthChecks("/health/weatherservice", HealthCheckHelper.BuildHealthCheckOptions("weatherservice"));

        return app;
    }
}