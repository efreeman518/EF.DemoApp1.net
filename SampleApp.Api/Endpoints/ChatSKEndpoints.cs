using Application.Contracts.Model;
using Application.Services.JobSK;
using Package.Infrastructure.AspNetCore;

namespace SampleApp.Api.Endpoints;

public static class ChatSKEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    public static void MapChatSKEndpoints(this IEndpointRouteBuilder group, bool problemDetailsIncludeStackTrace)
    {
        _problemDetailsIncludeStackTrace = problemDetailsIncludeStackTrace;

        //auth, version, output cache, etc. can be applied to specific enpoints if needed
        group.MapPost("/", AppendMessage)
            .Produces<ChatResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Submit a chat message and expect a response.");
    }

    private static async Task<IResult> AppendMessage(HttpContext httpContext, IJobSearchOrchestrator chatService1, ChatRequest request)
    {
        var result = await chatService1.ChatCompletionAsync(request);
        return result.Match<IResult>(
            dto => TypedResults.Ok(dto),
            err => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponse(message: err.Message, exception: err, traceId: httpContext.TraceIdentifier, includeStackTrace: _problemDetailsIncludeStackTrace))
        );
    }

}
