namespace Package.Infrastructure.Common;

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

        // Optimization for single keyword case
        if (_keywordCount == 1)
            return input.Contains(_keywords.Span[0].AsSpan(), StringComparison.OrdinalIgnoreCase);

        // For small to medium number of keywords, sequential is faster
        // For larger sets, consider parallelization with appropriate threshold
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

        return false;
    }

    // Optional: Allow direct matching against string
    public bool ContainsKeyword(string input)
    {
        return input is not null && ContainsKeyword(input.AsSpan());
    }
}