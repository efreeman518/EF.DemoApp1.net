using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;

namespace Console.AI1.Demo.SKMultiAgentChat;

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class CustomTerminationStrategy(int maxIterations = 10, string terminationPhrase = "TERMINATE") : TerminationStrategy
{
    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
    {
        //check history for termination phrase
        if (history.LastOrDefault()?.Content?.Contains(terminationPhrase) ?? false)
        {
            return Task.FromResult(true);
        }

        if (history.Count >= maxIterations)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}

#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

