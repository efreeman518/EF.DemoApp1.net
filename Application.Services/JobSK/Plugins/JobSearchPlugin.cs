using Infrastructure.JobsApi;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Application.Services.JobSK.Plugins;

public class JobSearchPlugin(IJobsApiService jobsService)
{
    [KernelFunction("SearchJobs")]
    [Description("Search for jobs based on the given search request which contains the search criteria of allowed expertise codes and optional location (latitude, longitude, and radius).")]
    [return: Description("The search results containing a list of matching jobs.")]
    public async Task<JobSearchResponse> SearchJobsAsync(JobSearchRequest request, CancellationToken cancellationToken = default)
    {
        return await jobsService.SearchJobsAsync(request, cancellationToken);
    }

    [KernelFunction("FindExpertiseMatches")]
    [Description("Determine the expertise codes closely matching the user entered target expertise.")]
    [return: Description("The search results containing a list of matching expertise codes.")]
    public async Task<IReadOnlyList<int>> FindExpertiseMatchesAsync(string target, int maxCount, CancellationToken cancellationToken = default)
    {
        return await jobsService.FindExpertiseMatchesAsync(target, maxCount, cancellationToken);
    }
}
