using System.Text;
using System.Text.RegularExpressions;

namespace Package.Infrastructure.Common.Extensions;
public static class RegexExtensions
{
    public static async Task<string> ReplaceAsync(this Regex regex, string input, Func<Match, Task<string>> funcReplacement)
    {
        var sb = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in regex.Matches(input))
        {
            sb.Append(input, lastIndex, match.Index - lastIndex)
              .Append(await funcReplacement(match).ConfigureAwait(false));

            lastIndex = match.Index + match.Length;
        }

        sb.Append(input, lastIndex, input.Length - lastIndex);
        return sb.ToString();
    }
}
