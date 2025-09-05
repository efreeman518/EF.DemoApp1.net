namespace Package.Infrastructure.AspNetCore.HealthChecks;

/// <summary>
/// To log every time, configure in DI: services.Configure<HealthLoggingPublisherSettings>(o => o.LogOnlyOnChange = false);
/// </summary>
public sealed class HealthLoggingPublisherSettings
{
    /// <summary>
    /// When true, logs only when the overall health status changes from the last observed status.
    /// When false, logs every time the publisher runs.
    /// </summary>
    public bool LogOnlyOnChange { get; init; } = true;
}