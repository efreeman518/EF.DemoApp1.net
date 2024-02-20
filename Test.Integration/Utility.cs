using Microsoft.Extensions.Configuration;

namespace Test.Integration;

public static class Utility
{
    public static readonly IConfigurationRoot Config = Config ?? BuildConfiguration();

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = Support.Utility.BuildConfiguration();
        var config = builder.Build();
        string env = config.GetValue<string>("ASPNETCORE_ENVIRONMENT", "Development")!;
        var isDevelopment = env?.ToLower() == "development";
        if (isDevelopment) builder.AddUserSecrets<IntegrationTestBase>();
        return builder.Build();
    }
}
