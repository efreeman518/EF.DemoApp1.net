using Azure;

namespace Package.Infrastructure.Common.Extensions;
public static class AsyncPageableExtensions
{
    public static async Task<(IReadOnlyList<T>, string?)> GetPageAsync<T>(this AsyncPageable<T> pageable, string? continuationToken = null, CancellationToken cancellationToken = default)
        where T : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();
        var enumerator = pageable.AsPages(continuationToken).GetAsyncEnumerator(cancellationToken);
        await enumerator.MoveNextAsync();
        var page = enumerator.Current;
        return (page.Values, page.ContinuationToken);
    }
}
