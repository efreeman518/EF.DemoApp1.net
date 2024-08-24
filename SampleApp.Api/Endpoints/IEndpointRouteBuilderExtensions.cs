using Asp.Versioning.Builder;

namespace SampleApp.Api.Endpoints;

public static class IEndpointRouteBuilderExtensions
{
    public static void MapEndpoints(this IEndpointRouteBuilder group, string routePrefix, Action mapEndpointAction,
        ApiVersionSet? apiVersionSet = null, string[]? authPolicies = null)
    {
        var builder = group.MapGroup(routePrefix);
        if (apiVersionSet != null)
        {
            builder = builder.WithApiVersionSet(apiVersionSet); //.RequireAuthorization("policy1", "policy2");
        }
        mapEndpointAction();

    }
}
