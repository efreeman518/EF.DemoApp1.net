using Microsoft.Extensions.Configuration;

namespace Test.Integration;

public static class Utility
{
    public static readonly IConfigurationRoot Config = BuildConfiguration();

    static Utility()
    {
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        //var devEnvironmentVariable = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        //var isDevelopment = devEnvironmentVariable?.ToLower() == "development";

        var builder = Support.Utility.BuildConfiguration();
        builder.AddUserSecrets<IntegrationTestBase>();
        IConfigurationRoot config = builder.Build();
        return config;
    }

}
