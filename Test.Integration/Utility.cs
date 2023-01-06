using Microsoft.Extensions.Configuration;
using System;

namespace Test.Integration;

public static class Utility
{
    public static readonly IConfigurationRoot Config = BuildConfiguration();

    static Utility()
    {
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var devEnvironmentVariable = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var isDevelopment = devEnvironmentVariable?.ToLower() == "development";

        var builder = Support.Utility.BuildConfiguration();

        if (isDevelopment) builder.AddUserSecrets<ServiceTestBase>();

        IConfigurationRoot config = builder.Build();

        return config;
    }

}
