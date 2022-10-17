using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace SampleApp.Bootstrapper.Automapper;

public static class ConfigureAutomapper
{
    public static void Configure(IServiceCollection services)
    {
        services.AddSingleton(CreateMapper());
    }

    public static IMapper CreateMapper()
    {
        var mc = new MapperConfiguration(mc =>
        {
                //no mapping audit properties
                mc.AddGlobalIgnore("Created");
            mc.AddGlobalIgnore("Updated");

            mc.AddProfile(new MappingProfile());
        });
        IMapper mapper = mc.CreateMapper();
        mapper.ConfigurationProvider.AssertConfigurationIsValid(); //ensure valid mapping
        return mapper;
    }
}
