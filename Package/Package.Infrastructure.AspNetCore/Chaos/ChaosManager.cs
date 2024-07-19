using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Polly;
using System.Net;

namespace Package.Infrastructure.AspNetCore.Chaos;

/// <summary>
/// https://www.pollydocs.org/chaos/
/// https://devblogs.microsoft.com/dotnet/resilience-and-chaos-engineering/
/// https://www.pollydocs.org/chaos/
/// </summary>
/// <param name="contextAccessor"></param>
/// <param name="settings"></param>
public class ChaosManager(IHttpContextAccessor contextAccessor, IOptions<ChaosManagerSettings> settings) : IChaosManager
{
    public ValueTask<bool> IsChaosEnabledAsync(ResilienceContext context)
    {
        if (contextAccessor.HttpContext is { } httpContext &&
            httpContext.Request.Query.TryGetValue(settings.Value.EnableFromQueryStringKey, out var _))
        {
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    public ValueTask<double> GetInjectionRateAsync(ResilienceContext context)
    {
        return ValueTask.FromResult(settings.Value.InjectionRate);
    }

    public int LatencySeconds() => settings.Value.LatencySeconds;
    public Exception FaultException() => settings.Value.FaultException;
    public HttpStatusCode OutcomHttpStatusCode() => settings.Value.OutcomHttpStatusCode;
}
