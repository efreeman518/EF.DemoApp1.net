using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Package.Infrastructure.Utility;

public static class ValidationUtility
{
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        // Use IdnMapping class to convert Unicode domain names.
        try
        {
            email = Regex.Replace(email, @"(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200));
        }
        catch (Exception)
        {
            return false;
        }

        // Return true if email is in valid e-mail format.
        try
        {
            return Regex.IsMatch(email,
                  @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                  @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                  RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// convert Unicode domain names.
    /// </summary>
    /// <param name="match"></param>
    /// <returns></returns>
    private static string DomainMapper(Match match)
    {
        // IdnMapping class with default property values.
        IdnMapping idn = new();
        string domainName = match.Groups[2].Value;
        domainName = idn.GetAscii(domainName);
        return match.Groups[1].Value + domainName;
    }
}
