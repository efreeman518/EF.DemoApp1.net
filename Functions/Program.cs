using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Functions;
using Functions.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using Package.Infrastructure.Host;
using SampleApp.Bootstrapper;


/// <summary>
/// https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide
/// retries - https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-error-pages?tabs=fixed-delay%2Cin-process&pivots=programming-language-csharp#retries
/// </summary>
///

const string SERVICE_NAME = "FunctionsApp";
ILogger<Program> loggerStartup = null!;

try
{
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.SetMinimumLevel(LogLevel.Information);
        builder.AddConsole();
        //builder.AddApplicationInsights();
    });

    // Assign loggerStartup after loggerFactory is created
    loggerStartup = loggerFactory.CreateLogger<Program>();
    loggerStartup.LogInformation("{ServiceName} - Startup.", SERVICE_NAME);

    var builder = FunctionsApplication.CreateBuilder(args);
    builder.ConfigureFunctionsWebApplication();

    builder.Configuration.AddJsonFile("appsettings.json", optional: true);
    var config = builder.Configuration;
    // NOTE: It's important to add json config sources before the call to ConfigureFunctionsWorkerDefaults as this
    // adds environment variables into configuration enabling overrides by azure configuration settings.
    config.AddJsonFile("appsettings.json", optional: true);
    //set up DefaultAzureCredential for subsequent use in configuration providers
    //https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Azure.Identity/1.8.0/api/Azure.Identity/Azure.Identity.DefaultAzureCredentialOptions.html
    var credentialOptions = new DefaultAzureCredentialOptions();
    //Specifies the client id of a user assigned ManagedIdentity. 
    string? credOptionsManagedIdentity = config.GetValue<string?>("ManagedIdentityClientId", null);
    if (credOptionsManagedIdentity != null) credentialOptions.ManagedIdentityClientId = credOptionsManagedIdentity;
    //Specifies the tenant id of the preferred authentication account, to be retrieved from the shared token cache for single sign on authentication with development tools, in the case multiple accounts are found in the shared token.
    string? credOptionsTenantId = config.GetValue<string?>("SharedTokenCacheTenantId", null);
    if (credOptionsTenantId != null) credentialOptions.SharedTokenCacheTenantId = credOptionsTenantId;
    var credential = new DefaultAzureCredential(credentialOptions);

    var env = config.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? "Development";
    string? endpoint;

    //Azure AppConfig
    var appConfig = config.GetSection("AzureAppConfig");
    if (appConfig.GetChildren().Any())
    {
        endpoint = appConfig.GetValue<string>("Endpoint");
        loggerStartup.LogInformation("{AppName} - Add Azure App Configuration {Endpoint} {Environment}", SERVICE_NAME, endpoint, env);
        builder.AddAzureAppConfiguration(endpoint!, credential, env, appConfig.GetValue<string>($"{SERVICE_NAME}Sentinel"), appConfig.GetValue("RefreshCacheExpireTimeSpan", new TimeSpan(1, 0, 0)),
            "SampleApi", "Shared");
    }

    //Azure Key Vault - load AKV direct (not through Azure AppConfig or App Service-Configuration-AppSettings)
    //endpoint = config.GetValue<string>("KeyVaultEndpoint");
    //if (!string.IsNullOrEmpty(endpoint))
    //{
    //    loggerStartup.LogInformation("{AppName} - Add KeyVault {Endpoint} Configuration", SERVICE_NAME, endpoint);
    //    builder.AddAzureKeyVault(new Uri(endpoint), credential);
    //}

    //load user secrets here which will override all previous (appsettings.json, env vars, Azure App Config, etc)
    //user secrets are only available when running locally
    //if (builder.Environment.IsDevelopment())
    //{
    config.AddUserSecrets<Program>();
    //}

    // Logging and OpenTelemetry
    builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    builder.Logging.AddOpenTelemetry(options =>
    {
        options.AddConsoleExporter();
        options.AddAzureMonitorLogExporter(options =>
        {
            options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        });
    });

    // Application Insights
    builder.Services
        .AddApplicationInsightsTelemetryWorkerService(builder.Configuration)
        .ConfigureFunctionsApplicationInsights()
        .Configure<LoggerFilterOptions>(options =>
        {
            var aiProvider = options.Rules.FirstOrDefault(rule =>
                rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

            if (aiProvider is not null)
                options.Rules.Remove(aiProvider);
        });

    builder.Services
        //domain services
        .RegisterDomainServices(config)
        //infrastructure - caches, DbContexts, repos, external service proxies, startup tasks
        .RegisterInfrastructureServices(config)
        //app services
        .RegisterApplicationServices(config)
        //BackgroundTaskQueue needed by other services
        .RegisterBackgroundServices(config)
        //Function app specific registrations
        .AddTransient<IDatabaseService, DatabaseService>()
        //Configuration, enables injecting IOptions<>
        .Configure<Settings1>(config.GetSection("Settings1"));

    // Register middleware
    builder.UseMiddleware<GlobalExceptionHandler>();

    var app = builder.Build();

    await app.RunStartupTasks();
    await app.RunAsync();
}
catch (Exception ex)
{
    loggerStartup?.LogCritical(ex, "{ServiceName} - Host terminated unexpectedly.", SERVICE_NAME);
}
finally
{
    loggerStartup?.LogInformation("{ServiceName} - Ending application.", SERVICE_NAME);
}