using System.Data;
using DotNetAppBase.Std.Exceptions.Bussines;
using JetBrains.Annotations;
using Keel.Infra.Db.Access;
using Keel.Infra.Db.SqlServer.Access;
using Microsoft.Data.SqlClient;

// ReSharper disable CheckNamespace
namespace Keel.Infra.Db;
// ReSharper restore CheckNamespace

[UsedImplicitly]
public static class DbDapperAccessExtensions
{
    public static async Task<TResult> QueryAsync<TResult>(this DbDirectAccess access, Action<DbDirectAccessBuilder> config, CancellationToken cancellationToken = default)
    {
        await using var comm = await access.Provider.GetCommandAsync(cancellationToken);

        var builder = new DbDirectAccessBuilder(comm.CastTo<SqlCommand>());

        config(builder);

        builder.SetExecutionByReturnType<TResult>();

        if (builder.Mode == DbDirectAccessBuilder.EExecMode.PrimitiveValue)
        {
            return (TResult)(object)comm.ExecuteScalarAsync(cancellationToken);
        }

        var set = new DataSet();

        using var adapter = new SqlDataAdapter(comm.CastTo<SqlCommand>());

        await Task.Run(() => adapter.Fill(set), cancellationToken);

        return builder.Mode switch
        {
            DbDirectAccessBuilder.EExecMode.DataRow when set.Tables.Count < 1 || set.Tables[0].Rows.Count < 1 =>
                throw XFlowException.Create("Query don't return valida result"),
            DbDirectAccessBuilder.EExecMode.DataRow => (TResult)(object)set.Tables[0].Rows[0],
            DbDirectAccessBuilder.EExecMode.DataTable when set.Tables.Count < 1 => throw XFlowException.Create(
                "Query don't return valida result"),
            DbDirectAccessBuilder.EExecMode.DataTable => (TResult)(object)set.Tables[0],
            _ => (TResult)(object)set,
        };
    }

    public static async Task<TResult> NonQueryAsync<TResult>(this DbDirectAccess access, Action<DbDirectAccessBuilder> config, CancellationToken cancellationToken = default)
    {
        await using var comm = await access.Provider.GetCommandAsync(cancellationToken);

        var builder = new DbDirectAccessBuilder(comm.CastTo<SqlCommand>());

        config(builder);

        builder.SetExecutionByReturnType<TResult>();

        if (builder.Mode == DbDirectAccessBuilder.EExecMode.PrimitiveValue)
        {
            return (TResult)(object)comm.ExecuteScalarAsync(cancellationToken);
        }

        var set = new DataSet();

        using var adapter = new SqlDataAdapter(comm.CastTo<SqlCommand>());

        await Task.Run(() => adapter.Fill(set), cancellationToken);

        if (builder.Mode == DbDirectAccessBuilder.EExecMode.DataRow)
        {
            if (set.Tables.Count < 1 || set.Tables[0].Rows.Count < 1)
            {
                throw XFlowException.Create("Query don't return valida result");
            }

            return (TResult)(object)set.Tables[0].Rows[0];
        }

        if (builder.Mode == DbDirectAccessBuilder.EExecMode.DataTable)
        {
            if (set.Tables.Count < 1)
            {
                throw XFlowException.Create("Query don't return valida result");
            }

            return (TResult)(object)set.Tables[0];
        }

        return (TResult)(object)set;
    }
}