using Application.Contracts.Model;
using AutoMapper;

namespace SampleApp.Grpc;

public class GrpcMappingProfile : Profile
{
    public GrpcMappingProfile()
    {
        //app <-> grpc proto
        CreateMap<TodoItemDto, SampleApp.Grpc.Proto.TodoItemDto>()
            //grpc .Id is a nullable string
            .ForPath(d => d.Id.Isnull, o => o.MapFrom(s => s == null))
            .ForPath(d => d.Id.Data, o => o.MapFrom(s => s != null ? s.Id : Guid.Empty))
            //grpc .Name is a nullable string
            .ForPath(d => d.Name.Isnull, o => o.MapFrom(s => s == null))
            .ForPath(d => d.Name.Data, o => o.MapFrom(s => s != null ? s.Name : default))
            .ReverseMap()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.Isnull ? null : s.Id.Data))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name.Isnull ? null : s.Name.Data))
            ;
    }
}
