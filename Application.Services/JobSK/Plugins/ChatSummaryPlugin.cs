using Azure.AI.OpenAI;
using Microsoft.Extensions.Azure;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Text;

namespace Application.Services.JobSK.Plugins;

public class ChatSummaryPlugin(IAzureClientFactory<AzureOpenAIClient> clientFactory)
{
    private readonly AzureOpenAIClient aoaiClient = clientFactory.CreateClient("AzureOpenAI");
    //private string? _summary = "";

    //[KernelFunction]
    //public string? GetSummary() => _summary;

    [KernelFunction("summarize")]
    [Description("Summarize the chat history.")]
    [return: Description("The summary of the chat history in a few sentences.")]
    public async Task<string?> Summarize(ChatHistory chatHistory)
    {
        // Use the kernel to generate an updated summary
        var kernel = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion("gpt-4o-mini", aoaiClient).Build();

        //_summary = await kernel.InvokePromptAsync<string>(
        //    $"""
        //    Update this summary with the new conversation. Keep it under 100 words and keep the most recent job search criteria including most recent expertises, location, and distance if specified.
        //    Previous: {_summary}
        //    New Input: {input}
        //    New Response: {response}
        //    Updated Summary: 
        //    """);

        //var summary = await kernel.InvokeAsync("ChatSummary", "UpdateSummary", new()
        //{
        //    ["input"] = chatHistory.ToString(),
        //    ["response"] = response
        //});

        StringBuilder sb = new StringBuilder();

        foreach (var message in chatHistory)
        {
            sb.AppendLine($"{message.Role}: {message.Content}");
        }

        //create prompt to summarize the chat history
        var prompt = $"""
            Summarize the following chat history in a few sentences. Keep it under 100 words and keep the most recent job search criteria including most recent expertises, location, and distance if specified.
            Chat history:
            {sb}
            """;
        var summary = await kernel.InvokePromptAsync<string>(prompt);

        return summary;
    }
}
