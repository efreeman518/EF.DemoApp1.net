using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Configuration;

// Define a class that implements the IConfigurationSource interface
public class DatabaseConfigurationSource(Action<DbContextOptionsBuilder> optionsAction, IDatabaseConfigurationRefresher? dbConfigRefresher) : IConfigurationSource
{
    internal IDatabaseConfigurationRefresher? DbConfigRefresher { get; set; } = dbConfigRefresher;

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new DatabaseConfigurationProvider(this, optionsAction);
}
