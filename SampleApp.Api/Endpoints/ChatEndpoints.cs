using Application.Contracts.Model;
using Application.Contracts.Services;
using Package.Infrastructure.AspNetCore;

namespace SampleApp.Api.Endpoints;

public static class ChatEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    public static void MapChatEndpoints(this IEndpointRouteBuilder group, bool problemDetailsIncludeStackTrace)
    {
        _problemDetailsIncludeStackTrace = problemDetailsIncludeStackTrace;

        //auth, version, aoutput cache, etc. can be applied to specific enpoints if needed
        group.MapGet("/", AppendMessage).MapToApiVersion(1.0)
            .Produces<List<TodoItemDto>>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Submit a chat message and expect a response.");
    }

    private static async Task<IResult> AppendMessage(HttpContext httpContext, IChatService chatService, string message)
    {
        var result = await chatService.SendMessageAsync(message);
        return result.Match<IResult>(
            dto => TypedResults.Created(httpContext.Request.Path, dto),
            err => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponse(exception: err, traceId: httpContext.TraceIdentifier, includeStackTrace: _problemDetailsIncludeStackTrace))
        );
    }

}
