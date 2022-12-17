using Application.Contracts.Model;
using AutoMapper;
using Domain.Model;
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
            //mc.CreateProjection<TodoItem, TodoItemDto>(); //creates duplicate profile
        });
        IMapper mapper = mc.CreateMapper();
        mapper.ConfigurationProvider.AssertConfigurationIsValid(); //ensure valid mapping
        return mapper;
    }
}
