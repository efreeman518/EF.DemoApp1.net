using LanguageExt.Common;

namespace Infrastructure.JobsApi;

public interface IJobsService
{
    Task<IReadOnlyList<string>> FindTopExpertiseMatches(string target, int maxCount, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> SearchJobsAsync(List<string> expertises, decimal latitude, decimal longitude, int radiusMiles, int pageSize = 3);

    //Task<Lookups> GetLookupsAsync(CancellationToken cancellationToken = default);
    //Task<Result<ProfessionWithType[]?>> GetLookupProfWithTypeAsync();
    //Task<Result<Job?>> GetJob(string jobId);
}
