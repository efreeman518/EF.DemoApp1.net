using Microsoft.Azure.Cosmos;

namespace Package.Infrastructure.CosmosDb;
public static class CosmosDbExtensions
{
    /// <summary>
    /// Convert a FeedIterator to IAsyncEnumerable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="iterator"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this FeedIterator<T> iterator)
    {
        while (iterator.HasMoreResults)
        {
            foreach (var item in await iterator.ReadNextAsync())
            {
                yield return item;
            }
        }
    }
}
