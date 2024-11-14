using Infrastructure.JobsApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenAI.Chat;
using Package.Infrastructure.AzureOpenAI;
using Package.Infrastructure.Common.Extensions;
using System.Text.Json;
using Test.Support;

namespace Test.Integration.JobsApi;

//[Ignore("AzureOpenAI deployment required - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md")]

[TestClass]
public class AzureOpenAIChatServiceJobTests : IntegrationTestBase
{
    private readonly JobSearchChatOrchestrator _jobChat;

    public AzureOpenAIChatServiceJobTests()
    {
        ConfigureServices("AzureOpenAIChatServiceJobTests");
        var chatService = Services.GetRequiredService<IChatService>();
        var jobsService = Services.GetRequiredService<IJobsService>();
        //var cache = Services.GetRequiredService<IFusionCacheProvider>();

        _jobChat = new JobSearchChatOrchestrator(chatService, jobsService); //, cache);
    }

    [TestMethod]
    public async Task JobSearchConversationWithTools_pass()
    {
        await _jobChat.ChatCompletionWithToolsAsync();
        Assert.IsTrue(true);
    }
}

public class JobSearchChatOrchestrator(IChatService chatService, IJobsService jobsService) //, IFusionCacheProvider cacheProvider)
{
    //private IFusionCache _cache = cacheProvider.GetCache("IntegrationTest.DefaultCache");

    //Tools
    private async Task<string?> GetValidExpertises()
    {
        return (await jobsService.GetExpertiseList()).SerializeToJson();
    }

    private async Task<IReadOnlyList<Job>> SearchJobs(List<int> expertiseCodes, decimal latitude, decimal longitude, int radiusMiles)
    {
        return await jobsService.SearchJobsAsync(expertiseCodes, latitude, longitude, radiusMiles);
    }

    /// <summary>
    /// This is a ChatTool that wires up the data retrieval function to be used in a chat.
    /// Description only since the function does not take any parameters since the target GetCurrentLocation in theory uses the device's loaction
    /// </summary>
    /// arrays - https://community.openai.com/t/function-call-is-invalid-please-help/266803/6
    private readonly ChatTool getValidExpertises = ChatTool.CreateFunctionTool(
        functionName: nameof(GetValidExpertises),
        functionDescription: "Get the list of valid expertises (code and name)"
    );

    private readonly ChatTool searchJobs = ChatTool.CreateFunctionTool(
        functionName: nameof(SearchJobs),
        functionDescription: "Search for jobs based on the expertise codes and location (using latitude, longitude, and radius) entered by the user.",
        functionParameters: BinaryData.FromBytes("""
        {
            "type": "object",
            "additionalProperties": false,
            "properties": {
                "expertises": {
                    "type": "array",
                    "description": "The list of expertise codes found by the expertise names entered by the user.",
                    "items": {
                      "type": "number"
                    }
                },
                "latitude": {
                    "type": "number",
                    "description": "The latitude of the location entered by the user."
                },
                "longitude": {
                    "type": "number",
                    "description": "The longitude of the location entered by the user."
                },
                "radius": {
                    "type": "number",
                    "description": "The radius in miles from the location entered by the user."
                }
            },
            "required":["expertises", "latitude", "longitude", "radius"]
        }
        """u8.ToArray()),
        functionSchemaIsStrict: true
    );

    public async Task ChatCompletionWithToolsAsync()
    {
        var systemPrompt = @"You are an AI assistant that helps people find the job they are looking for. 
You ask for specific information if not provided. You are professional, positive, and complimentary 
and mention an interesting  one-sentence fact about the location, but only after the location is determined. 
You first need to collect a location and distance from that location (or willingness to travel anywhere), 
as well as expertises they are qualified in. You will validate expertises against a known list of valid expertises. 
Once the location, distance, 
and expertises are defined, you will give a concise summarization, and ask the user to confirm or change any details. 
After confirmation, determine the location latitude and longitude for use in calling tools, then use the tools 
to search for matching jobs based on expertises, latitude, longitude, and radius
and present the user with the jobs and information provided by the tool only, 
in a concise, detailed, easily readable format that includes relevant details such as required certifications 
and shift hours if applicable, and compensation range, with an 'Apply Here' link to the specific job application on the job website.";

        var userMessage = "memphis, 20 miles, er, icu";
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage)
        };

        //options for the chat - identify the tools available to the model
        ChatCompletionOptions options = new()
        {
            Tools = { getValidExpertises, searchJobs }
        };

        await chatService.ChatCompletionWithTools(messages, options, ToolsCallback);

        //should have enough info to request confirmation & continue to search jobs
        messages.Add(new UserChatMessage("yes"));
        await chatService.ChatCompletionWithTools(messages, options, ToolsCallback);

        //should have job search results
        _ = true;

    }

    private async Task ToolsCallback(List<ChatMessage> messages, IReadOnlyList<ChatToolCall> toolCalls)
    {
        // Then, add a new tool message for each tool call that is resolved.
        // Should be processed in parallel if possible.
        foreach (ChatToolCall toolCall in toolCalls)
        {
            switch (toolCall.FunctionName)
            {
                case nameof(GetValidExpertises):
                    {
                        var toolResult = await GetValidExpertises();
                        var toolResultMessage = string.Join(", ", toolResult);
                        messages.Add(new ToolChatMessage(toolCall.Id, toolResultMessage));
                        break;
                    }

                case nameof(SearchJobs):
                    {
                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                        bool hasParamExpertises = argumentsJson.RootElement.TryGetProperty("expertises", out JsonElement elExpertises);
                        var paramExpertises = elExpertises.EnumerateArray().Select(e => e.GetInt32()!).ToList();
                        bool hasParamLatitude = argumentsJson.RootElement.TryGetProperty("latitude", out JsonElement elLatitude);
                        var paramLatitude = elExpertises.GetDecimal()!;
                        bool hasParamLongitude = argumentsJson.RootElement.TryGetProperty("longitude", out JsonElement elLongitude);
                        var paramLongitude = elExpertises.GetDecimal()!;
                        bool hasParamRadius = argumentsJson.RootElement.TryGetProperty("radius", out JsonElement elRadius);
                        var paramRadius = elExpertises.GetInt32()!;
                        var toolResult = await SearchJobs(paramExpertises, paramLatitude, paramLongitude, paramRadius);
                        var toolResultMessage = string.Join(", ", toolResult);
                        messages.Add(new ToolChatMessage(toolCall.Id, toolResultMessage));
                        break;
                    }

                default:
                    {
                        // Handle other unexpected calls.
                        throw new NotImplementedException();
                    }
            }
        }
    }
}
