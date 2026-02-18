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
        string basePath = AppContext.BaseDirectory;
        string? fileName = null;
        if (path != null)
        {
            string resolvedPath = ResolveJsonConfigPath(path);
            basePath = Path.GetDirectoryName(resolvedPath) ?? AppContext.BaseDirectory;
            fileName = Path.GetFileName(resolvedPath);
        }

        var builder = new ConfigurationBuilder().SetBasePath(basePath);
        if (fileName != null) builder.AddJsonFile(fileName);
        if (includeEnvironmentVars) builder.AddEnvironmentVariables();
        var config = builder.Build();
        string env = config.GetValue<string>("ASPNETCORE_ENVIRONMENT", "development")!.ToLower();
        builder.AddJsonFile($"appsettings.{env}.json", true);
        return builder;
    }

    public static string ResolveJsonConfigPath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        string? foundPath = GetCandidateDirectories()
            .Select(directory => Path.Combine(directory, path))
            .FirstOrDefault(File.Exists);

        return foundPath ?? Path.Combine(AppContext.BaseDirectory, path);
    }

    private static IEnumerable<string> GetCandidateDirectories()
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string candidate in EnumerateCurrentAndParents(AppContext.BaseDirectory))
        {
            if (visited.Add(candidate))
            {
                yield return candidate;
            }
        }

        foreach (string candidate in EnumerateCurrentAndParents(Directory.GetCurrentDirectory()))
        {
            if (visited.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private static IEnumerable<string> EnumerateCurrentAndParents(string startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
        {
            yield break;
        }

        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            yield return dir.FullName;
            dir = dir.Parent;
        }
    }

    private static readonly char[] chars = "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
    public static string RandomString(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");

        Span<char> result = stackalloc char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[Random.Shared.Next(chars.Length)];
        }
        return new string(result);
    }
}
