namespace Package.Infrastructure.MSGraph.Models;
public record UpsertUserRequest(string? id, bool AccountEnabled, string DisplayName, string Email, bool forceChangePasswordNextSignIn, string? Password = null, Dictionary<string, object>? AdditionalData = null);
