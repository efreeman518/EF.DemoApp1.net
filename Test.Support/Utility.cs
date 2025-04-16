﻿using Microsoft.Extensions.Configuration;

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
