using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace SampleApp.Gateway;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        ConfigureSecurity(app);
        ConfigureAzureAppConfiguration(app);
        ConfigureCors(app);
        ConfigureMiddleware(app);
        ConfigureEndpoints(app);
        ConfigureReverseProxy(app);

        return app;
    }

    private static void ConfigureSecurity(WebApplication app)
    {
        // ACA (internal to the container env) doesn't like, but ok for other hosting (local, App Service, etc.)
        if (app.Configuration.GetValue("EnforceHttpsRedirection", false))
        {
            app.UseHttpsRedirection();
        }
    }

    private static void ConfigureAzureAppConfiguration(WebApplication app)
    {
        // Use Azure App Configuration middleware for dynamic configuration refresh
        if (app.Configuration.GetValue<string>("AzureAppConfig:Endpoint") != null)
        {
            // Middleware monitors the Azure AppConfig sentinel - a change triggers configuration refresh
            // Middleware triggers on http request, not background service scope
            app.UseAzureAppConfiguration();
        }
    }

    private static void ConfigureCors(WebApplication app)
    {
        string corsConfigSectionName = "GatewayCors";
        var corsConfigSection = app.Configuration.GetSection(corsConfigSectionName);

        if (corsConfigSection.GetChildren().Any())
        {
            var policyName = corsConfigSection.GetValue<string>("PolicyName")!;
            app.UseCors(policyName);
        }
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
    }

    private static void ConfigureEndpoints(WebApplication app)
    {
        app.MapGet("/", () => Results.Json(new { message = "API is running" }));
        app.MapHealthChecks();

        //aspire
        app.MapDefaultEndpoints();
    }

    private static void ConfigureReverseProxy(WebApplication app)
    {
        app.MapReverseProxy(proxyPipeline =>
        {
            // Add error handling middleware for the proxy pipeline
            proxyPipeline.Use(AddProxyErrorLogging);
        });
    }

    private static async Task AddProxyErrorLogging(HttpContext context, Func<Task> next)
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
    }

    private static WebApplication MapHealthChecks(this WebApplication app)
    {
        // For this YARP API (does not forward)
        app.MapHealthChecks("/health", new HealthCheckOptions()
        {
            // Exclude all checks and return a 200 - Ok.
            Predicate = (_) => false,
        });
        return app;
    }
}
