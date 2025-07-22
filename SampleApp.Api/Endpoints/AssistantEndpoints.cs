using Application.Contracts.Model;
using Application.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Package.Infrastructure.AspNetCore;

namespace SampleApp.Api.Endpoints;

public static class AssistantEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    public static void MapAssistantEndpoints(this IEndpointRouteBuilder group, bool problemDetailsIncludeStackTrace)
    {
        _problemDetailsIncludeStackTrace = problemDetailsIncludeStackTrace;

        //auth, version, output cache, etc. can be applied to specific endpoints if needed
        group.MapPost("/", AppendMessage)
            .Produces<AssistantResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Submit a chat message and expect a response.");
    }

    private static async Task<IResult> AppendMessage(HttpContext httpContext, [FromServices] IJobAssistantOrchestrator assistantService, [FromBody] AssistantRequest request)
    {
        var result = await assistantService.AssistantRunAsync(request);

        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }
        else
        {
            return TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponse(
                message: string.Join(",", result.Errors),
                exception: new Exception(string.Join(",", result.Errors)),
                traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace));
        }
    }

}
