namespace Infrastructure.JobsApi;

public interface IJobsApiService
{
    Task<IReadOnlyList<string>> FindExpertiseMatchesAsync(string target, int maxCount, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> SearchJobsAsync(List<string> expertises, decimal latitude, decimal longitude, int radiusMiles, int pageSize = 10);
}
