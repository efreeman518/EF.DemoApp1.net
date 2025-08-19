using Microsoft.EntityFrameworkCore.Query;

namespace Package.Infrastructure.Data.Contracts;
/// <summary>
/// Configuration for split query decision thresholds
/// </summary>
public class SplitQueryOptions
{
    /// <summary>
    /// Force split query regardless of other settings
    /// </summary>
    public bool ForceSplitQuery { get; set; } = false;

    /// <summary>
    /// Minimum number of includes that triggers split query
    /// </summary>
    public int MinIncludeCountForSplit { get; set; } = 2;

    /// <summary>
    /// Page size that triggers split query when includes are present
    /// </summary>
    public int PageSizeThresholdWithIncludes { get; set; } = 25;

    /// <summary>
    /// Total record count that triggers split query regardless of page size
    /// </summary>
    public int TotalCountThresholdForSplit { get; set; } = 1000;

    /// <summary>
    /// Page size that always triggers split query regardless of other factors
    /// </summary>
    public int AlwaysSplitPageSize { get; set; } = 100;

    /// <summary>
    /// Default threshold configuration
    /// </summary>
    public static SplitQueryOptions Default => new();

    /// <summary>
    /// Conservative thresholds for high-performance scenarios
    /// </summary>
    public static SplitQueryOptions Conservative => new()
    {
        MinIncludeCountForSplit = 2,
        PageSizeThresholdWithIncludes = 15,
        TotalCountThresholdForSplit = 500,
        AlwaysSplitPageSize = 50
    };

    /// <summary>
    /// Aggressive thresholds that favor single queries
    /// </summary>
    public static SplitQueryOptions Aggressive => new()
    {
        MinIncludeCountForSplit = 4,
        PageSizeThresholdWithIncludes = 50,
        TotalCountThresholdForSplit = 2000,
        AlwaysSplitPageSize = 200
    };

    /// <summary>
    /// Determines split query strategy when total count is known
    /// </summary>
    public static bool DetermineSplitQueryWithTotal<T>(int? pageSize, int totalCount,
        Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes, SplitQueryOptions thresholds)
    {
        var includeCount = includes?.Length ?? 0;
        var currentPageSize = pageSize ?? totalCount; // If no page size, we're getting all records

        // Enhanced decision logic using actual total count
        return
            // Large dataset with includes - definitely use split query
            (totalCount >= thresholds.TotalCountThresholdForSplit && includeCount > 0) ||

            // Multiple includes with any significant dataset
            (includeCount >= thresholds.MinIncludeCountForSplit && totalCount > 100) ||

            // Large page size with includes
            (currentPageSize >= thresholds.PageSizeThresholdWithIncludes && includeCount > 0) ||

            // Very large page sizes always split
            (currentPageSize >= thresholds.AlwaysSplitPageSize) ||

            // High cartesian explosion risk calculation
            (EstimateCartesianExplosionRisk(currentPageSize, totalCount, includeCount) > 10000);
    }

    /// <summary>
    /// Estimates the potential cartesian explosion risk
    /// </summary>
    private static int EstimateCartesianExplosionRisk(int pageSize, int totalCount, int includeCount)
    {
        if (includeCount == 0) return 0;

        // Simple heuristic: assume each include could multiply result set by 2-5x
        // This is a rough estimate - real cartesian explosion depends on actual data relationships
        var estimatedMultiplier = Math.Pow(3, includeCount); // Conservative estimate
        var estimatedResultSize = Math.Min(pageSize, totalCount) * estimatedMultiplier;

        return (int)estimatedResultSize;
    }
}