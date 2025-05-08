using OpenTelemetry;
using System.Diagnostics;

namespace Package.Infrastructure.AspNetCore.ActivityProcessors;

/// <summary>
/// MSAL is super chatty in log traces which does not abide by ILogger filters so squash it with this
/// </summary>
public class MsalFilteringActivityProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // You could filter here if needed
    }

    public override void OnEnd(Activity activity)
    {
        if (activity.DisplayName.Contains("MSAL", StringComparison.OrdinalIgnoreCase))
        {
            // Suppress export by marking it as non-recording
            activity.IsAllDataRequested = false;
        }
    }
}