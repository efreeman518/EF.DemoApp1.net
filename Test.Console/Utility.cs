using Microsoft.Extensions.Configuration;

namespace Test.Console;
internal static class Utility
{
    public static readonly IConfigurationRoot Config = BuildConfiguration();

    private static IConfigurationRoot BuildConfiguration()
    {
        //var devEnvironmentVariable = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        //var isDevelopment = devEnvironmentVariable?.ToLower() == "development";

        var builder = Support.Utility.BuildConfiguration();
        builder.AddUserSecrets<Program>();
        IConfigurationRoot config = builder.Build();
        return config;
    }
}
