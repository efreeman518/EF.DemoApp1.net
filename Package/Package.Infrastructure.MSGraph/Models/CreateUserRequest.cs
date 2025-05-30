namespace Package.Infrastructure.MSGraph.Models;
public record CreateUserRequest(bool AccountEnabled, string DisplayName, string Email, string Password, Dictionary<string, object>? AdditionalData = null);
