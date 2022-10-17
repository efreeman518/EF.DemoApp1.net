namespace Package.Infrastructure.Utility.Extensions;

public static class StringExtensions
{
    public static string ToCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var first = input[..1].ToLower();
        if (input.Length == 1) return first;

        return first + input[1..];
    }
}
