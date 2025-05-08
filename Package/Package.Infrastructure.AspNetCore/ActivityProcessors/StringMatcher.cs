namespace Package.Infrastructure.AspNetCore.ActivityProcessors; //copied from Package.Infrastructure.Common to avoid reference for a single utility
public class StringMatcher
{
    private readonly ReadOnlyMemory<string> _keywords;
    private readonly int _keywordCount;

    public StringMatcher(IEnumerable<string> keywords)
    {
        _keywords = new ReadOnlyMemory<string>([.. keywords]);
        _keywordCount = _keywords.Length;
    }

    public bool ContainsKeyword(ReadOnlySpan<char> input)
    {
        // Early exit for empty input or no keywords
        if (input.IsEmpty || _keywordCount == 0)
            return false;

        // Special check specifically for MSAL traces that start with "False MSAL"
        //if (input.Length > 10 && input.StartsWith("False MSAL", StringComparison.OrdinalIgnoreCase))
        //    return true;

        //// Special check for Key Vault calls which might not have MSAL explicitly mentioned
        //if (input.Length > 10 &&
        //    (input.Contains(".vault.azure.net", StringComparison.OrdinalIgnoreCase) ||
        //     input.Contains("DefaultAzureCredential", StringComparison.OrdinalIgnoreCase) ||
        //     input.Contains("ManagedIdentityCredential", StringComparison.OrdinalIgnoreCase)))
        //    return true;

        // Optimization for single keyword case
        if (_keywordCount == 1)
            return input.Contains(_keywords.Span[0].AsSpan(), StringComparison.OrdinalIgnoreCase);

        // For small to medium number of keywords, sequential is faster
        // For larger sets, consider parallelization with appropriate threshold
#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions - performance over readability here
        foreach (var keyword in _keywords.Span)
        {
            // Skip empty keywords
            if (keyword.Length == 0)
                continue;

            // Use StringComparison.OrdinalIgnoreCase for fast, culture-invariant comparison
            if (input.Contains(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
        return false;
    }

    // Optional: Allow direct matching against string
    public bool ContainsKeyword(string input)
    {
        return input is not null && ContainsKeyword(input.AsSpan());
    }
}
