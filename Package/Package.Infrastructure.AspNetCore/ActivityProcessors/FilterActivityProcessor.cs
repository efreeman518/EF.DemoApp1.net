using OpenTelemetry;
using System.Diagnostics;

namespace Package.Infrastructure.AspNetCore.ActivityProcessors;

/// <summary>
/// MSAL is super chatty in log traces which does not abide by ILogger filters so squash it with this
/// </summary>
public class FilterActivityProcessor(IEnumerable<string> blockedKeywords) : BaseProcessor<Activity>
{
    private readonly StringMatcher _matcher = new(blockedKeywords);

    public override void OnEnd(Activity activity)
    {
        ReadOnlySpan<char> displayName = activity.DisplayName;

        // Check display name using the StringMatcher
        if (_matcher.ContainsKeyword(displayName))
        {
            activity.IsAllDataRequested = false;
            return;
        }

        // Check message/description if available
        if (activity.DisplayName != null && _matcher.ContainsKeyword(activity.DisplayName))
        {
            activity.IsAllDataRequested = false;
            return;
        }

        // Check event name if available
        if (activity.OperationName != null && _matcher.ContainsKeyword(activity.OperationName))
        {
            activity.IsAllDataRequested = false;
            return;
        }

#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions - performance over readability here
        foreach (var @event in activity.Events)
        {
            if (@event.Name != null && _matcher.ContainsKeyword(@event.Name))
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }

        foreach (var tag in activity.Tags)
        {
            if (tag.Value is string tagValue)
            {
                // Check tags using the StringMatcher
                if (_matcher.ContainsKeyword(tagValue))
                {
                    activity.IsAllDataRequested = false;
                    return;
                }
            }
            else if (tag.Value != null)
            {
                var tagValue1 = tag.Value.ToString();
                if (tagValue1 != null && _matcher.ContainsKeyword(tagValue1))
                {
                    activity.IsAllDataRequested = false;
                    return;
                }
            }
        }
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
    }
}
