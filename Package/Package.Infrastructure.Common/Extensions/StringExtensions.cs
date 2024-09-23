using System.Text.RegularExpressions;

namespace Package.Infrastructure.Common.Extensions;

public static class StringExtensions
{
    public static string ToCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var first = input[..1].ToLower();
        if (input.Length == 1) return first;

        return first + input[1..];
    }

    /// <summary>
    /// Parse a string for integers. The string can contain single numbers or ranges. "1, 3-5, 7, 10-12"
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static List<int> ParseStringForIntegers(this string input)
    {
        var result = new List<int>();

        // Regex to match single numbers or ranges
        string pattern = @"(\d+-\d+|\d+)";
        Regex regex = new(pattern);

        // Find all matches in the input string
        var matches = regex.Matches(input).Select(match => match.Value);

        foreach (var value in matches)
        {
            if (value.Contains('-'))
            {
                // It's a range, split by '-' and generate the range of numbers
                var parts = value.Split('-');
                int start = int.Parse(parts[0]);
                int end = int.Parse(parts[1]);

                for (int i = start; i <= end; i++)
                {
                    result.Add(i);
                }
            }
            else
            {
                // It's a single number, just add it to the result list
                result.Add(int.Parse(value));
            }
        }

        return result;
    }
}
