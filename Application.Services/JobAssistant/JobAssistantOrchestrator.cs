﻿using Azure.AI.OpenAI.Assistants;
using Infrastructure.JobsApi;
using LanguageExt;
using LanguageExt.Common;
using Package.Infrastructure.Common.Extensions;
using System.Text.Json;

namespace Application.Services.JobAssistant;

public class JobAssistantOrchestrator(ILogger<JobAssistantOrchestrator> logger, IOptions<JobAssistantOrchestratorSettings> settings, IJobAssistantService assistantService, IJobsApiService jobsService)
    : ServiceBase(logger), IJobAssistantOrchestrator
{
    public async Task<Result<AssistantResponse>> AssistantRunAsync(AssistantRequest request, CancellationToken cancellationToken = default)
    {
        Assistant assistant = null!;
        List<string> fileIds = [];

        if (request.AssistantId == null)
        {
            //ensure assistant files are loaded
            fileIds = await EnsureSupportFilesUploaded(cancellationToken);

            //setup the assistant
            var aOptions = new AssistantCreationOptions(settings.Value.DeploymentName)
            {
                Name = "job-search-assistant",
                Description = "Job search expert assistant helps users find jobs based on their location and expertise.",
                Tools = { searchJobs, new RetrievalToolDefinition() }, //RetrievalToolDefinition() required for files
                Instructions = InitialSystemMessage()
            };

            assistant = await assistantService.GetOrCreateAssistantByName("job-search-assistant", aOptions, cancellationToken);

            //(request.AssistantId, request.ThreadId) = await assistantService.CreateAssistandAndThreadAsync(aOptions, cancellationToken: cancellationToken);
        }

        //ensure files are linked to the assistant
        await EnsureSupportFilesLinked(assistant.Id, fileIds, cancellationToken);

        //continue the conversation
        var crOptions = new CreateRunOptions(request.AssistantId);
        //var response = await assistantService.AddMessageAndRunThreadAsync(request.ThreadId, request.Message, crOptions, RunToolCalls, cancellationToken: cancellationToken);
       // return new AssistantResponse(request.AssistantId, request.ThreadId, response);
        return new AssistantResponse("", "", "");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>File Ids</returns>
    private async Task<List<string>> EnsureSupportFilesUploaded(CancellationToken cancellationToken = default)
    {
        List<string> fileIds = [];
        var file = await assistantService.GetFileByFilenameAsync("joblookups.json", default);
        if(file == null)
        {
            var jobLookups = await jobsService.GetLookupsAsync(cancellationToken);
            file = await assistantService.UploadFileAsync(jobLookups.SerializeToJson()!.ToStream(), OpenAIFilePurpose.Assistants, "joblookups.json", cancellationToken);
        }
        fileIds.Add(file.Id);
        return fileIds;
    }

    private async Task EnsureSupportFilesLinked(string assistantId, List<string> fileIds, CancellationToken cancellationToken = default)
    {
        var files = await assistantService.GetAssistantFilesAsync(assistantId, 100, cancellationToken: cancellationToken);
        //ensure all files are linked
        foreach (var fileId in fileIds)
        {
            if (!files.Any(f => f.Id == fileId))
            {
                await assistantService.LinkAssistantFileASync(assistantId, fileId, cancellationToken);
            }
        }
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
You are a professional job search assistant that helps people find the job they are looking for, introduce yourself and your mission.
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

    //private async Task<IReadOnlyList<int>> FindExpertiseMatchesAsync(string input)
    //{
    //    var matches = await jobsService.FindExpertiseMatchesAsync(input, settings.Value.MaxJobSearchResults);
    //    return matches;
    //}

    private async Task<IEnumerable<Job>> SearchJobsAsync(List<int> expertiseCodes, decimal latitude, decimal longitude, int radiusMiles)
    {
        return await jobsService.SearchJobsAsync(expertiseCodes, latitude, longitude, radiusMiles);
    }

    /// <summary>
    /// This is a ChatTool that wires up the data retrieval function to be used in a chat.
    /// Description only since the function does not take any parameters since the target GetCurrentLocation in theory uses the device's loaction
    /// </summary>
    /// arrays - https://community.openai.com/t/function-call-is-invalid-please-help/266803/6

    //private readonly FunctionToolDefinition findExpertiseMatches = new(
    //    name: nameof(FindExpertiseMatchesAsync),
    //    description: "Find closest matching allowed expertise codes.",
    //    parameters: BinaryData.FromBytes("""
    //    {
    //        "type": "object",
    //        "additionalProperties": false,
    //        "properties": {
    //            "input": {
    //                "type": "string",
    //                "description": "The expertise entered by the user."
    //            }
    //        },
    //        "required":["input"]
    //    }
    //    """u8.ToArray())
    //);

    private readonly FunctionToolDefinition searchJobs = new(
        name: nameof(SearchJobsAsync),
        description: "Find jobs based on the allowed expertise codes and location (using latitude, longitude, and radius).",
        parameters: BinaryData.FromBytes("""
        {
            "type": "object",
            "additionalProperties": false,
            "properties": {
                "expertises": {
                    "type": "array",
                    "description": "The list of allowed expertise codes.",
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
        """u8.ToArray())
    );

    private async Task<List<ToolOutput>> RunToolCalls(IReadOnlyList<RequiredToolCall> toolCalls)
    {
        logger.LogInformation("RunToolCalls - {ToolCallsCount}", toolCalls.Count);

        var toolOutputs = new List<ToolOutput>();

        foreach (RequiredToolCall toolCall in toolCalls)
        {
            if (toolCall is RequiredFunctionToolCall functionToolCall)
            {
                logger.LogInformation("RunToolCalls - {FunctionName}", functionToolCall.Name);
                using JsonDocument argumentsJson = JsonDocument.Parse(functionToolCall.Arguments);
                switch (functionToolCall.Name)
                {
                    //case nameof(FindExpertiseMatchesAsync):
                    //    {
                    //        _ = argumentsJson.RootElement.TryGetProperty("input", out JsonElement elInput);
                    //        var toolResult = await FindExpertiseMatchesAsync(elInput.GetString()!);
                    //        var toolResultMessage = string.Join(", ", toolResult);
                    //        toolOutputs.Add(new ToolOutput(functionToolCall, toolResultMessage));
                    //        break;
                    //    }

                    case nameof(SearchJobsAsync):
                        {
                            _ = argumentsJson.RootElement.TryGetProperty("expertises", out JsonElement elExpertises);
                            var paramExpertises = elExpertises.EnumerateArray().Select(e => e.GetInt32()!).ToList();
                            _ = argumentsJson.RootElement.TryGetProperty("latitude", out JsonElement elLatitude);
                            var paramLatitude = elLatitude.GetDecimal()!;
                            _ = argumentsJson.RootElement.TryGetProperty("longitude", out JsonElement elLongitude);
                            var paramLongitude = elLongitude.GetDecimal()!;
                            _ = argumentsJson.RootElement.TryGetProperty("radius", out JsonElement elRadius);
                            var paramRadius = elRadius.GetInt32()!;
                            var toolResult = await SearchJobsAsync(paramExpertises, paramLatitude, paramLongitude, paramRadius);
                            var toolResultMessage = string.Join(", ", toolResult);
                            toolOutputs.Add(new ToolOutput(functionToolCall, toolResultMessage));
                            break;
                        }
                    default:
                        {
                            // Handle other unexpected calls.
                            throw new NotImplementedException($"RunToolCalls - {functionToolCall.Name}");
                        }
                }
            }
        }

        return toolOutputs;
    }
}
