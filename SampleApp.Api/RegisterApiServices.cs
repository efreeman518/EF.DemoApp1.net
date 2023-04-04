using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundService;
using Package.Infrastructure.Common;
using SampleApp.Api.Background;
using SampleApp.Api.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Linq;
using System.Security.Claims;

namespace SampleApp.Api;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder RegisterApiServices(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var services = builder.Services;

        //background services - defined in the api project
        services.AddHostedService<CronService>();
        services.Configure<CronJobBackgroundServiceSettings<CustomCronJob>>(config.GetSection(CronServiceSettings.ConfigSectionName));

        //Application Insights (for logging telemetry directly to AI)
        services.AddApplicationInsightsTelemetry();
        //capture full sql
        services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
        {
            module.EnableSqlCommandTextInstrumentation = config.GetValue<bool>("Logging:EnableSqlCommandTextInstrumentation", false);
        });

        //IAuditDetail 
        services.AddTransient<ServiceRequestContext>(provider =>
        {
            var httpContext = provider.GetService<IHttpContextAccessor>()?.HttpContext;

            //TraceId from possible header used for correllation across multiple service calls
            var traceId = Guid.NewGuid().ToString();

            var user = httpContext?.User;
            //Get auditId from token claim
            //AAD bearer token
            string? auditId =
                //AAD from user
                user?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                //AAD ObjectId from user or client AAD enterprise app [ServicePrincipal Id / Object Id]:
                ?? user?.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                //AppId for the AAD Ent App/App Reg (client) whether its the client/secret or user with permissions on the Ent App
                ?? user?.Claims.FirstOrDefault(c => c.Type == "appid")?.Value
                //TODO: Remove this default
                ?? "NoAuthImplemented"
                ;

            return new ServiceRequestContext(auditId, traceId);
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
        services.AddHeaderPropagation(); // (options => options.Headers.Add("x-correlation-id"));

        services.AddControllers();

        if (config.GetValue("SwaggerEnable", false))
        {
            //enable swagger
            //https://markgossa.com/2022/05/asp-net-6-api-versioning-swagger.html
            services.AddVersionedApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VV";
                o.SubstituteApiVersionInUrl = true;
            });
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerGenConfigurationOptions>();
            services.AddSwaggerGen(o => SwaggerGenConfigurationOptions.AddSwaggerXmlComments(o));
            services.AddTransient<IConfigureOptions<SwaggerUIOptions>, SwaggerUIConfigurationOptions>();
        }

        return builder;
    }
}
