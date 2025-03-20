using LazyCache;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Package.Infrastructure.AspNetCore.HealthChecks;
using Package.Infrastructure.Auth.Tokens;
using Scalar.AspNetCore;

namespace SampleApp.Gateway;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        var config = app.Configuration;

        app.UseHttpsRedirection();
        //app.UseExceptionHandler(); ?? not needed, handled by services.AddExceptionHandler<DefaultExceptionHandler>();
        app.UseCors();

        //before auth so it will render without auth
        //https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/openapi?view=aspnetcore-8.0
        if (config.GetValue("OpenApiSettings:Enable", false))
        {
            //for openapi UI - map gettoken endpoint 
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
                    .WithTitle("SampleApp.Gateway API")
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.AsyncHttp);
            });
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        //endpoints
        app.MapHealthChecks();

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
        app.MapHealthChecks("/health/memory", HealthCheckHelper.BuildHealthCheckOptions("memory"));
        app.MapHealthChecks("/health/someservice", HealthCheckHelper.BuildHealthCheckOptions("someservice"));

        return app;
    }
}
