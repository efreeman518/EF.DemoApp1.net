namespace Infrastructure.JobsApi;

public interface IJobsApiService
{
    Task<IReadOnlyList<int>> FindExpertiseMatchesAsync(string target, int maxCount, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> SearchJobsAsync(List<int> expertiseCodes, decimal latitude, decimal longitude, int radiusMiles, int pageSize = 10);
}
