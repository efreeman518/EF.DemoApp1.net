using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleApp.Bootstrapper;

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        // Minimal config: in-memory defaults to avoid external dependencies
        var dict = new Dictionary<string, string?>
        {
            ["AzureAppConfig:Endpoint"] = null,
            ["CacheSettings:0:Name"] = "DefaultCache",
            ["CacheSettings:0:DurationMinutes"] = "1",
            ["CacheSettings:0:DistributedCacheDurationMinutes"] = "1",
            ["CacheSettings:0:FailSafeMaxDurationMinutes"] = "1",
            ["CacheSettings:0:FailSafeThrottleDurationMinutes"] = "1",
            ["CacheSettings:0:RedisConfigurationSection:EndpointUrl"] = "localhost",
            ["CacheSettings:0:RedisConfigurationSection:Port"] = "6379"
        };
        cfg.AddInMemoryCollection(dict!);
    })
    .ConfigureServices((ctx, services) =>
    {
        services
            .RegisterDomainServices(ctx.Configuration)
            .RegisterInfrastructureServices(ctx.Configuration)
            .RegisterApplicationServices(ctx.Configuration)
            .RegisterBackgroundServices(ctx.Configuration);
    })
    .Build();

// Resolve service provider to ensure DI graph composes
_ = host.Services.GetRequiredService<IServiceProvider>();

Console.WriteLine("Composition OK");
