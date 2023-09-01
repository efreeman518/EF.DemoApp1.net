using Application.Contracts.Model;
using AutoMapper;
using Domain.Model;

namespace SampleApp.Bootstrapper.Automapper;

public class MappingProfile : Profile
{
    //domain <-> application
    public MappingProfile()
    {
        CreateMap<TodoItem, TodoItemDto>()
           .ReverseMap();

        CreateMap<SystemSetting, SystemSettingDto>()
           .ReverseMap();
    }
}
