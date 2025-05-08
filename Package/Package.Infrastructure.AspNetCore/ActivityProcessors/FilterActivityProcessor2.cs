using OpenTelemetry;
using System.Diagnostics;

namespace Package.Infrastructure.AspNetCore.ActivityProcessors;
public class FilterActivityProcessor2(Predicate<Activity> filter) : BaseProcessor<Activity>
{
    private readonly Predicate<Activity> _filter = filter ?? throw new ArgumentNullException(nameof(filter));

    public override void OnEnd(Activity activity)
    {
        if (_filter(activity))
        {
            // Suppress the activity from being exported
            activity.SetStatus(ActivityStatusCode.Unset); // Correct way to call SetStatus
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
