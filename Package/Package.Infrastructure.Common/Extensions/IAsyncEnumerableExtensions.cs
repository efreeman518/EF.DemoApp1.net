using System.Collections.Generic;
using System.Threading.Tasks;

namespace Package.Infrastructure.Common.Extensions;
public static class IAsyncEnumerableExtensions
{
    public static async Task<T?> FirstOrDefault<T>(this IAsyncEnumerable<T> asyncEnumerable)
    {
        await foreach (var item in asyncEnumerable)
        {
#pragma warning disable S1751 // Loops with at most one iteration should be refactored
            return item; //return only the first item intended
#pragma warning restore S1751 // Loops with at most one iteration should be refactored
        }
        return default;
    }
}
