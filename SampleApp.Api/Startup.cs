using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Package.Infrastructure.BackgroundService;
using Package.Infrastructure.Common;
using SampleApp.Api.Background;
using SampleApp.Api.Middleware;
using SampleApp.Api.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Security.Claims;

namespace SampleApp.Api;

public class Startup
{
    private readonly IConfiguration _config;

    public Startup(IConfiguration configuration)
    {
        _config = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        //Bootstrapper registers application, domain, and infrastructure services
        var bootstrapper = new Bootstrapper.Startup(services, _config);
        //infrastructure, application and domain services
        bootstrapper.ConfigureServices();
        //runtime http services - only for Services.Api - healthchecks, startup tasks
        bootstrapper.ConfigureRuntimeServices();

        //background services - residing in the api
        services.AddHostedService<CronService>();
        services.Configure<CronJobBackgroundServiceSettings<CustomCronJob>>(_config.GetSection(CronServiceSettings.ConfigSectionName));

        //Application Insights (for logging telemetry directly to AI)
        services.AddApplicationInsightsTelemetry();
        //capture full sql
        services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
        {
            module.EnableSqlCommandTextInstrumentation = _config.GetValue<bool>("Logging:EnableSqlCommandTextInstrumentation", false);
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

        services.AddControllers();

        if (_config.GetValue("SwaggerEnable", false))
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
        }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        if (_config.GetValue("SwaggerEnable", false))
        { 
            //enable swagger
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                provider.ApiVersionDescriptions.ToList().ForEach(description =>
                {
                    options.SwaggerEndpoint($"{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                });
            });
        }

        //serve html UI
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        //global error handler - http requests
        app.UseMiddleware(typeof(GlobalExceptionHandler));

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

