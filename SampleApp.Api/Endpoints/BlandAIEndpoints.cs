using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Package.Infrastructure.BlandAI;
using Package.Infrastructure.BlandAI.Enums;
using Package.Infrastructure.BlandAI.Model;
using System.Text.Json;

namespace SampleApp.Api.Endpoints;

public static class BlandAIEndpoints
{
    public static void MapBlandAIEndpoints(this IEndpointRouteBuilder group)
    {
        //auth, version, output cache, etc. can be applied to specific enpoints if needed
        //bland webclient
        group.MapGet("/blandwebclientconfig", GetBlandWebClientConfig)
            .Produces<string>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Get bland webclient config");
        group.MapGet("/webhook1", Webhook1)
            .Produces<string>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Bland webhook with some search criteria.");
        group.MapGet("/sendcall", SendCall)
            .Produces<string>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Bland send call.");
        group.MapGet("/analyzecall", AnalyzeCall)
            .Produces<string>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Bland analyze call.");
        group.MapGet("/calldetails", CallDetails)
            .Produces<string>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Bland get the call details.");
        group.MapGet("/calltranscript", CorrectedTranscript)
            .Produces<string>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Bland get the call corrected transcript using the (required) recorded audio.");
        group.MapGet("/callemotion", CallEmotion)
            .Produces<string>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Bland get the call emotion using the (required) recorded audio.");

    }

    private static async Task<IResult> SendCall([FromServices] IBlandAIRestClient client, string phoneNumber, string name)
    {
        var request = new SendCallRequest
        {
            PhoneNumber = phoneNumber,

            //1)What is your profession? 2)What are your areas of expertise? 3)What is your availability by days and work hours? 
            //4)How soon can you start?, 5)What time of the day do you normally jump to warp speed?, 6)Favorite color?, 7)Gender (male or female)?, 8)Current Age? 9)Feedback on this call?

            //1)What date and time did the incident occur ? 2)Describe what happened in 1 - 2 sentences ? 3)Was anyone injured and if so, describe the injuries ?
            //4)Describe your resting pain level on a scale of 1 - 10 ? 5)Describe your pain level climbing stairs 1 - 10 ?
            //6)Are you male or female ? 7)What is your current age ? 8)What is your favorite color ? 9)Feedback on this call?

            Task = @"You are Betty, an AI assistant from {{company}} calling {{name}}, get answers to the following questions: 
                1)What is your profession? 2)What are your areas of expertise? 3)What is your availability by days and work hours? 
                4)How soon can you start?, 5)What time of the day do you normally jump to warp speed?, 6)Favorite color?, 7)Gender (male or female)?, 8)Current Age? 9)Feedback on this call
            ",
            Voice = "Maya",
            InterruptionThreshold = 125,
            RecordCallAudio = true,
            FirstSentence = "Hello {{name}}, this is the AI assistant Betty from {{company}} and I have a few questions.",
            RequestData = new Dictionary<string, string>() { { "name", name ?? "" }, { "company", "The Shizzle-mah-Dizzle Firm" }, { "officenumber", "999-999-9999" } },
            //VoicemailMessage = "Hello, this is Betty from {{company}}. I have a few questions for you, I will try calling later or you call the office at {{officenumber}}.",
            Voicemail = new VoiceMail 
            {  
                Message = "Hello, this is Betty from {{company}}. I have a few questions for you, I will try calling later or you call the office at {{officenumber}}.", 
                Action = VoicemailAction.leave_message,
                Sms = new VoicemailSms { Message = "Hi, this is Betty from {{company}}. Sorry I missed you. Please call our office at {{officenumber}}. Thanks!" } 
            },
            //AvailableTags = ["successful", "incomplete", "failed"],
            Metadata = new Dictionary<string, object>() { { "originId", "123" }, { "reasonId", "456" } }
            //https://docs.bland.ai/tutorials/custom-tools#custom-tools
            //Tools = 
            //[
            //    new() {  

            //    }
            //],
        };

        var callResult = await client.SendCallAsync(request);

        if (callResult.IsSuccess)
        {
            return TypedResults.Ok(callResult.Value);
        }
        else
        {
            throw new InvalidOperationException(string.Join(",", callResult.Errors));
        }
    }

    private static async Task<IResult> AnalyzeCall(string callId, [FromServices] IBlandAIRestClient client)
    {
        var request = new AnalyzeCallRequest
        {
            Goal = "Get the answers from the customer",
            //Questions = [["Who answered the call?", "human or voicemail"],["Date and time of the incident", "date and time in format YYYY-MM-DDTHH:mm"], ["Incident description", "string"], ["Injuries", "string"],
            //["Resting pain level", "number"], ["Climbing stairs pain level", "number"], ["Gender", "male or female or other"], ["Current Age", "number"], ["Favorite color", "string"], ["Feedback on the call", "string"]]
            Questions = [["Who answered the call?", "human or voicemail"],["Profession", "string"], ["Areas of expertise", "string"], ["Availability by days and work hours", "weekdays with time ranges"],
            ["How soon can you start?", "date"], ["Normal time of the jump to warp speed", "time"], ["Favorite color", "string"], ["Gender", "male or female or other"], ["Current Age", "number"], ["Feedback on the call", "string"]]
        };
        var result = await client.AnalyzeCallAsync(callId, request);

        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }
        else
        {
            throw new InvalidOperationException(string.Join(",", result.Errors));
        }
    }

    private static async Task<IResult> CallDetails(string callId, [FromServices] IBlandAIRestClient client)
    {
        var result = await client.CallDetailsAsync(callId);
        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }
        else
        {
            throw new InvalidOperationException(string.Join(",", result.Errors));
        }
    }

    private static async Task<IResult> CorrectedTranscript(string callId, [FromServices] IBlandAIRestClient client)
    {
        var result = await client.CorrectedTranscriptAsync(callId);
        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }
        else
        {
            throw new InvalidOperationException(string.Join(",", result.Errors));
        }
    }

    private static async Task<IResult> CallEmotion(string callId, [FromServices] IBlandAIRestClient client)
    {
        var result = await client.CallEmotionsAsync(new CallEmotionRequest { CallId = callId });
        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }
        else
        {
            throw new InvalidOperationException(string.Join(",", result.Errors));
        }
    }

    private sealed class AgentConfigResponse
    {
        public string? AgentId { get; set; }
        public string? Token { get; set; }
    }

    private static async Task<IResult> GetBlandWebClientConfig([FromServices] IBlandAIRestClient client)
    {
        var request = new AgentRequest
        {
            Prompt = InitialPrompt(),
            FirstSentence = "Hello, I am here to help you find your dream job. Please tell me your location preference and your expertise.",
            AnalysisSchema = new Dictionary<string, object>() { { "expertise", "string" }, { "location", "string" }, { "distance", "string" } },
            Webhook = "https://tdcjwf8m-44318.use.devtunnels.ms/api1/v1/blandai/webhook1"
        };
        var resultAgent = await client.CreateWebAgentAsync(request);
        if (!resultAgent.IsSuccess)
        {
            throw new InvalidOperationException(string.Join(",", resultAgent.Errors));
        }
        var resultToken = await client.AuthorizeWebAgentCallAsync(resultAgent.Value!.AgentId!, CancellationToken.None);

        if (!resultToken.IsSuccess)
        {
            throw new InvalidOperationException(string.Join(",", resultToken.Errors));
        }

        return TypedResults.Ok(new AgentConfigResponse { AgentId = resultAgent.Value.AgentId, Token = resultToken.Value!.Token });

    }

    private static IResult Webhook1(HttpContext httpContext, [FromServices] IOptions<BlandAISettings> blandAISettings, [FromBody] JsonElement body) //someData item)
    {
        //verify webhook signature
        //Retrieve the signature from the request headers
        if (!httpContext.Request.Headers.TryGetValue("X-Webhook-Signature", out StringValues signatureHeader))
        {
            return TypedResults.BadRequest("Missing X-Webhook-Signature header.");
        }

        var signature = signatureHeader.ToString();
        // Serialize the request body to a JSON string
        string requestBody = JsonSerializer.Serialize(body);

        var webhookSigningSecret = blandAISettings.Value.WebhookSigningSecret;
        if (webhookSigningSecret is not null && !Utility.VerifyWebhookSignature(webhookSigningSecret, requestBody, signature))
        {
            return Results.Unauthorized();
        }

        //deserialize body into someData
        var item = body.GetRawText();
        Console.WriteLine(item);

        return TypedResults.Ok();
    }

    private static string InitialPrompt()
    {
        //Once the location, distance, and expertises are defined, you will give a concise summarization, and ask the user to confirm or change any details.
        //based on only valid expertise names, latitude, longitude, and radius.
        //You will validate the user input against a valid list of expertise names before searching jobs.
        //, considering the user input to identify matching valid expertises
        //If you are unable to find a matching allowed expertise, let the person know there is no match, and tell them a joke about the missing expertise.

        var systemPrompt = @"
You are a professional assistant that helps people find the job they are looking for, you can also manage todo items for the user (create, update, delete, search).
The user must enter job search criteria consisting of their expertise(s) and an optional location and distance, or be willing to travel anywhere. 
";

        return systemPrompt;
    }
}
