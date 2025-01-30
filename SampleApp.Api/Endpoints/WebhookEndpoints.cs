using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Package.Infrastructure.BlandAI;
using System.Text.Json;

namespace SampleApp.Api.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder group)
    {
        //auth, version, aoutput cache, etc. can be applied to specific enpoints if needed
        group.MapGet("/", GetPage)
            .Produces<List<string>>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Get a list of strings");
        group.MapGet("/{id:guid}", GetById)
            .Produces<string>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Get a item by id");
        group.MapPost("/", Post)
            .Produces<string>(StatusCodes.Status201Created).ProducesValidationProblem().ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Create item");
        group.MapPut("/{id:guid}", Put)
            .Produces<string>(StatusCodes.Status200OK).ProducesValidationProblem().ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Update item");
        group.MapDelete("/{id:guid}", Delete)
            .Produces(StatusCodes.Status204NoContent).ProducesValidationProblem().ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Delete item");
    }

    private static async Task<IResult> GetPage()
    {
        await Task.CompletedTask;
        List<string> items = ["apple", "orange", "lemon", "strawberry"];
        return TypedResults.Ok(items);
    }

    private static async Task<IResult> GetById(string id)
    {
        await Task.CompletedTask;
        return TypedResults.Ok($"orange {id}");
    }

    private static IResult Post(HttpContext httpContext, IOptions<BlandAISettings> blandAISettings, [FromBody] JsonElement body) //someData item)
    {
        //verify webhook signature
        // Retrieve the signature from the request headers
        if (!httpContext.Request.Headers.TryGetValue("X-Webhook-Signature", out StringValues signatureHeader))
        {
            return TypedResults.BadRequest("Missing X-Webhook-Signature header.");
        }

        var signature = signatureHeader.ToString();
        // Serialize the request body to a JSON string
        string requestBody = JsonSerializer.Serialize(body);

        
        if (!Utility.VerifyWebhookSignature(blandAISettings.Value.WebhookSigningSecret, requestBody, signature))
        {
            return Results.Unauthorized();
        }

        //deserialize body into someData
        var item = JsonSerializer.Deserialize<someData>(body.GetRawText());

        return TypedResults.Created(httpContext.Request.Path, item);
    }

    private static async Task<IResult> Put(string id, string item)
    {
        await Task.CompletedTask;
        return TypedResults.Ok($"{id} {item}");
    }

    private static async Task<IResult> Delete(string id)
    {
        await Task.CompletedTask;
        return Results.Ok(id);
    }
}

public record someData(string Expertise, string Location, int Distance);
