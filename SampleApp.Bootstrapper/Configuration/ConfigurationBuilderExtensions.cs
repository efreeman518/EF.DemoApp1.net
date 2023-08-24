using Microsoft.Extensions.Configuration;

namespace SampleApp.Bootstrapper.Configuration;
public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddEntityConfiguration(this IConfigurationBuilder builder)
    {
        var config = builder.Build(); //temp config
        return builder.Add(new EntityConfigurationSource(config.GetConnectionString("TodoDbContextQuery")));
    }
}
