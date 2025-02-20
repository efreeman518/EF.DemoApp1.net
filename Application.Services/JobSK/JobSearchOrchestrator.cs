using Infrastructure.JobsApi;
using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Package.Infrastructure.Common.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace Application.Services.JobSK;

public class JobSearchOrchestrator(ILogger<JobSearchOrchestrator> logger, IOptions<JobSearchOrchestratorSettings> settings, [FromKeyedServices("JobSearchKernel")] Kernel kernel,
    IJobsApiService jobsService, IFusionCacheProvider cacheProvider) : IJobSearchOrchestrator
{
    private readonly IFusionCache cache = cacheProvider.GetCache(settings.Value.CacheName);

    public async Task<Result<ChatResponse>> ChatCompletionAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        (var chatId, var chatHistory) = await GetOrCreateChatHistoryAsync(request.ChatId, cancellationToken);
        chatHistory.AddUserMessage(request.Message);


#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        if (kernel.Plugins.FirstOrDefault(p => p.Name == "TodoItemsApi") == null)
        {
            //https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/adding-openapi-plugins?pivots=programming-language-csharp
            await kernel.ImportPluginFromOpenApiAsync("TodoItemsApi", new Uri(settings.Value.TodoOpenApiDocUrl), new OpenApiFunctionExecutionParameters(new HttpClient())
            {
                // Determines whether payload parameter names are augmented with namespaces.
                // Namespaces prevent naming conflicts by adding the parent parameter name
                // as a prefix, separated by dots
                EnablePayloadNamespacing = false
            }, cancellationToken: cancellationToken);
        }
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


        //if (kernel.Plugins.FirstOrDefault(p => p.Name == "ExpertiseMemoryPlugin") == null)
        //{
        //    // Initialize Kernel Memory
        //    var memory = new KernelMemoryBuilder()
        //        .WithAzureOpenAITextGeneration(new AzureOpenAIConfig { Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity, Endpoint = "https://ef-oai-dev-1.openai.azure.com", Deployment = "text-embedding-3-small" })
        //        .WithAzureOpenAITextEmbeddingGeneration(new AzureOpenAIConfig { Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity, Endpoint = "https://ef-oai-dev-1.openai.azure.com", Deployment = "text-embedding-3-small" })
        //        .Build<MemoryServerless>();
            //var expertises = (await jobsService.GetLookupsAsync(cancellationToken)).Expertises;
            //await memory.ImportTextAsync(expertises.SerializeToJson(), "expertises", index:"expertisecodes", cancellationToken: cancellationToken);
            //kernel.ImportPluginFromObject(new MemoryPlugin(memory), "ExpertiseMemoryPlugin"); //Microsoft.KernelMemory.SemanticKernelPlugin
        //}

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var promptSettings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(null, true)
        };
        var chatCompletionResult = await chatCompletionService.GetChatMessageContentAsync(chatHistory, promptSettings, kernel, cancellationToken);
        chatHistory.AddAssistantMessage(chatCompletionResult.Content!);

        if (chatHistory.Count > 10)
        {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var reducer = new ChatHistorySummarizationReducer(chatCompletionService, 3, 10); // Keep system message and last few user messages
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var reducedMessages = await reducer.ReduceAsync(chatHistory, cancellationToken);
            if (reducedMessages is not null)
            {
                chatHistory = [.. reducedMessages];
            }
        }

        //if (chatHistory.Count > 10)
        //{
        //    // Update chat summary
        //    var summary = await kernel.InvokeAsync("ChatSummary", "summarize", new()
        //    {
        //        ["chatHistory"] = chatHistory
        //    }, cancellationToken);

        //    chatHistory = new ChatHistory(InitialSystemPrompt);
        //    chatHistory.AddSystemMessage($"[Summary] {summary}");
        //}

        //save the chat to the cache
        await cache.SetAsync($"chat-{chatId}", chatHistory, token: cancellationToken);
        var chatResponse = new ChatResponse(chatId, chatCompletionResult.Content!);
        return chatResponse;
    }

    private async Task<(Guid, ChatHistory)> GetOrCreateChatHistoryAsync(Guid? chatId = null, CancellationToken cancellationToken = default)
    {
        ChatHistory? chatHistory = null;
        if (chatId != null)
        {
            var cacheKey = $"chat-{chatId}";
            
            chatHistory = await cache.GetOrDefaultAsync<ChatHistory>(cacheKey, token: cancellationToken);

            //may have expired, in that case restart with a new chat
            //if (chatjson != null)
            //{
            //    chatHistory = chatjson.DeserializeJson<ChatHistory>()!;
            //}
        }

        if (chatHistory == null)
        {
            //chatHistory.Add(InitialSystemMessage);

            chatHistory = [];
            chatHistory.AddSystemMessage(InitialSystemPrompt);
        }

        return (chatId ?? Guid.CreateVersion7(), chatHistory);
    }

    private static string InitialSystemPrompt =>
@"###
    You are a professional assistant that helps people find the job they are looking for, you can also manage todo items for the user (create, update, delete, search).
    Introduce yourself and your mission.
    The user must enter search criteria consisting of a list of allowed expertises and an optional location and distance, or be willing to travel anywhere. 
    ###
    Use memory to find matching allowed expertises based on the user input, and present a list of the closest matches. At least one matching allowed expertise is required to search for jobs.
    ###
    After the allowed expertise list has been identified by matching user input to memory, and optional location and distance, present a summary of search criteria
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
    You will ask the user if they would like you to create a todo item for any or all of the jobs found in the search results, 
    and create them if requested with name ""apply for [facility name]"" all lower case and max 20 characters. 
    The todo item name must be unique so if there is a duplicate name, then modify the name by appending 3 random digits to the name.
    Also set SecureDetermistic = the url link to the job application.
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
}
