namespace Infrastructure.JobsApi;

public interface IJobsApiService
{
    Task<Lookups> GetLookupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> FindExpertiseMatchesAsync(string target, int maxCount, CancellationToken cancellationToken = default);
    Task<JobSearchResponse> SearchJobsAsync(JobSearchRequest request, CancellationToken cancellationToken = default);
}
