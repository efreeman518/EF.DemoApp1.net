using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace SampleApp.Gateway;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();

        //Cors
        string corsConfigSectionName = "Cors";
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
        app.MapHealthChecks();

        //helpful for debugging
        app.MapReverseProxy(static proxyPipeline =>
        {
            // Optionally log errors on the proxy pipeline
            proxyPipeline.Use(async (context, next) =>
            {
                try
                {
                    // Disable response buffering to allow ProblemDetails to flow through
                    //context.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
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
