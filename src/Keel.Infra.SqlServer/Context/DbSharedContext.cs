using Microsoft.Data.SqlClient;

namespace Keel.Infra.SqlServer.Context;

public class DbSharedContext(
    SqlConnection connection,
    SqlTransaction? transaction,
    bool dedicated) : IDisposable
{
    public SqlConnection Connection => connection;
    public SqlTransaction? Transaction => transaction;

    public SqlCommand CreateCommand()
    {
        if (dedicated)
        {
            connection.Open();
        }

        var command = connection.CreateCommand();
        command.Transaction = transaction;

        return command;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (!dedicated)
        {
            return;
        }

        connection.Dispose();
        transaction?.Dispose();
    }
}