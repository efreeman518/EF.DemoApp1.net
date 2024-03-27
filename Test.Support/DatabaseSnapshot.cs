using Microsoft.Data.SqlClient;
using System.Data;

namespace Test.Support;

/// <summary>
/// https://johnnyreilly.com/integration-tests-with-sql-server
/// </summary>
public class SqlDatabaseSnapshot(string dbName, string dbSnapshotPath, string dbSnapshotName, string dbConnectionString)
{
    public void CreateSnapshot()
    {
        if (!Directory.Exists(dbSnapshotPath))
            Directory.CreateDirectory(dbSnapshotPath);

        var sql = $"CREATE DATABASE {dbSnapshotName} ON (NAME=[{dbName}], FILENAME='{dbSnapshotPath}{dbSnapshotName}') AS SNAPSHOT OF [{dbName}]";

        ExecuteSqlAgainstMaster(sql);
    }

    public void DeleteSnapshot()
    {
        var sql = $"DROP DATABASE {dbSnapshotName}";

        ExecuteSqlAgainstMaster(sql);
    }

    public void RestoreSnapshot()
    {
        var sql = "USE master;\r\n" +

            $"ALTER DATABASE {dbName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;\r\n" +

            $"RESTORE DATABASE {dbName}\r\n" +
            $"FROM DATABASE_SNAPSHOT = '{dbSnapshotName}';\r\n" +

            $"ALTER DATABASE {dbName} SET MULTI_USER;\r\n";

        ExecuteSqlAgainstMaster(sql);
    }

    private void ExecuteSqlAgainstMaster(string sql, params SqlParameter[] parameters)
    {
        using (var conn = new SqlConnection(dbConnectionString))
        {
            conn.Open();
            var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text };
            cmd.Parameters.AddRange(parameters);
            cmd.ExecuteNonQuery();
            conn.Close();
        }
    }
}
