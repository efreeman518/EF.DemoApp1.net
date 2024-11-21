using Application.Contracts.Model;
using Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Support;

namespace Test.Integration.JobsChat;

[Ignore("AzureOpenAI deployment required - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md")]

[TestClass]
public class AzureOpenAIChatServiceJobTests : IntegrationTestBase
{
    private readonly IJobChatOrchestrator _jobChat;

    public AzureOpenAIChatServiceJobTests()
    {
        ConfigureServices("AzureOpenAIChatServiceJobTests");
        //var chatService = Services.GetRequiredService<IJobChatService>();
        //var jobsService = Services.GetRequiredService<IJobsApiService>();
        // _jobChat = new JobSearchChatOrchestrator(chatService, jobsService); //, cache);
        _jobChat = ServiceScope.ServiceProvider.GetRequiredService<IJobChatOrchestrator>();
    }

    [TestMethod]
    public async Task JobSearchChat_pass()
    {
        var request = new ChatRequest { ChatId = null, Message = "memphis, 20 miles, er" };
        //retrieve response chat id & message
        ChatResponse? response = null;
        var result = await _jobChat.ChatCompletionAsync(request);
        _ = result.Match(
            dto => response = dto,
            err => throw err
            );

        Assert.IsNotNull(response?.ChatId != Guid.Empty);
        Assert.IsNotNull(response?.Message);

        //continue the chat
        request = new ChatRequest { ChatId = response?.ChatId, Message = "san diego" };
        result = await _jobChat.ChatCompletionAsync(request);
        _ = result.Match(
            dto => response = dto,
            err => throw err
            );

        Assert.IsNotNull(response?.ChatId != Guid.Empty);
        Assert.IsNotNull(response?.Message);
    }
}

//public class JobSearchChatOrchestrator(IChatService chatService, IJobsApiService jobsService) //, IFusionCacheProvider cacheProvider)
//{
//    private async Task<IReadOnlyList<string>> FindMatchingValidExpertises(string input)
//    {
//        return await jobsService.FindExpertiseMatchesAsync(input, 10);
//    }

//    private async Task<IEnumerable<Job>> SearchJobs(List<string> expertises, decimal latitude, decimal longitude, int radiusMiles)
//    {
//        return await jobsService.SearchJobsAsync(expertises, latitude, longitude, radiusMiles);
//    }

//    /// <summary>
//    /// This is a ChatTool that wires up the data retrieval function to be used in a chat.
//    /// Description only since the function does not take any parameters since the target GetCurrentLocation in theory uses the device's loaction
//    /// </summary>
//    /// arrays - https://community.openai.com/t/function-call-is-invalid-please-help/266803/6
//    //private readonly ChatTool getValidExpertises = ChatTool.CreateFunctionTool(
//    //    functionName: nameof(GetValidExpertises),
//    //    functionDescription: "Get the list of valid expertises."
//    //);
//    private readonly ChatTool findMatchingValidExpertises = ChatTool.CreateFunctionTool(
//        functionName: nameof(FindMatchingValidExpertises),
//        functionDescription: "Find closest matching valid expertises.",
//        functionParameters: BinaryData.FromBytes("""
//        {
//            "type": "object",
//            "additionalProperties": false,
//            "properties": {
//                "input": {
//                    "type": "string",
//                    "description": "The expertise entered by the user."
//                }
//            },
//            "required":["input"]
//        }
//        """u8.ToArray()),
//        functionSchemaIsStrict: true
//    );

//    private readonly ChatTool searchJobs = ChatTool.CreateFunctionTool(
//        functionName: nameof(SearchJobs),
//        functionDescription: "Search for jobs based on valid expertises and location (using latitude, longitude, and radius).",
//        functionParameters: BinaryData.FromBytes("""
//        {
//            "type": "object",
//            "additionalProperties": false,
//            "properties": {
//                "expertises": {
//                    "type": "array",
//                    "description": "The list of valid expertises.",
//                    "items": {
//                      "type": "string"
//                    }
//                },
//                "latitude": {
//                    "type": "number",
//                    "description": "The latitude of the location entered by the user."
//                },
//                "longitude": {
//                    "type": "number",
//                    "description": "The longitude of the location entered by the user."
//                },
//                "radius": {
//                    "type": "number",
//                    "description": "The radius in miles from the location entered by the user."
//                }
//            },
//            "required":["expertises", "latitude", "longitude", "radius"]
//        }
//        """u8.ToArray()),
//        functionSchemaIsStrict: true
//    );

//    //Once the location, distance, and expertises are defined, you will give a concise summarization, and ask the user to confirm or change any details.
//    // based on only valid expertise names, latitude, longitude, and radius.
//    //You will validate the user input against a valid list of expertise names before searching jobs. 
//    public async Task<(Guid, string)> ChatCompletionWithToolsAsync()
//    {
//        var systemPrompt = @"You are a professional assistant that helps people find the job they are looking for. 
//You ask for specific information if not provided.  
//You assist the user in determining up to 5 valid expertises, considering the user input to identify matching valid expertises.
//You collect a location, calculate the latitude and longitude, and search radius distance from that location in miles, 
//or willingness to travel anywhere. 
//Search for jobs and present the user with the search result jobs and information provided by the tool only, 
//in a concise, detailed, easily readable html table format that includes relevant details such as required certifications 
//and shift hours if applicable, and compensation range, with an 'More details and Apply' link to the specific job application on the job website
//using the format https://www.ayahealthcare.com/travel-nursing-job/{JobId} to open in a new tab.";

//        var userMessage = "memphis, 20 miles, er";
//        var messages = new List<ChatMessage>
//        {
//            new SystemChatMessage(systemPrompt),
//            new UserChatMessage(userMessage)
//        };

//        //options for the chat - identify the tools available to the model
//        ChatCompletionOptions options = new()
//        {
//            Tools = { findMatchingValidExpertises, searchJobs }
//        };

//        return await chatService.ChatCompletionAsync(null, messages, options, ToolsCallback);
//    }

//    private async Task ToolsCallback(List<ChatMessage> messages, IReadOnlyList<ChatToolCall> toolCalls)
//    {
//        // Then, add a new tool message for each tool call that is resolved.
//        // Should be processed in parallel if possible.
//        foreach (ChatToolCall toolCall in toolCalls)
//        {
//            switch (toolCall.FunctionName)
//            {
//                case nameof(FindMatchingValidExpertises):
//                    {
//                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
//                        bool hasParamInput = argumentsJson.RootElement.TryGetProperty("input", out JsonElement elInput);
//                        var toolResult = await FindMatchingValidExpertises(elInput.GetString()!);
//                        var toolResultMessage = string.Join(", ", toolResult);
//                        messages.Add(new ToolChatMessage(toolCall.Id, toolResultMessage));
//                        break;
//                    }

//                case nameof(SearchJobs):
//                    {
//                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
//                        bool hasParamExpertises = argumentsJson.RootElement.TryGetProperty("expertises", out JsonElement elExpertises);
//                        var paramExpertises = elExpertises.EnumerateArray().Select(e => e.GetString()!).ToList();
//                        bool hasParamLatitude = argumentsJson.RootElement.TryGetProperty("latitude", out JsonElement elLatitude);
//                        var paramLatitude = elLatitude.GetDecimal()!;
//                        bool hasParamLongitude = argumentsJson.RootElement.TryGetProperty("longitude", out JsonElement elLongitude);
//                        var paramLongitude = elLongitude.GetDecimal()!;
//                        bool hasParamRadius = argumentsJson.RootElement.TryGetProperty("radius", out JsonElement elRadius);
//                        var paramRadius = elRadius.GetInt32()!;
//                        var toolResult = await SearchJobs(paramExpertises, paramLatitude, paramLongitude, paramRadius);
//                        var toolResultMessage = string.Join(", ", toolResult);
//                        messages.Add(new ToolChatMessage(toolCall.Id, toolResultMessage));
//                        break;
//                    }

//                default:
//                    {
//                        // Handle other unexpected calls.
//                        throw new NotImplementedException();
//                    }
//            }
//        }
//    }
//}
