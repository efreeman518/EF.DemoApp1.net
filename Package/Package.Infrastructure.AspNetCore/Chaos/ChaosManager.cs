using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Polly;

namespace Package.Infrastructure.AspNetCore.Chaos;

/// <summary>
/// https://www.pollydocs.org/chaos/
/// https://medium.com/@tauraigombera/chaos-engineering-with-net-e3a194426940
/// </summary>
/// <param name="contextAccessor"></param>
/// <param name="settings"></param>
public class ChaosManager(IHttpContextAccessor contextAccessor, IOptions<ChaosManagerSettings> settings) : IChaosManager
{
    public ValueTask<bool> IsChaosEnabledAsync(ResilienceContext context)
    {
        if(contextAccessor.HttpContext is { } httpContext &&
            httpContext.Request.Query.TryGetValue(settings.Value.EnableFromQueryStringKey, out var value))
        {
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    public ValueTask<double> GetInjectionRateAsync(ResilienceContext context)
    {
        return ValueTask.FromResult(settings.Value.InjectionRate);
    }
}
