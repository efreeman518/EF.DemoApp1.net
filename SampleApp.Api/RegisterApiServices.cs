using Asp.Versioning;
using CorrelationId.DependencyInjection;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Package.Infrastructure.AspNetCore.Swagger;
using Package.Infrastructure.Grpc;
using Sample.Api.ExceptionHandlers;
using SampleApp.Bootstrapper.HealthChecks;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SampleApp.Api;

internal static class IServiceCollectionExtensions
{
    internal static readonly string[] healthCheckTagsFullMem = ["full", "memory"];
    internal static readonly string[] healthCheckTagsFullDb = ["full", "db"];
    internal static readonly string[] healthCheckTagsFullExt = ["full", "extservice"];

    /// <summary>
    /// Used at runtime for http services; not used for Workers/Functions/Tests
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterApiServices(this IServiceCollection services, IConfiguration config, ILogger logger)
    {
        //Application Insights telemtry for http services (for logging telemetry directly to AI)
        //var aiOptions = new ApplicationInsightsServiceOptions
        //{
        //    ConnectionString = config.GetValue<string>("ApplicationInsights:ConnectionString")
        //};

        //services.AddApplicationInsightsTelemetry(aiOptions)
        //    .ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
        //    {
        //        module.EnableSqlCommandTextInstrumentation = config.GetValue("EnableSqlCommandTextInstrumentation", false);
        //    });
        services.AddApplicationInsightsTelemetry();

        ////TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
        ////configuration.ConnectionString = config.GetValue<string>("ApplicationInsights:ConnectionString");

        //// Add OpenTelemetry Tracing
        ////services.AddOpenTelemetryTracing((sp, builder) =>
        ////{
        ////    builder
        ////        .AddAspNetCoreInstrumentation()
        ////        .AddHttpClientInstrumentation()
        ////        .AddNpgsqlInstrumentation()
        ////        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Configuration["MicroserviceName"]))
        ////        .AddAzureMonitorTraceExporter(config =>
        ////        {
        ////            config.ConnectionString = Configuration["ApplicationInsights:ConnectionString"];
        ////        });
        ////});

        //global unhandled exception handler
        services.AddExceptionHandler<DefaultExceptionHandler>();

        //api versioning
        var apiVersioningBulder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader(); // /v1.1/context/method
        });

        //header propagation - implement here since integration testing breaks when there is no existing http request, so no headers to propagate
        services.AddHeaderPropagation();
        //.AddHeaderPropagation(options =>
        //{
        //    options.Headers.Add("x-request-id");
        //    options.Headers.Add("x-correlation-id");
        //    options.Headers.Add("x-username-etc");
        //}); 
        //.AddCorrelationIdForwarding();

        //https://github.com/stevejgordon/CorrelationId/wiki
        services.AddDefaultCorrelationId(options =>
        {
            options.AddToLoggingScope = true;
            options.UpdateTraceIdentifier = true; //ASP.NET Core TraceIdentifier 
        });

        services.AddCors(opt =>
        {
            opt.AddPolicy(name: "AllowSpecific", options =>
            {
                options.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        //app clients - Enable JWT Bearer Authentication
        var configSection = config.GetSection("AzureAd");
        if (configSection.Exists())
        {
            //https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-app-configuration?tabs=aspnetcore
            //https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-verification-scope-app-roles?tabs=aspnetcore
            //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //    .AddMicrosoftIdentityWebApi(configSection)
            //    //.AddJwtBearer(options =>
            //    //{
            //    //    var authority = $"{configSection.GetValue<string?>("Instance", null)}{configSection.GetValue<string?>("TenantId", null)}/";
            //    //    var clientId = configSection.GetValue<string?>("ClientId", null);
            //    //    options.Authority = $"{authority}/";
            //    //    options.Audience = clientId; 
            //    //    options.TokenValidationParameters = new TokenValidationParameters
            //    //    {
            //    //        ValidateIssuer = true,
            //    //        ValidateAudience = true,
            //    //        ValidateLifetime = true,
            //    //        ValidateIssuerSigningKey = true,
            //    //        ValidIssuer = $"{authority}/v2.0",
            //    //        ValidAudience = clientId
            //    //    };
            //    //})
            //    ;

            //services.AddMicrosoftIdentityWebApiAuthentication(config, "AzureAd");

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddMicrosoftIdentityWebApi(config);

            //.AddMicrosoftIdentityWebApi(configSection, JwtBearerDefaults.AuthenticationScheme)
            //.EnableTokenAcquisitionToCallDownstreamApi()
            //.AddInMemoryTokenCaches()

            //services.AddMicrosoftIdentityWebApiAuthentication(config, "AzureAd");

            services.AddAuthorizationBuilder()
                //require authenticated user globally
                //.SetFallbackPolicy(new AuthorizationPolicyBuilder()
                //    .RequireAuthenticatedUser()
                //    .Build())
                //.SetDefaultPolicy(new AuthorizationPolicyBuilder()
                //    .RequireAuthenticatedUser()
                //    .Build())
                //require specific roles on spcific endpoints
                .AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"))
                .AddPolicy("SomeAccess1Policy", policy => policy.RequireRole("SomeAccess1"))
             ;

            //services.AddAuthorization(options =>
            //{
            //    //require auth globally
            //    options.FallbackPolicy = new AuthorizationPolicyBuilder()
            //        .RequireAuthenticatedUser()
            //        .Build();
            //    //require auth for specific roles
            //    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            //    options.AddPolicy("SomeAccess1", policy => policy.RequireRole("SomeAccess1"));
            //});
        }

        services.AddControllers();

        //convenient for model validation
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Extensions.Add("machineName", Environment.MachineName);
            }
        );

        //Add gRPC framework services
        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaxReceiveMessageSize = 100000; //bytes
            options.Interceptors.Add<ServiceErrorInterceptor>();
        });
        services.AddScoped<ServiceErrorInterceptor>();

        services.AddRouting(options => options.LowercaseUrls = true);

        if (config.GetValue("SwaggerSettings:Enable", false))
        {
            services.AddEndpointsApiExplorer();

            apiVersioningBulder.AddApiExplorer(o =>
            {
                // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                // note: the specified format code will format the version as "'v'major[.minor][-status]"
                o.GroupNameFormat = "'v'VVV";

                // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                o.SubstituteApiVersionInUrl = true;
            });
            // this enables binding ApiVersion as a endpoint callback parameter. if you don't use it, then remove this configuration.
            //.EnableApiVersionBinding();

            services.Configure<SwaggerSettings>(config.GetSection(SwaggerSettings.ConfigSectionName));
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerGenConfigurationOptions>();
            //services.AddTransient<IConfigureOptions<SwaggerUIOptions>, SwaggerUIConfigurationOptions>();
            var xmlCommentsFileName = config.GetValue<string>("SwaggerSettings:XmlCommentsFileName");
            if (xmlCommentsFileName != null) services.AddSwaggerGen(o => SwaggerGenConfigurationOptions.AddSwaggerXmlComments(o, xmlCommentsFileName));
        }

        //ChatGPT plugin
        if (config.GetValue("ChatGPT_Plugin:Enable", false))
        {
            services.AddCors(options =>
            {
                options.AddPolicy("ChatGPT", policy =>
                {
                    policy.WithOrigins("https://chat.openai.com", config.GetValue<string>("ChatGPT_Plugin:Url")!).AllowAnyHeader().AllowAnyMethod();
                });
            });
        }

        //HealthChecks - having infrastructure references
        //search nuget aspnetcore.healthchecks - many prebuilt health checks 
        //tag full will run when hitting health/full

        services.AddHealthChecks()
            .AddMemoryHealthCheck("memory", tags: healthCheckTagsFullMem, thresholdInBytes: config.GetValue<long>("MemoryHealthCheckBytesThreshold", 1024L * 1024L * 1024L))
            .AddDbContextCheck<TodoDbContextTrxn>("TodoDbContextTrxn", tags: healthCheckTagsFullDb)
            .AddDbContextCheck<TodoDbContextQuery>("TodoDbContextQuery", tags: healthCheckTagsFullDb)
            .AddCheck<WeatherServiceHealthCheck>("External Service", tags: healthCheckTagsFullExt);

        //Todo - for http clients previously registered in infrastructure services, add header propagation here since it only applies at runtime when an http context is present

        return services;
    }
}
