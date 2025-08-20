using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections;
using System.Linq.Expressions;

namespace Package.Infrastructure.Data.Contracts;
/// <summary>
/// Configuration for split query decision thresholds
/// </summary>
public class SplitQueryThresholdOptions
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
    /// The estimated number of records returned from a join that would trigger a split query.
    /// </summary>
    public int CartesianExplosionRiskThreshold { get; set; } = 10000;

    /// <summary>
    /// Default threshold configuration
    /// </summary>
    public static SplitQueryThresholdOptions Default => new();

    /// <summary>
    /// Conservative thresholds for high-performance scenarios, favoring split queries.
    /// </summary>
    public static SplitQueryThresholdOptions Conservative => new()
    {
        MinIncludeCountForSplit = 2,
        PageSizeThresholdWithIncludes = 15,
        TotalCountThresholdForSplit = 500,
        AlwaysSplitPageSize = 50,
        CartesianExplosionRiskThreshold = 5000
    };

    /// <summary>
    /// Aggressive thresholds that favor single queries since multiple round trips are expensive.
    /// </summary>
    public static SplitQueryThresholdOptions Aggressive => new()
    {
        MinIncludeCountForSplit = 4,
        PageSizeThresholdWithIncludes = 50,
        TotalCountThresholdForSplit = 2000,
        AlwaysSplitPageSize = 200,
        CartesianExplosionRiskThreshold = 20000
    };

    /// <summary>
    /// Determines whether to use a split query based on query characteristics and configured thresholds.
    /// A split query is only effective when loading related collections via Include().
    /// </summary>
    /// <typeparam name="T">The type of the entity being queried.</typeparam>
    /// <param name="pageSize">The number of records requested in the current page.</param>
    /// <param name="totalCount">The total number of records in the dataset. Use -1 if unknown.</param>
    /// <param name="includes">The collection of Include expressions for the query.</param>
    /// <param name="options">The configured thresholds for making the decision.</param>
    /// <returns>True to use a split query; otherwise, false.</returns>
    public static bool DetermineSplitQueryWithTotal<T>(int? pageSize, int totalCount,
        Expression<Func<IQueryable<T>, IIncludableQueryable<T, object?>>>[] includes, SplitQueryThresholdOptions options)
    {
        if (options.ForceSplitQuery) return true;

        var includeCount = CountCollectionIncludes(includes);
        if (includeCount == 0) return false; // Split query has no effect without collection includes.

        // If pageSize is null, it implies an unbounded query.
        // If totalCount is also unknown (-1), treat the size as int.MaxValue to force size-based rules to trigger.
        // Otherwise, use the known totalCount.
        var currentPageSize = pageSize ?? (totalCount >= 0 ? totalCount : int.MaxValue);

        // The decision is based on a set of rules. If any rule is met, a split query is recommended.
        return
            // 1. Total dataset is large (and known), and there are included collections.
            (totalCount >= options.TotalCountThresholdForSplit) ||

            // 2. The number of included collections meets the minimum threshold.
            (includeCount >= options.MinIncludeCountForSplit) ||

            // 3. The requested page size is large.
            (currentPageSize >= options.PageSizeThresholdWithIncludes) ||

            // 4. The requested page size is very large, making a split query almost always preferable.
            (currentPageSize >= options.AlwaysSplitPageSize) ||

            // 5. The estimated risk of cartesian explosion is high.
            (EstimateCartesianExplosionRisk(currentPageSize, includeCount) > options.CartesianExplosionRiskThreshold);
    }

    /// <summary>
    /// Estimates the potential cartesian explosion risk based on page size and include count.
    /// This heuristic models the potential for the result set to grow exponentially with each collection join.
    /// </summary>
    /// <param name="pageSize">The number of root entities being fetched.</param>
    /// <param name="includeCount">The number of included collections.</param>
    /// <returns>An integer representing the estimated size of the result set from a single query.</returns>
    private static int EstimateCartesianExplosionRisk(int pageSize, int includeCount)
    {
        // A simple heuristic: assume each included collection multiplies the result set size.
        // The multiplier (e.g., 3) is a conservative estimate of the average number of related
        // entities per root entity. This value may need tuning based on the actual data model.
        var estimatedMultiplier = Math.Pow(3, includeCount);
        var estimatedResultSize = pageSize * estimatedMultiplier;

        return (int)estimatedResultSize;
    }

    /// <summary>
    /// Analyzes the expression trees of the includes to count how many are for collections.
    /// </summary>
    private static int CountCollectionIncludes<T>(Expression<Func<IQueryable<T>, IIncludableQueryable<T, object?>>>[]? includes)
    {
        if (includes is null || includes.Length == 0) return 0;

        var visitor = new CollectionIncludeVisitor();
        foreach (var include in includes)
        {
            visitor.Visit(include);
        }
        return visitor.CollectionIncludeCount;
    }

    /// <summary>
    /// An ExpressionVisitor that traverses Include/ThenInclude chains to count collection navigations.
    /// </summary>
    private sealed class CollectionIncludeVisitor : ExpressionVisitor
    {
        public int CollectionIncludeCount { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Check if the method is Include() or ThenInclude()
            if (node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
                (node.Method.Name == nameof(EntityFrameworkQueryableExtensions.Include) ||
                 node.Method.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude)) &&
                node.Arguments.Count > 1 && node.Arguments[1] is LambdaExpression lambda &&
                typeof(IEnumerable).IsAssignableFrom(lambda.ReturnType) && lambda.ReturnType != typeof(string))
            {
                CollectionIncludeCount++;
            }
            return base.VisitMethodCall(node);
        }
    }
}