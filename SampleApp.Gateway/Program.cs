using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Package.Infrastructure.Common;
using SampleApp.Gateway;


//CreateBuilder defaults:
//- config gets 'ASPNETCORE_*' env vars, appsettings.json and appsettings.{Environment}.json, user secrets
//- logging gets Console
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

//static logger factory setup - for startup
StaticLogging.CreateStaticLoggerFactory(logBuilder =>
{
    logBuilder.SetMinimumLevel(LogLevel.Information);
    logBuilder.AddApplicationInsights(configureTelemetryConfiguration: (configTelemetry) =>
            configTelemetry.ConnectionString = config.GetValue<string>("ApplicationInsights:ConnectionString"),
            configureApplicationInsightsLoggerOptions: (options) => { });
    logBuilder.AddConsole();
});

//startup logger
ILogger<Program> loggerStartup = StaticLogging.CreateLogger<Program>();
var appName = config.GetValue<string>("AppName");
loggerStartup.LogInformation("{AppName} - Startup.", appName);

try
{
    loggerStartup.LogInformation("{AppName} - Configure app logging.", appName);
    builder.Logging
        .ClearProviders()
        .AddApplicationInsights(configureTelemetryConfiguration: (config) =>
            config.ConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString"),
            configureApplicationInsightsLoggerOptions: (options) => { });
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

    //Data Protection - use blobstorage (key file) and keyvault; server farm/instances will all use the same keys
    //register here since credential has been configured
    //https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview
    string? dataProtectionKeysFileUrl = builder.Configuration.GetValue<string?>("DataProtectionKeysFileUrl", null); //blob key file
    string? dataProtectionEncryptionKeyUrl = builder.Configuration.GetValue<string?>("DataProtectionEncryptionKeyUrl", null); //vault encryption key
    if (!string.IsNullOrEmpty(dataProtectionKeysFileUrl) && !string.IsNullOrEmpty(dataProtectionEncryptionKeyUrl))
    {
        loggerStartup.LogInformation("{AppName} - Configure Data Protection.", appName);
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
    loggerStartup.LogCritical(ex, "{AppName} - Host terminated unexpectedly.", appName);
}
finally
{
    loggerStartup.LogInformation("{AppName} - Ending application.", appName);
}




