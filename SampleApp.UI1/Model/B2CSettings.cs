namespace SampleApp.UI1.Model;

public class B2CSettings
{
    public string? Authority { get; set; }
    public string? ClientId { get; set; }
    public bool ValidateAuthority { get; set; }
    public string? SignUpSignInPolicyId { get; set; }
    public string? SignInPolicyId { get; set; }
    public string? PasswordResetPolicyId { get; set; }
    public string? EditProfilePolicyId { get; set; } // Add if you use it
    public string? RedirectUri { get; set; } // This will be populated if present in appsettings
}
