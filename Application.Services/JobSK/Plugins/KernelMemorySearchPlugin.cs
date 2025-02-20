using Infrastructure.JobsApi;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Package.Infrastructure.Common.Extensions;
using System.ComponentModel;

namespace Application.Services.JobSK.Plugins;

public class KernelMemorySearchPlugin(IKernelMemory memory, IJobsApiService jobsApiService)
{
    private static bool _loaded;

    [KernelFunction("QueryExpertiseLookupData")]
    [Description("Search lookup data to find closest expertise matches based on user input.")]
    public async Task<List<string>> FindExpertiseMatchesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (!_loaded)
        {
            var expertises = (await jobsApiService.GetLookupsAsync(cancellationToken)).Expertises;
            await memory.ImportTextAsync(expertises.SerializeToJson()!, index:"expertises");
            //await memory.ImportDocumentAsync(new Document("docAzure", null, [$"{AppDomain.CurrentDomain.BaseDirectory}\\JobSK\\DigitalTransformation-MSFTvisionforAIintheenterprise.pdf"]),"idxAzureDoc" );
            _loaded = true;
        }
        query = new Expertise(null, "Accting Clrk").SerializeToJson(new System.Text.Json.JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull})!;
        var memoryResult = await memory.SearchAsync(query, "expertises", limit: 3,  minRelevance: 0.58, cancellationToken: cancellationToken);
        var relevantContent = memoryResult.Results.SelectMany(x => x.Partitions.Select(p => p.Text)).ToList();
        var matches = $"{relevantContent[0]}]".DeserializeJson<List<Expertise>>();
        return relevantContent;
    }
}
