using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Package.Infrastructure.Host;

public static class IHostApplicationBuilderExtensions
{
    /// <summary>
    /// IHostApplicationBuilder - Load configuration from Azure App Config
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="endpoint"></param>
    /// <param name="credential"></param>
    /// <param name="env"></param>
    /// <param name="sentinelSetting">Observe for refreshing config cache</param>
    /// <param name="cacheExpire">used with sentinelSetting, Timespan to expire cache, default 5 minutes when null</param>
    /// <param name="keyPrefixes"></param>
    public static void AddAzureAppConfiguration(this IHostApplicationBuilder builder, string endpoint,
        DefaultAzureCredential credential, string env, string? sentinelSetting = null, TimeSpan? cacheExpire = null, params string[] keyPrefixes)
    {
        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            options.ConfigurePackageDefaults(endpoint, credential, env, sentinelSetting, cacheExpire, keyPrefixes);
        });
    }
}
