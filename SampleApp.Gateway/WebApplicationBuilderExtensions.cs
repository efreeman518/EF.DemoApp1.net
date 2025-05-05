using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace SampleApp.Gateway;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();

        // Use Azure App Configuration middleware for dynamic configuration refresh.
        if (app.Configuration.GetValue<string>("AzureAppConfig:Endpoint") != null)
        {
            //middleware monitors the Azure AppConfig sentinel - a change triggers configuration refresh.
            //middleware triggers on http request, not background service scope
            app.UseAzureAppConfiguration();
        }

        //Cors
        string corsConfigSectionName = "GatewayCors";
        var corsConfigSection = app.Configuration.GetSection(corsConfigSectionName);
        if (corsConfigSection.GetChildren().Any())
        {
            var policyName = corsConfigSection.GetValue<string>("PolicyName")!;
            app.UseCors(policyName);
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        //endpoints
        app.MapGet("/", () => Results.Json(new { message = "API is running" }));
        app.MapHealthChecks();

        //helpful for debugging
        app.MapReverseProxy(static proxyPipeline =>
        {
            // Optionally log errors on the proxy pipeline
            proxyPipeline.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred during proxy request.");
                    throw;
                }
            });
        });

        return app;
    }

    private static WebApplication MapHealthChecks(this WebApplication app)
    {
        //for this yarp api (does not forward)
        app.MapHealthChecks("/health", new HealthCheckOptions()
        {
            // Exclude all checks and return a 200 - Ok.
            Predicate = (_) => false,
        });
        return app;
    }
}
