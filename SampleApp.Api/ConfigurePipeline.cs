using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SampleApp.Api.Middleware;

namespace SampleApp.Api;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        var config = app.Configuration;
        var env = app.Environment;

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        if (config.GetValue("SwaggerEnable", false))
        {
            //enable swagger
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //serve html UI
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseHeaderPropagation();

        //global error handler - http requests
        app.UseMiddleware(typeof(GlobalExceptionHandler));

#pragma warning disable ASP0014 //minimal api endpoints
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            //health checks
            endpoints.MapHealthChecks("/health", new HealthCheckOptions()
            {
                // Exclude all checks and return a 200 - Ok.
                Predicate = (_) => false
            });
            endpoints.MapHealthChecks("/health/full", BuildHealthCheckOptions("full"));
            endpoints.MapHealthChecks("/health/db", BuildHealthCheckOptions("db"));
            endpoints.MapHealthChecks("/health/memory", BuildHealthCheckOptions("memory"));
            endpoints.MapHealthChecks("/health/extservice", BuildHealthCheckOptions("extservice"));
        });
#pragma warning restore ASP0014

        return app;
    }

    private static HealthCheckOptions BuildHealthCheckOptions(string tag)
    {
        return new HealthCheckOptions()
        {
            Predicate = (check) => check.Tags.Contains(tag),
            ResponseWriter = HealthCheckHelper.WriteHealthReportResponse
        };
    }
}