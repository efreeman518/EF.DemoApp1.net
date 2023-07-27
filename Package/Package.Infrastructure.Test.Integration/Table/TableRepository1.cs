using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Table;

namespace Package.Infrastructure.Test.Integration.Table;

/// <summary>
/// Implementation for each TableServiceClientName
/// </summary>
public class TableRepository1 : TableRepositoryBase, ITableRepository1
{
    public TableRepository1(ILogger<TableRepository1> logger, IAzureClientFactory<TableServiceClient> clientFactory, IOptions<TableRepositorySettings1> settings)
        : base(logger, clientFactory, settings.Value.TableServiceClientName)
    {
    }
}
