namespace Package.Infrastructure.AspNetCore.Chaos;

public class ChaosManagerSettings
{
    public static string ConfigSectionName => "ChaosManagerSettings";
    public bool Enabled { get; set; } = false;
    public string EnableFromQueryStringKey { get; set; } = "chaos";
    public double InjectionRate { get; set; } = 0.0;
}