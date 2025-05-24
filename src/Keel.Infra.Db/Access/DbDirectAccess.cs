using System.Data;
using System.Data.Common;
using DotNetAppBase.Std.Db.Work;
using Keel.Infra.Db.Access.Context;

// ReSharper disable UnusedMember.Global

namespace Keel.Infra.Db.Access;

public abstract class DbDirectAccess(IDbSharedContextProvider provider)
{
    public async Task<DataSet> DataSetAsync(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        await using var comm = await provider
            .GetCommandAsync(cancellationToken)
            .ConfigureAwait(false);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var set = new DataSet();
        
        using var adapter = InternalCreateDataAdapter(comm);
        adapter.Fill(set);

        return set;
    }
    public async Task<DataTable> DataTableAsync(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync(cancellationToken);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var table = new DataTable();

        using var adapter = InternalCreateDataAdapter(comm);
        adapter.Fill(table);

        return table;
    }
    
    public async Task<DataRow?> DataRowAsync(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync(cancellationToken);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var table = new DataTable();

        using var adapter = InternalCreateDataAdapter(comm);
        adapter.Fill(table);

        return table
            .AsEnumerable()
            .FirstOrDefault();
    }

    public async Task<TScalar?> ScalarAsync<TScalar>(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync(cancellationToken);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        return (TScalar?)await comm.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> NonQueryAsync(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync(cancellationToken);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        return await comm.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<TModel?> ReadOneAsync<TModel>(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters)
        where TModel : DbEntity, new()
    {
        var query = await ReadAsync<TModel>(
                command, commandType, cancellationToken, parameters)
            .ConfigureAwait(false);
        return query.FirstOrDefault();
    }

    public async Task<IEnumerable<TModel>> ReadAsync<TModel>(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters) where TModel : DbEntity, new()
    {
        await using var comm = await provider.GetCommandAsync(cancellationToken);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var table = new DataTable();

        using (var adapter = InternalCreateDataAdapter(comm))
        {
            adapter.Fill(table);
        }

        return table.Translate<TModel>();
    }

    public void Read(
        string command, CommandType commandType, Action<DbDataReader> callback, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        using var comm = provider.GetCommandAsync(cancellationToken).GetAwaiter().GetResult();

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

    public IEnumerable<T> Read<T>(
        string command, CommandType commandType, Func<DbDataReader, T> processAction, CancellationToken cancellationToken,
        params DbParameter[] parameters)
    {
        using var comm = provider.GetCommandAsync(cancellationToken)
            .GetAwaiter()
            .GetResult();

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var reader = comm.ExecuteReader();
        while (reader.Read())
        {
            yield return processAction(reader);
        }
    }
    
    public Task<DateTime> GetCurrentUtcDateTimeAsync(CancellationToken cancellationToken)
    {
        return ScalarAsync<DateTime>(InternalGetCurrentUtcDateTimeSql(), CommandType.Text, cancellationToken);
    }

    protected abstract DbDataAdapter InternalCreateDataAdapter(DbCommand comm);
    protected abstract string InternalGetCurrentUtcDateTimeSql();
}