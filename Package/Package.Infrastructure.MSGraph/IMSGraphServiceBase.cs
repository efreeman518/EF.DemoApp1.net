using Microsoft.Graph.Models;
using Package.Infrastructure.MSGraph.Models;

namespace Package.Infrastructure.MSGraph;

public interface IMSGraphServiceBase
{
    Task<User?> GetUserAsync(string userId, List<string>? select = null, List<string>? expand = null);
    Task<string?> CreateUserAsync(GraphUserRequest request);
    Task UpdateUserAsync(GraphUserRequest request);
    Task ChangeUserIdentityAsync(string userId, string newEmail);
    Task DeleteUserAsync(string userId);
}
