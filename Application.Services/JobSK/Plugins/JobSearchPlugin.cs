using Infrastructure.JobsApi;

namespace Application.Services.JobSK.Plugins;
public class JobSearchPlugin(IJobsApiService jobsService)
{
    private readonly IJobsApiService _jobsService = jobsService;
    public async Task<JobSearchResponse> SearchJobs(JobSearchRequest request)
    {
        return await _jobsService.SearchJobsAsync(request);
    }
}
