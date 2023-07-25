using Package.Infrastructure.Table;

namespace Package.Infrastructure.Test.Integration.Table;

/// <summary>
/// Implementation for a TableServiceClient
/// </summary>
public class TableRepositorySettings1 : TableRepositorySettingsBase
{
    public static string ConfigSectionName => "TableServiceClient1";

    public TableRepositorySettings1() : base()
    {

    }
}
