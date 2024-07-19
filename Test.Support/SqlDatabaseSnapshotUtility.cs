using Microsoft.Data.SqlClient;
using System.Data;

namespace Test.Support;

/// <summary>
/// https://johnnyreilly.com/integration-tests-with-sql-server
/// </summary>
public class SqlDatabaseSnapshotUtility(string dbConnectionString)
{
    public async Task CreateSnapshotAsync(string dbName, string snapshotPath, string snapshotName, CancellationToken cancellationToken = default)
    {
        //get server data folder (works on sql server, not Azure SQL or MI)
        //select SERVERPROPERTY('InstanceDefaultDataPath')
        if (!Directory.Exists(snapshotPath))
            Directory.CreateDirectory(snapshotPath);

        var sql = $"CREATE DATABASE {snapshotName} ON (NAME=[{dbName}], FILENAME='{snapshotPath}\\{snapshotName}') AS SNAPSHOT OF [{dbName}]";
        await ExecuteSqlAgainstMasterAsync(sql, null, cancellationToken);
    }

    public async Task DeleteSnapshotAsync(string dbSnapshotName, CancellationToken cancellationToken = default)
    {
        var sql = $"DROP DATABASE {dbSnapshotName}";
        await ExecuteSqlAgainstMasterAsync(sql, null, cancellationToken);
    }

    public async Task RestoreSnapshotAsync(string dbName, string dbSnapshotName, CancellationToken cancellationToken = default)
    {
        var sql = "USE master;\r\n" +
            $"ALTER DATABASE {dbName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;\r\n" +
            $"RESTORE DATABASE {dbName}\r\n" +
            $"FROM DATABASE_SNAPSHOT = '{dbSnapshotName}';\r\n" +
            $"ALTER DATABASE {dbName} SET MULTI_USER;\r\n";

        await ExecuteSqlAgainstMasterAsync(sql, null, cancellationToken);
    }

    private async Task ExecuteSqlAgainstMasterAsync(string sql, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default)
    {
        using var conn = new SqlConnection(dbConnectionString);
        await conn.OpenAsync(cancellationToken);
        var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text };
        if (parameters != null) cmd.Parameters.AddRange(parameters);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        await conn.CloseAsync();
    }
}

/*
Check DB for snapshots

SELECT
 db_name(db.source_database_id) source_database,
 db.name AS snapshot_db_name,
 db.database_id,
 db.source_database_id,
 db.create_date,
 db.compatibility_level,
 db.is_read_only,
 mf.physical_name
FROM sys.databases db
INNER JOIN sys.master_files mf
 ON db.database_id = mf.database_id
WHERE db.source_database_id is not null
 AND mf.is_sparse =1
ORDER BY db.name;

DROP DATABASE Snapshot1

 */