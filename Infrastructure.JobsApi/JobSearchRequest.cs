namespace Infrastructure.JobsApi;
public record JobSearchRequest (List<int> ExpertiseCodes, decimal Latitude, decimal Longitude, int RadiusMiles, int PageSize = 10);