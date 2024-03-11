using Microsoft.Extensions.Configuration;

namespace Test.Support;

public static class Utility
{
    /// <summary>
    /// For loading config for tests since we don't have a host that automatically loads it
    /// </summary>
    /// <param name="path"></param>
    /// <param name="includeEnvironmentVars"></param>
    /// <returns>Config builder for further composition and the environment</returns>
    public static IConfigurationBuilder BuildConfiguration(string? path = "appsettings.json", bool includeEnvironmentVars = true)
    {
        //order matters here (last wins)
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
        if (path != null) builder.AddJsonFile(path);
        if (includeEnvironmentVars) builder.AddEnvironmentVariables();

        var config = builder.Build();
        string env = config.GetValue<string>("ASPNETCORE_ENVIRONMENT", "development")!.ToLower();
        builder.AddJsonFile($"appsettings.{env}.json", true);

        return builder;
    }

    private static readonly Random random = new();
    public static string RandomString(int length)
    {
        const string chars = "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
