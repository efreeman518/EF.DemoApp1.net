using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace Infrastructure.JobsApi;

public class JobsService(ILogger<JobsService> logger, IOptions<JobsServiceSettings> settings, IFusionCacheProvider cacheProvider, HttpClient httpClient) : IJobsService
{
    private const string CACHEKEY_LOOKUPS = "Lookups";

    private readonly IFusionCache _cache = cacheProvider.GetCache("IntegrationTest.DefaultCache");

    //lookups - expertises, professions, states, locationAliases, cities, 
    //https://api.ayahealthcare.com/AyaHealthCareWeb/job/joblookups
    private async Task<Lookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        var lookups = await _cache.GetOrSetAsync(CACHEKEY_LOOKUPS, async _ =>
        {
            var url = "job/joblookups";
            logger.LogInformation("GetLookupsAsync: {Url}", url);
            (HttpResponseMessage _, Result<Lookups?> result) = await httpClient.HttpRequestAndResponseResultAsync<Lookups?>(HttpMethod.Get, url, cancellationToken: cancellationToken);
            return result.Match(
                Succ: response => response ?? throw new InvalidDataException("Lookup endpoint returned null."),
                Fail: err => throw err);
        }, token: cancellationToken);

        return lookups;
    }

    public async Task<IReadOnlyList<Expertise>> GetExpertiseList(CancellationToken cancellationToken = default)
    {
        return (await GetLookupsAsync(cancellationToken)).Expertises;
    }

    //lookups - professions with types
    //https://api.ayahealthcare.com/AyaHealthCareWeb/job/ProfessionsWithTypes
    //public async Task<Result<ProfessionWithType[]?>> GetLookupProfWithTypeAsync()
    //{
    //    var url = "job/ProfessionsWithTypes";
    //    logger.LogInformation("GetLookupProfWithTypeAsync: {Url}", url);

    //    (HttpResponseMessage _, Result<ProfessionWithType[]?> result) = await httpClient.HttpRequestAndResponseResultAsync<ProfessionWithType[]?>(HttpMethod.Get, url, null);
    //    return result;
    //}

    //search 
    //https://api.ayahealthcare.com/AyaHealthcareWeb/job/search?professionCode=1&expertiseCodes=22&stateCodes=5&limit=30&includeRelatedSpecialties=false&useCityLatLong=true&offset=0
    //https://api.ayahealthcare.com/AyaHealthCareWeb/Job/Search?LocationLat=34&LocationLong=-118&Radius=50&ExpertiseCode=22
    public async Task<IReadOnlyList<Job>> SearchJobsAsync(List<int> expertiseCodes, decimal latitude, decimal longitude, int radiusMiles)
    {
        var joinExpertises = string.Join("&expertiseCodes=", expertiseCodes);
        var url = $"job/search?LocationLat={latitude}&LocationLong={longitude}&Radius={radiusMiles}&expertiseCodes={joinExpertises}";
        logger.LogInformation("job/GetJobSearchResultAsync: {Url}", url);

        (HttpResponseMessage _, Result<JobSearchResult?> result) = await httpClient.HttpRequestAndResponseResultAsync<JobSearchResult>(HttpMethod.Get, url, null);
        var jobResult = result.Match(
               Succ: response => response ?? throw new InvalidDataException($"Endpoint returned null: {url}"),
               Fail: err => throw err);
        return jobResult.Items;
    }

    //get job details
    //https://api-int.ayahealthcare.com/AyaHealthCareWeb/Job/2288040
    //public async Task<Result<Job?>> GetJob(string jobId)
    //{
    //    var url = "job/{jobId}";
    //    logger.LogInformation("GetJob: {Url}", url);

    //    (HttpResponseMessage _, Result<Job?> result) = await httpClient.HttpRequestAndResponseResultAsync<Job?>(HttpMethod.Get, url, null);
    //    return result;
    //}

}
