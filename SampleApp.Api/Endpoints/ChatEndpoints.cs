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
        group.MapPost("/", AppendMessage)
            .Produces<List<TodoItemDto>>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Submit a chat message and expect a response.");
    }

    private static async Task<IResult> AppendMessage(HttpContext httpContext, IJobChatOrchestrator chatService, ChatRequest request)
    {
        var result = await chatService.ChatCompletionAsync(request);
        return result.Match<IResult>(
            dto => TypedResults.Ok(dto),
            err => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponse(message: err.Message, exception: err, traceId: httpContext.TraceIdentifier, includeStackTrace: _problemDetailsIncludeStackTrace))
        );
    }

}
