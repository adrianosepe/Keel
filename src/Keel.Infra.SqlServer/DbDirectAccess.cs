using System.Data;
using System.Data.Common;
using DotNetAppBase.Std.Db.Work;
using DotNetAppBase.Std.Exceptions.Bussines;
using Keel.Infra.SqlServer.Context;
using Microsoft.Data.SqlClient;

// ReSharper disable UnusedMember.Global

namespace Keel.Infra.SqlServer;

public class DbDirectAccess(IDbSharedContextProvider provider)
{
    public async Task<DataSet> DataSetAsync(string command, CommandType commandType,
        CancellationToken cancellationToken = default, params SqlParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync().ConfigureAwait(false);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var set = new DataSet();

        using var adapter = new SqlDataAdapter(comm.CastTo<SqlCommand>());
        adapter.Fill(set);

        return set;
    }
    public async Task<DataTable> DataTableAsync(string command, CommandType commandType,
        CancellationToken cancellationToken = default, params SqlParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync();

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var table = new DataTable();

        using var adapter = new SqlDataAdapter(comm.CastTo<SqlCommand>());
        adapter.Fill(table);

        return table;
    }
    public async Task<DataRow?> DataRowAsync(string command, CommandType commandType,
        CancellationToken cancellationToken = default, params SqlParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync();

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var table = new DataTable();

        using var adapter = new SqlDataAdapter(comm.CastTo<SqlCommand>());
        adapter.Fill(table);

        return table
            .AsEnumerable()
            .FirstOrDefault();
    }

    public async Task<TScalar?> ScalarAsync<TScalar>(string command, CommandType commandType, params SqlParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync();

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        return (TScalar?)await comm.ExecuteScalarAsync().ConfigureAwait(false);
    }

    public async Task<int> NonQueryAsync(string command, CommandType commandType,
        CancellationToken cancellationToken = default, params SqlParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync();

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        return await comm.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<TModel?> ReadOneAsync<TModel>(string command, CommandType commandType, params SqlParameter[] parameters)
        where TModel : DbEntity, new()
    {
        return (await ReadAsync<TModel>(command, commandType, parameters)).FirstOrDefault();
    }

    public async Task<IEnumerable<TModel>> ReadAsync<TModel>(
        string command, CommandType commandType, params SqlParameter[] parameters)
        where TModel : DbEntity, new()
    {
        await using var comm = await provider.GetCommandAsync();

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var table = new DataTable();

        using (var adapter = new SqlDataAdapter(comm.CastTo<SqlCommand>()))
        {
            adapter.Fill(table);
        }

        return table.Translate<TModel>();
    }

    public async Task<TResult> QueryAsync<TResult>(Action<DbDirectAccessBuilder> config, CancellationToken cancellationToken = default)
    {
        await using var comm = await provider.GetCommandAsync();

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

    public async Task<TResult> NonQueryAsync<TResult>(Action<DbDirectAccessBuilder> config, CancellationToken cancellationToken = default)
    {
        await using var comm = await provider.GetCommandAsync();

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

    public Task<DateTime> NowAsync() => ScalarAsync<DateTime>("SELECT dbo.LOCALGETDATE()", CommandType.Text);

    public void Read(string command, CommandType commandType, Action<DbDataReader> callback,
        params SqlParameter[] parameters)
    {
        using var comm = provider.GetCommandAsync().GetAwaiter().GetResult();

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var reader = comm.ExecuteReader();
        while (reader.NextResult())
        {
            callback(reader);
        }
    }

    public IEnumerable<T> Read<T>(string command, CommandType commandType, Func<SqlDataReader, T> processAction,
        params SqlParameter[] parameters)
    {
        using var comm = provider.GetCommandAsync().GetAwaiter().GetResult();

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var reader = comm.ExecuteReader().CastTo<SqlDataReader>();
        while (reader.Read())
        {
            yield return processAction(reader);
        }
    }
}