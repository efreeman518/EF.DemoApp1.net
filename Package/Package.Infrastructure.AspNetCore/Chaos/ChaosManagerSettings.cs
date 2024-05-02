using System.Net;

namespace Package.Infrastructure.AspNetCore.Chaos;

public class ChaosManagerSettings
{
    public static string ConfigSectionName => "ChaosManagerSettings";
    public bool Enabled { get; set; } = false;
    public string EnableFromQueryStringKey { get; set; } = "chaos";
    public double InjectionRate { get; set; } = 0.02;
    public int LatencySeconds { get; set; } = 10;
    public Exception FaultException { get; set; } = new InvalidOperationException("Chaos strategy injection!");
    public HttpStatusCode OutcomHttpStatusCode { get; set; } = HttpStatusCode.InternalServerError;
}