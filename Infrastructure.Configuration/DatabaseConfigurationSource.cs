using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Configuration;

// Define a class that implements the IConfigurationSource interface
public class DatabaseConfigurationSource : IConfigurationSource
{
    private readonly Action<DbContextOptionsBuilder> _optionsAction;

    public DatabaseConfigurationSource(Action<DbContextOptionsBuilder> optionsAction) => _optionsAction = optionsAction;

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new DatabaseConfigurationProvider(this, _optionsAction);
}
