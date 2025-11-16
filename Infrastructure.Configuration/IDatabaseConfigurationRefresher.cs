using Microsoft.Extensions.Primitives;

namespace Infrastructure.Configuration;

public interface IDatabaseConfigurationRefresher : IDisposable
{
    IChangeToken StartWatch();
}

