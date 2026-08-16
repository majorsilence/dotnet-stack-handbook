using System.Data;
using Microsoft.Data.Sqlite;

namespace MajorSilence.DataAccess;

// The base abstract repository from "Structuring an Application".
public abstract class BaseRepo
{
    private readonly string cnStr;

    protected BaseRepo(string cnStr)
    {
        this.cnStr = cnStr;
    }

    protected T WithConnection<T>(Func<IDbConnection, T> sqlTransaction)
    {
        using (var connection = new SqliteConnection(cnStr))
        {
            connection.Open();
            return sqlTransaction(connection);
        }
    }

    protected void WithConnection(Action<IDbConnection> sqlTransaction)
    {
        using (var connection = new SqliteConnection(cnStr))
        {
            connection.Open();
            sqlTransaction(connection);
        }
    }

    protected async Task<T> WithConnectionAsync<T>(Func<IDbConnection, Task<T>> sqlTransaction)
    {
        using (var connection = new SqliteConnection(cnStr))
        {
            await connection.OpenAsync();
            return await sqlTransaction(connection);
        }
    }

    // No <T> here.  With the type parameter appearing nowhere in the signature it
    // could never be inferred, so the overload was uncallable.
    protected async Task WithConnectionAsync(Func<IDbConnection, Task> sqlTransaction)
    {
        using (var connection = new SqliteConnection(cnStr))
        {
            await connection.OpenAsync();
            await sqlTransaction(connection);
        }
    }
}
