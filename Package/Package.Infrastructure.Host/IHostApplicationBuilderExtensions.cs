﻿using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;

namespace Package.Infrastructure.Host;
public static class IHostApplicationBuilderExtensions
{
    /// <summary>
    /// Load configuration from Azure App Config
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
            options.Connect(new Uri(endpoint), credential);

            foreach (var prefix in keyPrefixes.Distinct())
            {
                var trimmedPrefix = prefix.TrimEnd(':') + ":"; // ensure proper trailing slash
                options.Select($"{trimmedPrefix}*", LabelFilter.Null).TrimKeyPrefix(trimmedPrefix); // no label (global) SampleApi:* all env
                options.Select($"{trimmedPrefix}*", env).TrimKeyPrefix(trimmedPrefix);              // environment-specific
            }

            //app config may contain references to AKV
            options.ConfigureKeyVault(kv =>
            {
                kv.SetCredential(credential);
            });

            if (sentinelSetting != null)
            {
                options.ConfigureRefresh(refresh =>
                {
                    refresh.Register(sentinelSetting, refreshAll: true).SetRefreshInterval(cacheExpire ?? new TimeSpan(1, 0, 0));
                });
            }
        });
    }
}
