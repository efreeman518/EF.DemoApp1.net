using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Package.Infrastructure.AspNetCore;
using Package.Infrastructure.AspNetCore.Filters;
using AppConstants = Application.Contracts.Constants.Constants;

namespace SampleApp.Api.Endpoints;

public static class ExternalEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    public static void MapExternalEndpoints(this IEndpointRouteBuilder group, bool problemDetailsIncludeStackTrace)
    {
        _problemDetailsIncludeStackTrace = problemDetailsIncludeStackTrace;

        //openapidocs replace swagger

        group.MapGet("/", GetPage1).MapToApiVersion(1.0)
            .Produces<List<TodoItemDto>>().ProducesProblem(500);
        group.MapGet("/", GetPage1_1).MapToApiVersion(1.1)
            .Produces<List<TodoItemDto>>().ProducesProblem(500);
        group.MapGet("/{id:guid}", GetById)
            .Produces<TodoItemDto>().ProducesProblem(404).ProducesProblem(500);
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<TodoItemDto>>()
            .Produces<TodoItemDto>().ProducesValidationProblem().ProducesProblem(500);
        group.MapPut("/{id:guid}", Update).AddEndpointFilter<ValidationFilter<TodoItemDto>>()
            .Produces<TodoItemDto>().ProducesValidationProblem().ProducesProblem(500);
        group.MapDelete("/{id:guid}", Delete)
            .Produces(204).ProducesValidationProblem().ProducesProblem(500);
        group.MapGet("/getuser", GetUser)
            .Produces<object?>().ProducesProblem(500);
        group.MapGet("/getuserclaims", GetUserClaims)
            .Produces<object?>().ProducesProblem(500);
        group.MapGet("/getauthheader", GetAuthHeader)
            .Produces<object?>().ProducesProblem(500);
    }

    private static async Task<IResult> GetPage1(ISampleApiRestClient apiClient, int pageSize = 10, int pageIndex = 1)
    {
        var items = await apiClient.GetPageAsync(pageSize, pageIndex);
        return TypedResults.Ok(items);
    }

    private static async Task<IResult> GetPage1_1(ISampleApiRestClient apiClient, int pageSize = 20, int pageIndex = 1)
    {
        var items = await apiClient.GetPageAsync(pageSize, pageIndex);
        return TypedResults.Ok(items);
    }

    private static async Task<IResult> GetById(ISampleApiRestClient apiClient, Guid id)
    {
        var todoItem = await apiClient.GetItemAsync(id);
        return (todoItem != null)
           ? TypedResults.Ok(todoItem)
           : ProblemDetailsHelper.BuildProblemDetailsResponse(message: $"Id '{id}' not found.", statusCodeOverride: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Create(HttpContext httpContext, ISampleApiRestClient apiClient, TodoItemDto todoItemDto)
    {
        var result = await apiClient.SaveItemAsync(todoItemDto);
        return result.Match(
            dto => TypedResults.Created(httpContext.Request.Path, dto),
            err => ProblemDetailsHelper.BuildProblemDetailsResponse(exception: err, traceId: httpContext.TraceIdentifier, includeStackTrace: _problemDetailsIncludeStackTrace)
            );
    }

    private static async Task<IResult> Update(HttpContext httpContext, ISampleApiRestClient apiClient, Guid id, TodoItemDto todoItem)
    {
        //validator?
        if (todoItem.Id != null && todoItem.Id != id)
        {
            return ProblemDetailsHelper.BuildProblemDetailsResponse(statusCodeOverride: StatusCodes.Status400BadRequest, message: $"{AppConstants.ERROR_URL_BODY_ID_MISMATCH}: {id} <> {todoItem.Id}");
        }

        var result = await apiClient.SaveItemAsync(todoItem);
        return result.Match(
            dto => dto is null ? Results.NotFound(id) : TypedResults.Ok(dto),
            err => ProblemDetailsHelper.BuildProblemDetailsResponse(exception: err, traceId: httpContext.TraceIdentifier, includeStackTrace: _problemDetailsIncludeStackTrace));
    }

    private static async Task<IResult> Delete(ISampleApiRestClient apiClient, Guid id)
    {
        await apiClient.DeleteItemAsync(id);
        return Results.NoContent();
    }

    private static async Task<IResult> GetUser(ISampleApiRestClient apiClient)
    {
        return TypedResults.Ok(await apiClient.GetUserAsync());
    }

    private static async Task<IResult> GetUserClaims(ISampleApiRestClient apiClient)
    {
        return TypedResults.Ok(await apiClient.GetUserClaimsAsync());
    }

    private static async Task<IResult> GetAuthHeader(ISampleApiRestClient apiClient)
    {
        return TypedResults.Ok(await apiClient.GetAuthHeaderAsync());
    }
}
