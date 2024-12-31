using System.Text.RegularExpressions;

namespace Package.Infrastructure.Common.Extensions;

public static class StringExtensions
{
    public static Stream ToStream(this string input)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(input);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

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


    public static List<string> FindTopMatches(this string target, List<string> list,
        int maxMatches = 4, int distanceThreshold = 10, bool returnExactOnlyIfMatch = true,
        bool prirotizeStartMatch = true, bool ignoreCase = true)
    {
        if (returnExactOnlyIfMatch)
        {
            var match = list.Find(str => string.Equals(str, target, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
            if (match != null) return [match];
        }

        List<string> matches = [];
        if (prirotizeStartMatch)
        {
            matches.AddRange(list.Where(str => str.StartsWith(target, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).Take(maxMatches).ToList());
        }

        matches.AddRange(list.Where(s => s.Contains(target, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).Take(maxMatches).ToList());

        if (matches.Count < maxMatches)
        {
            matches.AddRange(list
                .Select(str => new { Str = str, Distance = LevenshteinDistance(target, str, ignoreCase) })
                .Where(result => result.Distance <= distanceThreshold)
                .OrderBy(result => result.Distance)
                .Take(maxMatches - matches.Count)
                .Select(result => result.Str)
                .ToList());
        }

        return matches;
    }

    public static string? FindClosestMatch(string target, List<string> list, bool ignoreCase = true)
    {
        string? closestMatch = null;
        int smallestDistance = int.MaxValue;

        foreach (var item in list)
        {
            int distance = LevenshteinDistance(target, item, ignoreCase);
            if (distance < smallestDistance)
            {
                smallestDistance = distance;
                closestMatch = item;
            }
        }

        return closestMatch;
    }

    private static int LevenshteinDistance(string source, string target, bool ignoreCase = true)
    {
        // If either string is null, return the length of the other string
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        //case ignore
        if (ignoreCase)
        {
            source = source.ToLower();
            target = target.ToLower();
        }

        int sourceLength = source.Length;
        int targetLength = target.Length;

        // Initialize a matrix
        int[,] distance = new int[sourceLength + 1, targetLength + 1];

        // Initialize the matrix edges
        for (int i = 0; i <= sourceLength; i++) distance[i, 0] = i;
        for (int j = 0; j <= targetLength; j++) distance[0, j] = j;

        // Populate the matrix
        for (int i = 1; i <= sourceLength; i++)
        {
            for (int j = 1; j <= targetLength; j++)
            {
                int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        // The Levenshtein distance is in the bottom-right cell of the matrix
        return distance[sourceLength, targetLength];
    }
}
