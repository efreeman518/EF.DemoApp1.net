using CorrelationId;
using LazyCache;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Package.Infrastructure.AspNetCore;
using Package.Infrastructure.Auth.Tokens;
using SampleApp.Api.Endpoints;
using SampleApp.Grpc;
using Scalar.AspNetCore;

namespace SampleApp.Api;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        var config = app.Configuration;

        if (config.GetValue<string>("AzureAppConfig:Endpoint") != null)
        {
            //middleware monitors the Azure AppConfig sentinel - a change triggers configuration refresh.
            //middleware triggers on http request, not background service scope
            app.UseAzureAppConfiguration();
        }

        if (config.GetValue("ChatGPT_Plugin:Enable", false))
        {
            app.UseCors("ChatGPT");
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), ".well-known")),
                RequestPath = "/.well-known"
            });
        }

        //ChatGPT https not supported
        app.UseHttpsRedirection();

        //serve sample html/js UI
        app.UseDefaultFiles(); //default serve files from wwwroot
        app.UseStaticFiles(); //Serve files from wwwroot

        //global error handler
        app.UseExceptionHandler();

        app.UseCors("AllowSpecific");

        //swagger before auth so it will render without auth
        //https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/openapi?view=aspnetcore-8.0
        if (config.GetValue("OpenApiSettings:Enable", false))
        {
            //for swagger - map gettoken endpoint 
            var resourceId = config.GetValue<string>("SampleApiRestClientSettings:ResourceId");
            app.MapGet("/getauthtoken", async (HttpContext context, string resourceId, string scope) =>
            {
                var tokenProvider = new AzureDefaultCredTokenProvider(new CachingService());
                return await tokenProvider.GetAccessTokenAsync(resourceId, scope);
            }).AllowAnonymous().WithName("GetAuthToken").WithOpenApi(generatedOperation =>
            {
                var parameter = generatedOperation.Parameters[0];
                parameter.Description = $"External service resourceId {resourceId}";
                parameter = generatedOperation.Parameters[1];
                parameter.Description = $"External service scope .default";
                return generatedOperation;
            }).WithTags("_Top").WithDescription("Retrieve a token for the resource using the DefaultAzureCredetnial (Managed identity, env vars, VS logged in user, etc.");

            //.net9 //openapi/v1.json
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("SampleApp API")
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.AsyncHttp);
            });
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCorrelationId(); //requres http client service configuration - services.AddHttpClient().AddCorrelationIdForwarding();
        app.UseHeaderPropagation();

        //any other middleware
        //app.UseSomeMiddleware();

        //endpoints
        app.MapHealthChecks();
        app.MapControllers(); //.RequireAuthorization();
        app.MapGrpcService<TodoGrpcService>();

        //api versioning
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 1))
            .ReportApiVersions()
            .Build();

        var prodDetailsIncludeStack = !app.Environment.IsProduction();

        //endpoints - todoitems
        var group = app.MapGroup("api1/v{apiVersion:apiVersion}/todoitems")
            .WithApiVersionSet(apiVersionSet);
        //.RequireAuthorization("policy1", "policy2");
        group.MapTodoItemEndpoints(prodDetailsIncludeStack);

        //endpoints - skchat
        group = app.MapGroup("api1/v{apiVersion:apiVersion}/skchat")
            .WithApiVersionSet(apiVersionSet);
        //.RequireAuthorization("policy1", "policy2");
        group.MapChatSKEndpoints(prodDetailsIncludeStack);

        //endpoints - chat
        group = app.MapGroup("api1/v{apiVersion:apiVersion}/chat")
            .WithApiVersionSet(apiVersionSet);
        //.RequireAuthorization("policy1", "policy2");
        group.MapChatEndpoints(prodDetailsIncludeStack);

        //endpoints - assistant
        group = app.MapGroup("api1/v{apiVersion:apiVersion}/assistant")
            .WithApiVersionSet(apiVersionSet);
        //.RequireAuthorization("policy1", "policy2");
        group.MapAssistantEndpoints(prodDetailsIncludeStack);

        //endpoints - event grid
        group = app.MapGroup("api1/v{apiVersion:apiVersion}/eventgrid")
            .WithApiVersionSet(apiVersionSet);
        //.RequireAuthorization("policy1", "policy2");
        group.MapEventGridEndpoints();

        //endpoints - external
        group = app.MapGroup("api1/v{apiVersion:apiVersion}/external")
            .WithApiVersionSet(apiVersionSet);
        //.RequireAuthorization("policy1", "policy2");
        group.MapExternalEndpoints(prodDetailsIncludeStack);

        //endpoints - webhook
        group = app.MapGroup("api1/v{apiVersion:apiVersion}/blandai")
            .WithApiVersionSet(apiVersionSet);
        //.RequireAuthorization("policy1", "policy2");
        group.MapBlandAIEndpoints();

        return app;
    }

    private static WebApplication MapHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions()
        {
            // Exclude all checks and return a 200 - Ok.
            Predicate = (_) => false,
        });
        app.MapHealthChecks("/health/full", HealthCheckHelper.BuildHealthCheckOptions("full"));
        app.MapHealthChecks("/health/db", HealthCheckHelper.BuildHealthCheckOptions("db"));
        app.MapHealthChecks("/health/memory", HealthCheckHelper.BuildHealthCheckOptions("memory"));
        app.MapHealthChecks("/health/weatherservice", HealthCheckHelper.BuildHealthCheckOptions("weatherservice"));

        return app;
    }
}