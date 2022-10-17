using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleApp.Bootstrapper.Automapper;

namespace Test.Benchmarks;

public static class Utility
{
    public static readonly IConfigurationRoot Config;
    public static readonly IServiceCollection Services;
    private static IServiceProvider? ServiceProvider;

    static Utility()
    {
        Config = BuildConfiguration();
        Services = new ServiceCollection();
        //bootstrapper container registrations - infrastructure, application and domain services
        new SampleApp.Bootstrapper.Startup(Config).ConfigureServices(Services);
        //configure & register Automapper, application and infrastructure mapping profiles
        ConfigureAutomapper.Configure(Services);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var devEnvironmentVariable = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var isDevelopment = devEnvironmentVariable?.ToLower() == "development";

        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json");

        if (isDevelopment) builder.AddUserSecrets<Program>();

        IConfigurationRoot config = builder.Build();

        return config;
    }

    public static IServiceProvider GetServiceProvider()
    {
        if (ServiceProvider != null) return ServiceProvider;

        //build IServiceProvider for subsequent use finding/injecting services
        ServiceProvider = Services.BuildServiceProvider(validateScopes: true);
        return ServiceProvider;
    }

    private static readonly Random random = new();
    public static string RandomString(int length)
    {
        const string chars = "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
