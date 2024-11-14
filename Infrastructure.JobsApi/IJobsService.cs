using LanguageExt.Common;

namespace Infrastructure.JobsApi;

public interface IJobsService
{
    Task<IReadOnlyList<Expertise>> GetExpertiseList(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Job>> SearchJobsAsync(List<int> expertiseCodes, decimal latitude, decimal longitude, int radiusMiles);

    //Task<Lookups> GetLookupsAsync(CancellationToken cancellationToken = default);
    //Task<Result<ProfessionWithType[]?>> GetLookupProfWithTypeAsync();
    //Task<Result<Job?>> GetJob(string jobId);
}
