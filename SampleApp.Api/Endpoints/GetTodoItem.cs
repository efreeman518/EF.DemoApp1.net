using Application.Contracts.Model;
using Application.Contracts.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace SampleApp.Api.Endpoints;

public class GetTodoItem(ITodoService todoService) : Endpoint<Guid, Results<Ok<TodoItemDto>, NotFound, ProblemDetails>>
{
    public override void Configure()
    {
        Get("/api/todoitem/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Guid req, CancellationToken ct)
    {
        var option = await todoService.GetItemAsync(req, ct);
        var result = option.Match<IResult>(
            Some: dto => TypedResults.Ok(dto),
            None: () => TypedResults.NotFound(req)
        );
        await SendResultAsync(result);
    }
}
