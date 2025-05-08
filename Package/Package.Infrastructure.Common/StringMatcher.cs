namespace Package.Infrastructure.Common; 
public class StringMatcher(IEnumerable<string> keywords)
{
    private readonly ReadOnlyMemory<string> _keywords = new ReadOnlyMemory<string>([.. keywords]);

    public bool ContainsKeyword(ReadOnlySpan<char> input)
    {
#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions - performance over readability here
        foreach (var keyword in _keywords.Span)
        {
            // Use StringComparison.OrdinalIgnoreCase for fast, culture-invariant comparison
            if (input.Contains(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
        return false;
    }

    // Optional: Allow direct matching against string[]
    public bool ContainsKeyword(string input)
    {
        return ContainsKeyword(input.AsSpan());
    }
}
