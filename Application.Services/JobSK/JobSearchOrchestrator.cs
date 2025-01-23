using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Application.Services.JobSK;

public class JobSearchOrchestrator(ILogger<JobSearchOrchestrator> logger, IOptions<JobSearchOrchestratorSettings> settings, [FromKeyedServices("JobSearchKernel")] Kernel kernel)
    : IJobSearchOrchestrator
{
    private readonly IChatCompletionService chat  = kernel.GetRequiredService<IChatCompletionService>();

    public async Task<Result<ChatResponse>> ChatCompletionAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var chatId = request.ChatId ?? Guid.CreateVersion7();

        var result = await kernel.InvokeAsync("ConversationSummaryPlugin", "GetConversationActionItems", new() { { "input", request.Message } }, cancellationToken);

        return new ChatResponse(chatId, result.ToString());






        //get ChatHistory from cache otherwise create a new one

        ChatHistory? chatHistory = request.ChatId == null
            ? CreateNewChatHistory()
            : null; // (await cache.TryGetAsync<ChatHistory>(request.ChatId.ToString()!, token: cancellationToken)).GetValueOrDefault();
        chatHistory ??= CreateNewChatHistory();
        chatHistory.AddUserMessage(request.Message);
        try
        {
            var response = await chat.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            return new ChatResponse(chatId, string.Join(";", response.Items.Select(i => i.ToString()))); //??
        }
        catch (Exception ex)
        {
            return new Result<ChatResponse>(ex);
        }
    }

    private static ChatHistory CreateNewChatHistory()
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(InitialSystemMessage());
        return chatHistory;
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
