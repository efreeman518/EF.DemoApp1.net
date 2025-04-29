using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.DataProtection;
using Package.Infrastructure.Common;
using Package.Infrastructure.Host;
using SampleApp.Gateway;

//CreateBuilder defaults:
//- config gets 'ASPNETCORE_*' env vars, appsettings.json and appsettings.{Environment}.json, user secrets
//- logging gets Console
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;
var appName = config.GetValue<string>("AppName");
var env = config.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? "Development";
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]!;

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
loggerStartup.LogInformation("{AppName} env:{Environment} - Startup.", appName, env);

try
{
    loggerStartup.LogInformation("{AppName} env:{Environment} - Configure app logging.", appName, env);
    builder.Logging
        .ClearProviders()
        .AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.AddAzureMonitorLogExporter(options =>
            {
                options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]!;
            });
        });
    //old way
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
    string? credOptionsManagedIdentity = builder.Configuration.GetValue<string?>("ManagedIdentityClientId", null);
    if (credOptionsManagedIdentity != null) credentialOptions.ManagedIdentityClientId = credOptionsManagedIdentity;
    //Specifies the tenant id of the preferred authentication account, to be retrieved from the shared token cache for single sign on authentication with development tools, in the case multiple accounts are found in the shared token.
    string? credOptionsTenantId = builder.Configuration.GetValue<string?>("SharedTokenCacheTenantId", null);
    if (credOptionsTenantId != null) credentialOptions.SharedTokenCacheTenantId = credOptionsTenantId;
    var credential = new DefaultAzureCredential(credentialOptions);

    //configuration
    string? endpoint;

    //Azure AppConfig
    var appConfig = builder.Configuration.GetSection("AzureAppConfig");
    if (appConfig.GetChildren().Any())
    {
        endpoint = appConfig.GetValue<string>("Endpoint");
        loggerStartup.LogInformation("{AppName} - Add Azure App Configuration {Endpoint} {Environment}", appName, endpoint, env);
        builder.AddAzureAppConfiguration(endpoint!, credential, env, appConfig.GetValue<string>("Sentinel"), appConfig.GetValue("RefreshCacheExpireTimeSpan", new TimeSpan(1, 0, 0)),
            "Gateway", "Shared");
    }

    //Data Protection - use blobstorage (key file) and keyvault; server farm/instances will all use the same keys
    //register here since credential has been configured
    //https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview
    string? dataProtectionKeysFileUrl = builder.Configuration.GetValue<string?>("DataProtectionKeysFileUrl", null); //blob key file
    string? dataProtectionEncryptionKeyUrl = builder.Configuration.GetValue<string?>("DataProtectionEncryptionKeyUrl", null); //vault encryption key
    if (!string.IsNullOrEmpty(dataProtectionKeysFileUrl) && !string.IsNullOrEmpty(dataProtectionEncryptionKeyUrl))
    {
        loggerStartup.LogInformation("{AppName} env:{Environment} - Configure Data Protection.", appName, env);
        builder.Services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(new Uri(dataProtectionKeysFileUrl), credential)
            .ProtectKeysWithAzureKeyVault(new Uri(dataProtectionEncryptionKeyUrl), credential);
    }

    //register services
    services.RegisterServices(config, loggerStartup);

    var app = builder.Build();

    //configure pipeline
    app.ConfigurePipeline();

    await app.RunAsync();
}
catch (Exception ex)
{
    loggerStartup.LogCritical(ex, "{AppName} env:{Environment} - Host terminated unexpectedly.", appName, env);
}
finally
{
    loggerStartup.LogInformation("{AppName} env:{Environment} - Ending application.", appName, env);
}




