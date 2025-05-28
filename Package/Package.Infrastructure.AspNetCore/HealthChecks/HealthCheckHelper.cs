using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Package.Infrastructure.AspNetCore.HealthChecks;

public static class HealthCheckHelper
{
    public static HealthCheckOptions BuildHealthCheckOptions(string tag)
    {
        return new HealthCheckOptions()
        {
            Predicate = (check) => check.Tags.Contains(tag),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse // HealthCheckHelper.WriteHealthReportResponse
        };
    }
}
