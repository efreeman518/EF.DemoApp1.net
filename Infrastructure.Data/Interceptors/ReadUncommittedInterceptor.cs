using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Infrastructure.Data.Interceptors;
public class ReadUncommittedInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        command.CommandText = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; " + command.CommandText;
        return base.ReaderExecuting(command, eventData, result);
    }
}
