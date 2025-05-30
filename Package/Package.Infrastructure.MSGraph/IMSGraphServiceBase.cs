using Package.Infrastructure.MSGraph.Models;

namespace Package.Infrastructure.MSGraph;

public interface IMSGraphServiceBase
{
    Task<string?> CreateUserAsync(CreateUserRequest request);
}
