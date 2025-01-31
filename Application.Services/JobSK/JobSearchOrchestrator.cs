using Application.Services.JobSK.Plugins;
using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Package.Infrastructure.Common.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace Application.Services.JobSK;

public class JobSearchOrchestrator(ILogger<JobSearchOrchestrator> logger, IOptions<JobSearchOrchestratorSettings> settings, [FromKeyedServices("JobSearchKernel")] Kernel kernel, IFusionCacheProvider cacheProvider)
    : IJobSearchOrchestrator
{
    private readonly IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
    private readonly IFusionCache cache = cacheProvider.GetCache(settings.Value.CacheName);

    public async Task<Result<ChatResponse>> ChatCompletionAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        (var chatId, var chatHistory) = await GetOrCreateChatHistoryAsync(request.ChatId, cancellationToken);
        chatHistory.AddUserMessage(request.Message);

        //kernel.InvokeAsync
        //var functionResult = await kernel.InvokeAsync("ConversationSummaryPlugin", "GetConversationActionItems", new() { { "input", request.Message } }, cancellationToken);
        //var chatResponse = new ChatResponse(chatId, functionResult.ToString());

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        //async - register during service registration?
        await kernel.ImportPluginFromOpenApiAsync("todoitems", new Uri(settings.Value.TodoOpenApiDocUrl), new OpenApiFunctionExecutionParameters()
           {
               // Determines whether payload parameter names are augmented with namespaces.
               // Namespaces prevent naming conflicts by adding the parent parameter name
               // as a prefix, separated by dots
               EnablePayloadNamespacing = false
           }, cancellationToken: cancellationToken);
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var promptSettings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(null, true)
        };
        var chatCompletionResult = await chatCompletionService.GetChatMessageContentAsync(chatHistory, promptSettings, kernel, cancellationToken);
        chatHistory.AddSystemMessage(chatCompletionResult.Content!);

        var chatResponse = new ChatResponse(chatId, chatCompletionResult.Content!);

        kernel.Plugins.TryGetPlugin("ConversationSummaryPlugin", out var conversationSummaryPlugin);
        if (conversationSummaryPlugin != null && chatHistory.Count > 10)
        {
            FunctionResult summary = await kernel.InvokeAsync(conversationSummaryPlugin["SummarizeConversation"], new() { ["input"] = chatHistory }, cancellationToken);
            chatHistory.Clear();
            chatHistory.AddSystemMessage(InitialSystemMessage());
            chatHistory.AddSystemMessage(summary.ToString());
        }

        //save the chat to the cache
        await cache.SetAsync($"chat-{chatId}", chatHistory.SerializeToJson(), token: cancellationToken);

        return chatResponse;
    }

    private async Task<(Guid, ChatHistory)> GetOrCreateChatHistoryAsync(Guid? chatId = null, CancellationToken cancellationToken = default)
    {
        ChatHistory? chatHistory = null;
        if (chatId != null)
        {
            var cacheKey = $"chat-{chatId}";
            //may have expired, in that case restart with a new chat
            var chatjson = await cache.GetOrDefaultAsync<string>(cacheKey, token: cancellationToken);
            if (chatjson != null)
            {
                chatHistory = chatjson.DeserializeJson<ChatHistory>()!;
            }
        }

        if (chatHistory == null)
        {
            chatHistory = [];
            chatHistory.AddSystemMessage(InitialSystemMessage());
        }

        return (chatId ?? Guid.CreateVersion7(), chatHistory);
    }

    private static string InitialSystemMessage()
    {
        //Once the location, distance, and expertises are defined, you will give a concise summarization, and ask the user to confirm or change any details.
        //based on only valid expertise names, latitude, longitude, and radius.
        //You will validate the user input against a valid list of expertise names before searching jobs.
        //, considering the user input to identify matching valid expertises
        //If you are unable to find a matching allowed expertise, let the person know there is no match, and tell them a joke about the missing expertise.

        var systemPrompt = @"
###
You are a professional assistant that helps people find the job they are looking for, introduce yourself and your mission.
The user must enter search criteria consisting of a list of allowed expertises and an optional location and distance, or be willing to travel anywhere. 
###
First find matching allowed expertises based on the user input, and present a list of the closest matches. At least one matching allowed expertise is required to search for jobs.
###
After the allowed expertise list has been identified from the approved expertise function, and optional location and distance, present a summary of search criteria
in a html unordered bulletpoint list, and ask the user to confirm before searching for jobs.
###
If a location is provided, you calculate the latitude and longitude for the job search
You can perform a search with only: at least one allowed expertises (and location, if provided). If there are no allowed expertise's, 
you will reply that you are unable to search and make a joke about it.
###
Always present the user with an html search results table, containing only the jobs found in the search results.
Include up to 10 jobs and relevant details such as required certifications 
and shift hours if applicable, compensation range, with link 'More details and Apply' link to the specific job application on the job website
using the format https://www.ayahealthcare.com/travel-nursing-job/{JobId} to open in a new tab.
###
Sample confirmation list:
<ul>
    <li><strong>Expertise:</strong> ER</li>
    <li><strong>Location:</strong> San Diego</li>
    <li><strong>Distance:</strong> 20 miles</li>
</ul>
###
Sample search results table:
<table>
    <tbody><tr>
        <th>Facility Name</th>
        <th>Position</th>
        <th>Employment Type</th>
        <th>Shift Hours</th>
        <th>Compensation Range</th>
        <th>More details and Apply</th>
    </tr>
    <tr>
        <td>Sharp Memorial Hospital</td>
        <td>Registered Nurse</td>
        <td>Travel/Contract</td>
        <td>19:00 - 07:00 (3x12-Hour)</td>
        <td>$2,464 - $2,693</td>
        <td><a href=""https://www.ayahealthcare.com/travel-nursing-job/2676054"" target=""_blank"">More details and Apply</a></td>
    </tr>
</tbody></table>
";

        return systemPrompt;
    }
}
