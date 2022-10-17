using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Test.Integration;

public static class Utility
{
    public static readonly IConfigurationRoot Config;

    static Utility()
    {
        Config = BuildConfiguration();
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var devEnvironmentVariable = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var isDevelopment = devEnvironmentVariable?.ToLower() == "development";

        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json");

        if (isDevelopment) builder.AddUserSecrets<ServiceTestBase>();

        IConfigurationRoot config = builder.Build();

        return config;
    }

}
