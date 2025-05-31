namespace Package.Infrastructure.MSGraph.Models;

/// <summary>
/// Create or update a User based on id null (create) or not null (update).
/// </summary>
/// <param name="Id"></param>
/// <param name="AccountEnabled"></param>
/// <param name="DisplayName"></param>
/// <param name="Email"></param>
/// <param name="ForceChangePasswordNextSignIn"></param>
/// <param name="Password">Used only on Create; ignored on update</param>
/// <param name="AdditionalData">must conform to existing/custom defined AADB2C User Attributes</param>
public record GraphUserRequest(string? Id, bool AccountEnabled, string DisplayName, string Email, bool ForceChangePasswordNextSignIn, string? Password = null, Dictionary<string, object>? AdditionalData = null);
