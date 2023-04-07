using Application.Services;
using CorrelationId.Abstractions;
using CorrelationId.DependencyInjection;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.AspNetCore.Swagger;
using Package.Infrastructure.Common;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Linq;
using System.Security.Claims;

namespace SampleApp.Api;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection RegisterApiServices(this IServiceCollection services, IConfiguration config)
    {
        //Application Insights (for logging telemetry directly to AI)
        services.AddApplicationInsightsTelemetry();
        //capture full sql
        services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
        {
            module.EnableSqlCommandTextInstrumentation = config.GetValue<bool>("Logging:EnableSqlCommandTextInstrumentation", false);
        });

        //IRequestContext 
        services.AddScoped<IRequestContext>(provider =>
        {
            var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            var correlationContext = provider.GetRequiredService<ICorrelationContextAccessor>().CorrelationContext;

            //Background services will not have an http context
            if (httpContext == null)
            {
                var correlationId = Guid.NewGuid().ToString();
                return new RequestContext(correlationId, $"BackgroundService-{correlationId}");
            }

            var user = httpContext?.User;

            //Get auditId from token claim /header
            string? auditId =
                //AAD from user
                user?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                //AAD ObjectId from user or client AAD enterprise app [ServicePrincipal Id / Object Id]:
                ?? user?.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                //AppId for the AAD Ent App/App Reg (client) whether its the client/secret or user with permissions on the Ent App
                ?? user?.Claims.FirstOrDefault(c => c.Type == "appid")?.Value
                //TODO: Remove this default or specify a system audit identity (for background services)
                ?? "NoAuthImplemented"
                ;

            return new RequestContext(correlationContext!.CorrelationId, auditId);
        });

        // api versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader(); // /v1.1/context/method
        });

        //header propagation
        services.AddHeaderPropagation(); // (options => options.Headers.Add("x-username-etc"));

        //https://github.com/stevejgordon/CorrelationId/wiki
        services.AddDefaultCorrelationId(options =>
        {
            //options.EnforceHeader = false;
            options.UpdateTraceIdentifier = true; //ASP.NET Core TraceIdentifier 
        });

        services.AddControllers();

        if (config.GetValue("SwaggerSettings:Enable", false))
        {
            //enable swagger
            //https://markgossa.com/2022/05/asp-net-6-api-versioning-swagger.html
            services.AddVersionedApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VV";
                o.SubstituteApiVersionInUrl = true;
            });
            services.Configure<SwaggerSettings>(config.GetSection(SwaggerSettings.ConfigSectionName));
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerGenConfigurationOptions>();
            services.AddTransient<IConfigureOptions<SwaggerUIOptions>, SwaggerUIConfigurationOptions>();
            var xmlCommentsFileName = config.GetValue<string>("SwaggerSettings:XmlCommentsFileName");
            if (xmlCommentsFileName != null) services.AddSwaggerGen(o => SwaggerGenConfigurationOptions.AddSwaggerXmlComments(o, xmlCommentsFileName));
        }

        return services;
    }
}
