using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Application.Services.JobSK.Plugins;

public class MemorySearchPlugin(MemoryServerless memory)
{
    [KernelFunction("QueryLookupData")]
    [Description("Search expertise lookup data to find expertise match(es) based on user input.")]
    public async Task<string> QueryMemoryAsync(string query)
    {
        // Query Kernel Memory
        var memoryResult = await memory.SearchAsync(query, limit: 3);
        var relevantContent = string.Join("\n",memoryResult.Results.SelectMany(x => x.Partitions.Select(p => p.Text)));
        return relevantContent;
    }
}
