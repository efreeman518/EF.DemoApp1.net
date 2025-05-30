using Microsoft.Graph.Models;
using Package.Infrastructure.MSGraph.Models;

namespace Package.Infrastructure.MSGraph;

public interface IMSGraphServiceBase
{
    Task<User?> GetUserAsync(string userId, string? select = null, string? expand = null);
    Task<string?> UpsertUserAsync(UpsertUserRequest request);
    Task DeleteUserAsync(string userId);
}
