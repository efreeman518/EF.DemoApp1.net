using CorrelationId;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Package.Infrastructure.AspNetCore;
using SampleApp.Api.Grpc;
using SampleApp.Api.Middleware;

namespace SampleApp.Api;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        var config = app.Configuration;

        //serve sample html/js UI
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCorrelationId(); //internal service configuration - services.AddHttpClient().AddCorrelationIdForwarding();
        app.UseHeaderPropagation();

        //exception handler when no global
        //https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-7.0
        //global error handler
        app.UseMiddleware(typeof(GlobalExceptionHandler));

        app.MapControllers();

        app.MapGrpcService<TodoGrpcService>();

        app.MapHealthChecks("/health", new HealthCheckOptions()
        {
            // Exclude all checks and return a 200 - Ok.
            Predicate = (_) => false,
        });
        app.MapHealthChecks("/health/full", HealthCheckHelper.BuildHealthCheckOptions("full"));
        app.MapHealthChecks("/health/db", HealthCheckHelper.BuildHealthCheckOptions("db"));
        app.MapHealthChecks("/health/memory", HealthCheckHelper.BuildHealthCheckOptions("memory"));
        app.MapHealthChecks("/health/weatherservice", HealthCheckHelper.BuildHealthCheckOptions("weatherservice"));

        if (config.GetValue("SwaggerSettings:Enable", false))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }

}