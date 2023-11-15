using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace SampleApp.Bootstrapper.HealthChecks;

//https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-7.0

public class MemoryCheckOptions
{
    // Failure threshold (in bytes)
    public long Threshold { get; set; } = 1024L * 1024L * 1024L; //1Gb default
}

public class MemoryHealthCheck(IOptionsMonitor<MemoryCheckOptions> options) : IHealthCheck
{
    private readonly IOptionsMonitor<MemoryCheckOptions> _options = options;

    public static string Name => "memory_check";

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var options = _options.Get(context.Registration.Name);

        // Include GC information in the reported diagnostics.
        var allocated = GC.GetTotalMemory(forceFullCollection: false);
        var data = new Dictionary<string, object>()
            {
                { "AllocatedBytes", allocated },
                { "Gen0Collections", GC.CollectionCount(0) },
                { "Gen1Collections", GC.CollectionCount(1) },
                { "Gen2Collections", GC.CollectionCount(2) },
            };

        var status = (allocated < options.Threshold) ? HealthStatus.Healthy : HealthStatus.Degraded;

        return Task.FromResult(new HealthCheckResult(
            status,
            description: $"Reports degraded status if allocated bytes >= {options.Threshold} bytes.",
            exception: null,
            data: data));
    }
}

public static class GCInfoHealthCheckBuilderExtensions
{
    public static IHealthChecksBuilder AddMemoryHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        long? thresholdInBytes = null)
    {
        // Register a check of type GCInfo.
        builder.AddCheck<MemoryHealthCheck>(name, failureStatus ?? HealthStatus.Degraded, tags, null);

        // Configure named options to pass the threshold into the check.
        if (thresholdInBytes.HasValue)
        {
            builder.Services.Configure<MemoryCheckOptions>(name, options =>
            {
                options.Threshold = thresholdInBytes.Value;
            });
        }

        return builder;
    }
}
