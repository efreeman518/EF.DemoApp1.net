using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Package.Infrastructure.AspNetCore;

public static class HealthCheckHelper
{
    static readonly JsonSerializerOptions jsOoptions = new() { WriteIndented = true };

    public static string ParseReport(HealthReport result)
    {
        string response = JsonSerializer.Serialize(result, typeof(HealthReport), jsOoptions);

        //convert all Status enums to string
        string reMatchStatus = "\"Status\": ([0-9])";
        MatchCollection matches = Regex.Matches(response, reMatchStatus);
        string status1;
        matches.ToList().ForEach(m =>
        {
            status1 = m.Groups[1].ToString();
            if (Enum.TryParse(status1, out HealthStatus status)) response = response.Replace(m.Groups[0].ToString(), $"\"Status\" : \"{status}\"");
        });
        return response;
    }

    public static Task WriteHealthReportResponse(HttpContext httpContext, HealthReport result)
    {
        string report = ParseReport(result);
        httpContext.Response.ContentType = "application/json";
        return httpContext.Response.WriteAsync(report);
    }
    public static HealthCheckOptions BuildHealthCheckOptions(string tag)
    {
        return new HealthCheckOptions()
        {
            Predicate = (check) => check.Tags.Contains(tag),
            ResponseWriter = HealthCheckHelper.WriteHealthReportResponse
        };
    }
}
