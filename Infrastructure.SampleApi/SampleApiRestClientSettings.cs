namespace Infrastructure.SampleApi;

public class SampleApiRestClientSettings
{
    public const string ConfigSectionName = "SampleApiRestClientSettings";
    public string BaseUrl { get; set; } = null!;

    //Without managed identity access to the AAD App Reg, use client-secret
    public Guid? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string[] Scopes { get; set; } = null!;
}