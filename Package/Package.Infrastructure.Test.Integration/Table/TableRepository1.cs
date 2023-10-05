using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Table;

namespace Package.Infrastructure.Test.Integration.Table;

/// <summary>
/// Implementation for each TableServiceClientName
/// </summary>
public class TableRepository1(ILogger<TableRepository1> logger, IOptions<TableRepositorySettings1> settings, 
    IAzureClientFactory<TableServiceClient> clientFactory) : TableRepositoryBase(logger, settings, clientFactory), ITableRepository1
{
}
