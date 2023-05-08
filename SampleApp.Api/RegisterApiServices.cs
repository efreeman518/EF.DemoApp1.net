using CorrelationId.DependencyInjection;
using Infrastructure.Data;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.AspNetCore.Swagger;
using SampleApp.Bootstrapper.HealthChecks;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace SampleApp.Api;

internal static class IServiceCollectionExtensions
{
    /// <summary>
    /// Used at runtime for http services; not used for Workers/Functions/Tests
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterApiServices(this IServiceCollection services, IConfiguration config)
    {
        //Application Insights telemtry for http services (for logging telemetry directly to AI)
        services.AddApplicationInsightsTelemetry();
        //capture full sql
        services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
        {
            module.EnableSqlCommandTextInstrumentation = config.GetValue<bool>("EnableSqlCommandTextInstrumentation", false);
        });

        //api versioning
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
            options.AddToLoggingScope = true;
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

        //HealthChecks - having infrastructure references
        //tag full will run when hitting health/full
        services.AddHealthChecks()
            .AddMemoryHealthCheck("memory", tags: new[] { "full", "memory" }, thresholdInBytes: config.GetValue<long>("MemoryHealthCheckBytesThreshold", 1024L * 1024L * 1024L))
            .AddDbContextCheck<TodoDbContextTrxn>("TodoDbContextTrxn", tags: new[] { "full", "db" })
            .AddDbContextCheck<TodoDbContextQuery>("TodoDbContextQuery", tags: new[] { "full", "db" })
            .AddCheck<WeatherServiceHealthCheck>("External Service", tags: new[] { "full", "extservice" });

        return services;
    }
}
