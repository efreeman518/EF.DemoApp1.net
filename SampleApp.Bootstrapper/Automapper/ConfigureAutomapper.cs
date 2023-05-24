using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace SampleApp.Bootstrapper.Automapper;

public static class ConfigureAutomapper
{
    public static void Configure(IServiceCollection services, List<Profile>? mappingProfiles = null)
    {
        services.AddSingleton(CreateMapper(mappingProfiles));
    }

    public static IMapper CreateMapper(List<Profile>? mappingProfiles = null)
    {
        var mc = new MapperConfiguration(mc =>
        {
            //global custom mapping rules - ignore audit properties
            mc.AddGlobalIgnore("Created");
            mc.AddGlobalIgnore("Updated");

            mappingProfiles?.ForEach(p => mc.AddProfile(p));
        });
        IMapper mapper = mc.CreateMapper();
        mapper.ConfigurationProvider.AssertConfigurationIsValid(); //ensure valid mapping
        return mapper;
    }
}
