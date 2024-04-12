using CorrelationId;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Package.Infrastructure.AspNetCore;
using SampleApp.Api.Grpc;
using SampleApp.Api.Middleware;

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

        //serve sample html/js UI
        app.UseDefaultFiles();
        app.UseStaticFiles(); //Serve files from wwwroot

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

        //global error handler
        app.UseExceptionHandler();

        app.UseRouting();
        app.UseCors("AllowSpecific");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCorrelationId(); //requres http client service configuration - services.AddHttpClient().AddCorrelationIdForwarding();
        app.UseHeaderPropagation();

        //any other middleware
        app.UseSomeMiddleware();

        app.MapControllers(); //.RequireAuthorization();
        app.MapGrpcService<TodoGrpcService>();
        app.MapHealthChecks("/_health", new HealthCheckOptions()
        {
            // Exclude all checks and return a 200 - Ok.
            Predicate = (_) => false,
        });
        app.MapHealthChecks("/_health/full", HealthCheckHelper.BuildHealthCheckOptions("full"));
        app.MapHealthChecks("/_health/db", HealthCheckHelper.BuildHealthCheckOptions("db"));
        app.MapHealthChecks("/_health/memory", HealthCheckHelper.BuildHealthCheckOptions("memory"));
        app.MapHealthChecks("/_health/weatherservice", HealthCheckHelper.BuildHealthCheckOptions("weatherservice"));

        if (config.GetValue("SwaggerSettings:Enable", false))
        {
            app.UseSwagger(o =>
            {
                //Microsoft Power Apps and Microsoft Flow do not support OpenAPI 3.0
                //enable temporarily to produce a Swagger 2.0 file;
                //o.SerializeAsV2 = true;

                //ChatGPT plugin
                if (config.GetValue("ChatGPT_Plugin:Enable", false))
                {
                    o.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                    {
                        swaggerDoc.Servers = [new() { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }];
                    });
                }
            });
            app.UseSwaggerUI(o =>
            {
                // build a swagger endpoint for each discovered API version
                foreach (var description in app.DescribeApiVersions().Select(description => description.GroupName))
                {
                    var url = $"/swagger/{description}/swagger.json";
                    var name = description.ToUpperInvariant();
                    o.SwaggerEndpoint(url, name);
                }
            });
        }

        return app;
    }
}