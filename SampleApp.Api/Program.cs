using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.DataProtection;
using Package.Infrastructure.Common;
using Package.Infrastructure.Host;
using SampleApp.Api;
using SampleApp.Bootstrapper;

//CreateBuilder defaults:
//- config gets 'ASPNETCORE_*' env vars, appsettings.json and appsettings.{Environment}.json, user secrets
//- logging gets Console
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var services = builder.Services;
var appName = config.GetValue<string>("AppName")!;
var appInsightsConnectionString = config["ApplicationInsights:ConnectionString"]!;
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-9.0
var env = config.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? config.GetValue<string>("DOTNET_ENVIRONMENT") ?? "Undefined";

//static logger factory setup - for startup
StaticLogging.CreateStaticLoggerFactory(logBuilder =>
{
    logBuilder.SetMinimumLevel(LogLevel.Information);
    logBuilder.AddConsole();
    logBuilder.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
    {
        config.ConnectionString = appInsightsConnectionString;
    },
    configureApplicationInsightsLoggerOptions: (options) => { });
});

//startup logger
ILogger<Program> loggerStartup = StaticLogging.CreateLogger<Program>();
loggerStartup.LogInformation("{AppName} {Environment} - Startup.", appName, env);

try
{
    loggerStartup.LogInformation("{AppName} {Environment} - Configure app logging.", appName, env);
    builder.Logging
        .ClearProviders()
        .AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.AddAzureMonitorLogExporter(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });
        });
    //old way to add app insights logging
    //.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
    //    config.ConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString"),
    //    configureApplicationInsightsLoggerOptions: (options) => { });

    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
    }

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

    //Azure AppConfig
    var appConfig = config.GetSection("AzureAppConfig");
    if (appConfig.GetChildren().Any())
    {
        var appConfigEndpoint = appConfig.GetValue<string>("Endpoint");
        loggerStartup.LogInformation("{AppName} {Environment} - Add Azure App Configuration {Endpoint}", appName, env, appConfigEndpoint);
        builder.AddAzureAppConfiguration(appConfigEndpoint!, credential, env, appConfig.GetValue<string>($"{appName}Sentinel"), appConfig.GetValue("RefreshCacheExpireTimeSpan", new TimeSpan(1, 0, 0)),
            appName, "Shared");
    }

    //load user secrets here which will override all previous (appsettings.json, env vars, Azure App Config, etc)
    //user secrets are only available when running locally
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>();
    }

    //Azure Key Vault - load AKV direct (not through Azure AppConfig or App Service-Configuration-AppSettings)
    //endpoint = builder.Configuration.GetValue<string>("KeyVaultEndpoint");
    //if (!string.IsNullOrEmpty(endpoint))
    //{
    //    loggerStartup.LogInformation("{AppName} - Add KeyVault {Endpoint} Configuration", appName, endpoint);
    //    builder.Configuration.AddAzureKeyVault(new Uri(endpoint), credential);
    //}

    //Custom configuration provider - from DB
    //var connectionString = builder.Configuration.GetConnectionString("TodoDbContextQuery") ?? "";
    //builder.Configuration.AddDatabaseSource(connectionString, new TimeSpan(1, 0, 0));

    //Data Protection - use blob storage (key file) and AKV; server farm/instances will all use the same keys
    //register here since credential has been configured
    //https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview
    string? dataProtectionKeysFileUrl = config.GetValue<string?>("DataProtectionKeysFileUrl", null); //blob key file
    string? dataProtectionEncryptionKeyUrl = config.GetValue<string?>("DataProtectionEncryptionKeyUrl", null); //vault encryption key
    if (!string.IsNullOrEmpty(dataProtectionKeysFileUrl) && !string.IsNullOrEmpty(dataProtectionEncryptionKeyUrl))
    {
        loggerStartup.LogInformation("{AppName} {Environment} - Configure Data Protection.", appName, env);
        services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(new Uri(dataProtectionKeysFileUrl), credential)
            .ProtectKeysWithAzureKeyVault(new Uri(dataProtectionEncryptionKeyUrl), credential);
    }

    loggerStartup.LogInformation("{AppName} {Environment} - Register services.", appName, env);
    services
        //infrastructure - caches, DbContexts, repos, external service sdks/proxies, startup tasks
        .RegisterInfrastructureServices(config, true)
        //domain services
        .RegisterDomainServices(config)
        //app services
        .RegisterApplicationServices(config)
        //background services
        .RegisterBackgroundServices(config)
        //api services - controllers, versioning, health checks, openapidoc, telemetry
        .RegisterApiServices(config, loggerStartup, appName);

    var app = builder.Build().ConfigurePipeline();
    loggerStartup.LogInformation("{AppName} {Environment} - Running startup tasks.", appName, env);
    await app.RunStartupTasks();

    //static logger factory setup - re-configure for application static logging using the DI logger factory
    StaticLogging.SetStaticLoggerFactory(app.Services.GetRequiredService<ILoggerFactory>());

    loggerStartup.LogInformation("{AppName} {Environment} - Running app.", appName, env);

    await app.RunAsync();
}
catch (Exception ex)
{
    loggerStartup.LogCritical(ex, "{AppName} {Environment} - Host terminated unexpectedly.", appName, env);
}
finally
{
    loggerStartup.LogInformation("{AppName} {Environment} - Ending application.", appName, env);
}


/// <summary>
/// Program class must be explicitly defined for WebApplicationFactory in Test.Endpoints
/// </summary>
#pragma warning disable S1118
public partial class Program { }
#pragma warning restore S1118