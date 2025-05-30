namespace Package.Infrastructure.MSGraph;

public class MSGraphServiceSettingsBase
{
    public string ExtensionAppObjectId { get; set; } = string.Empty; // e.g. "b2c_extensions_app_objectid" (no dashes)
    public string IdentityIssuer { get; set; } = string.Empty; // e.g. "contosob2c.onmicrosoft.com" (no dashes)
}
