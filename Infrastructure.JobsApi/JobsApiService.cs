using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace Infrastructure.JobsApi;

public class JobsApiService(ILogger<JobsApiService> logger, IOptions<JobsApiServiceSettings> settings,
    IFusionCacheProvider cacheProvider, HttpClient httpClient) : IJobsApiService
{
    private const string CACHEKEY_LOOKUPS = "Lookups";

    private readonly IFusionCache _cache = cacheProvider.GetCache(settings.Value.CacheName);

    //lookups - expertises, professions, states, locationAliases, cities, 
    //https://api.ayahealthcare.com/AyaHealthCareWeb/job/joblookups
    public async Task<Lookups> GetLookupsAsync(CancellationToken cancellationToken = default)
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

    public async Task<IReadOnlyList<int>> FindExpertiseMatchesAsync(string target, int maxCount, CancellationToken cancellationToken = default)
    {
        var expertises = (await GetLookupsAsync(cancellationToken)).Expertises;
        var fullNameList = expertises.Select(e => e.Name!).ToList();
        var matches = target.FindTopMatches(fullNameList, maxCount, 10, false);
        //return the ids of the matches
        return [.. expertises.Where(e => matches.Contains(e.Name!)).Select(e => e.Id!.Value)];
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

    private async Task<IReadOnlyList<Expertise>> GetAllExpertiseList(CancellationToken cancellationToken = default)
    {
        return (await GetLookupsAsync(cancellationToken)).Expertises;
    }

    //search 
    //https://api.ayahealthcare.com/AyaHealthcareWeb/job/search?professionCode=1&expertiseCodes=22&stateCodes=5&limit=30&includeRelatedSpecialties=false&useCityLatLong=true&offset=0
    //https://api.ayahealthcare.com/AyaHealthCareWeb/Job/Search?LocationLat=34&LocationLong=-118&Radius=50&ExpertiseCode=22
    public async Task<JobSearchResponse> SearchJobsAsync(JobSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ExpertiseCodes.Count == 0)
        {
            return new([]);
        }
        //var expertiseCodes = (await GetAllExpertiseList()).Where(e => expertises.Contains(e.Name, StringComparer.OrdinalIgnoreCase)).Select(e => e.Id.ToString()).ToList();
        var joinExpertises = string.Join("&expertiseCodes=", request.ExpertiseCodes);
        var url = $"job/search?LocationLat={request.Latitude}&LocationLong={request.Longitude}&Radius={request.RadiusMiles}&expertiseCodes={joinExpertises}&includeRelatedSpecialties=true";
        logger.LogInformation("SearchJobsAsync: {Url}", url);

        (HttpResponseMessage _, Result<JobSearchResult?> result) = await httpClient.HttpRequestAndResponseResultAsync<JobSearchResult>(HttpMethod.Get, url, cancellationToken: cancellationToken);
        var jobResult = result.Match(
               Succ: response => response ?? throw new InvalidDataException($"Endpoint returned null: {url}"),
               Fail: err => throw err);
        return new JobSearchResponse([.. jobResult.Items.Take(request.PageSize)]);
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
