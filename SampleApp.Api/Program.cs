using Azure.Identity;
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
var env = config.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? config.GetValue<string>("DOTNET_ENVIRONMENT") ?? "Undefined"; // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-9.0
var credential = CreateAzureCredential(config);
ILogger<Program> startupLogger = CreateStartupLogger();
startupLogger.LogInformation("{AppName} {Environment} - Startup.", appName, env);

try
{
    startupLogger.LogInformation("{AppName} {Environment} - Configure service defaults.", appName, env);
    builder.AddServiceDefaults(config, appName); //aspire - open telementry (logging, traces, metrics), resilience, service discovery, health checks

    LoadConfiguration();
    ConfigureDataProtection();

    startupLogger.LogInformation("{AppName} {Environment} - Register services.", appName, env);
    services
        //infrastructure - caches, DbContexts, repos, external service sdks/proxies, startup tasks
        .RegisterInfrastructureServices(config)
        //domain services
        .RegisterDomainServices(config)
        //app services
        .RegisterApplicationServices(config)
        //background services
        .RegisterBackgroundServices(config)
        //api services - controllers, versioning, health checks, openapidoc, telemetry
        .RegisterApiServices(config, startupLogger);

    var app = builder.Build().ConfigurePipeline();

    startupLogger.LogInformation("{AppName} {Environment} - Running startup tasks.", appName, env);
    await app.RunStartupTasks();

    //static logger factory setup - re-configure for application static logging using the DI logger factory
    StaticLogging.SetStaticLoggerFactory(app.Services.GetRequiredService<ILoggerFactory>());

    startupLogger.LogInformation("{AppName} {Environment} - Running app.", appName, env);
    await app.RunAsync();
}
catch (Exception ex)
{
    startupLogger.LogCritical(ex, "{AppName} {Environment} - Host terminated unexpectedly.", appName, env);
}
finally
{
    startupLogger.LogInformation("{AppName} {Environment} - Ending application.", appName, env);
}

ILogger<Program> CreateStartupLogger()
{
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
    return StaticLogging.CreateLogger<Program>();
}

/// <summary>
/// Creates and configures a DefaultAzureCredential for authentication with Azure services.
/// </summary>
/// <param name="config">The configuration to read credential settings from</param>
/// <returns>A configured DefaultAzureCredential instance</returns>
static DefaultAzureCredential CreateAzureCredential(IConfiguration config)
{
    //set up DefaultAzureCredential for subsequent use in configuration providers
    //https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Azure.Identity/1.8.0/api/Azure.Identity/Azure.Identity.DefaultAzureCredentialOptions.html
    var credentialOptions = new DefaultAzureCredentialOptions();
    //Specifies the client id of a user assigned ManagedIdentity. 
    string? credOptionsManagedIdentity = config.GetValue<string?>("ManagedIdentityClientId", null);
    if (credOptionsManagedIdentity != null) credentialOptions.ManagedIdentityClientId = credOptionsManagedIdentity;
    //Specifies the tenant id of the preferred authentication account, to be retrieved from the shared token cache for single sign on authentication with development tools, in the case multiple accounts are found in the shared token.
    string? credOptionsTenantId = config.GetValue<string?>("SharedTokenCacheTenantId", null);
    if (credOptionsTenantId != null) credentialOptions.SharedTokenCacheTenantId = credOptionsTenantId;
    return new DefaultAzureCredential(credentialOptions);
}

void LoadConfiguration()
{
    //Azure AppConfig
    var appConfig = config.GetSection("AzureAppConfig");
    if (appConfig.GetChildren().Any())
    {
        var appConfigEndpoint = appConfig.GetValue<string>("Endpoint");
        startupLogger.LogInformation("{AppName} {Environment} - Add Azure App Configuration {Endpoint}", appName, env, appConfigEndpoint);
        //this middleware will check the Azure App Config Sentinel for a change which triggers reloading the configuration
        //middleware triggers on http request (not a background service scope)
        builder.AddAzureAppConfiguration(appConfigEndpoint!, credential, env, appConfig.GetValue<string>($"{appName}Sentinel"), appConfig.GetValue("RefreshCacheExpireTimeSpan", new TimeSpan(1, 0, 0)),
            appName, "Shared");
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

    //load user secrets here which will override all previous (appsettings.json, env vars, Azure App Config, etc)
    //user secrets are only available when running locally
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>();
    }
}

void ConfigureDataProtection()
{
    //Data Protection - use blob storage (key file) and AKV; server farm/instances will all use the same keys
    //register here since credential has been configured
    //https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview
    string? dataProtectionKeysFileUrl = config.GetValue<string?>("DataProtectionKeysFileUrl", null); //blob key file
    string? dataProtectionEncryptionKeyUrl = config.GetValue<string?>("DataProtectionEncryptionKeyUrl", null); //vault encryption key
    if (!string.IsNullOrEmpty(dataProtectionKeysFileUrl) && !string.IsNullOrEmpty(dataProtectionEncryptionKeyUrl))
    {
        startupLogger.LogInformation("{AppName} {Environment} - Configure Data Protection.", appName, env);
        builder.Services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(new Uri(dataProtectionKeysFileUrl), credential)
            .ProtectKeysWithAzureKeyVault(new Uri(dataProtectionEncryptionKeyUrl), credential);
    }
}

/// <summary>
/// Program class must be explicitly defined for WebApplicationFactory in Test.Endpoints
/// </summary>
#pragma warning disable S1118
public partial class Program { }
#pragma warning restore S1118

