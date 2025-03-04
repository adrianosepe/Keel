using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Keel.Infra.SqlServer.Context;

// ReSharper disable UnusedMember.Global

namespace Keel.Infra.SqlServer;

public class DbDapperAccess(IDbSharedContextProvider sharedConnectionProvider)
{
    public async Task<T?> ReadOneAsync<T>(string sql, object? param = null)
    {
        using var ctx = await sharedConnectionProvider.GetContextAsync();
        var conn = ctx.Connection;

        return conn.QueryFirstOrDefault<T>(
            sql, 
            param, 
            commandType: CommandType.Text, 
            transaction: ctx.Transaction);
    }
    public async Task<T?> ReadOneSpAsync<T>(string sql, object? param = null)
    {
        using var ctx = await sharedConnectionProvider.GetContextAsync();
        var conn = ctx.Connection;

        return conn.QueryFirstOrDefault<T>(
            sql, 
            param, 
            commandType: CommandType.StoredProcedure, 
            transaction: ctx.Transaction);
    }

    public async Task<IEnumerable<T>> ReadAsync<T>(string sql, object? param = null)
    {
        using var ctx = await sharedConnectionProvider.GetContextAsync();
        var conn = ctx.Connection;

        return conn.Query<T>(sql, param, commandType: CommandType.Text, transaction: ctx.Transaction);
    }
    
    public async Task<IEnumerable<T>> ReadSpAsync<T>(string sql, object? param = null)
    {
        using var ctx = await sharedConnectionProvider.GetContextAsync();
        var conn = ctx.Connection;

        return await conn.QueryAsync<T>(
            sql, param, commandType: CommandType.StoredProcedure, transaction: ctx.Transaction);
    }
    public async Task<IEnumerable<T>> ReadSpAsync<T>(string sql, int commandTimeout, object? param = null)
    {
        using var ctx = await sharedConnectionProvider.GetContextAsync();
        var conn = ctx.Connection;

        return await conn.QueryAsync<T>(
            sql, 
            param, 
            commandType: CommandType.StoredProcedure, 
            transaction: ctx.Transaction,
            commandTimeout: commandTimeout);
    }

    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(Action<DbDapperAccessBuilder> config, CancellationToken cancellationToken = default)
    {
        using var ctx = await sharedConnectionProvider.GetContextAsync();
        var conn = ctx.Connection;

        var builder = new DbDapperAccessBuilder();

        config(builder);

        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        return await conn.QueryAsync<TResult>(builder.Build(ctx.Transaction, cancellationToken));
    }
}

public class DbDapperAccessBuilder
{
    private readonly DynamicParameters _parameters = new();

    private int? _commandTimeout;
    private string _commandText = null!;
    private CommandType _commandType;

    public DbDapperAccessBuilder ForCommand(string command, CommandType commandType)
    {
        _commandText = command;
        _commandType = commandType;

        return this;
    }

    public DbDapperAccessBuilder ForText(string command)
    {
        _commandText = command;
        _commandType = CommandType.Text;

        return this;
    }

    public DbDapperAccessBuilder ForStoredProc(string command)
    {
        _commandText = command;
        _commandType = CommandType.StoredProcedure;

        return this;
    }

    public DbDapperAccessBuilder WithTimeout(int timeout)
    {
        _commandTimeout = timeout;

        return this;
    }

    public DbDapperAccessBuilder AddInt(string name, int value)
    {
        _parameters.Add(name, value, DbType.Int32);

        return this;
    }

    public DbDapperAccessBuilder AddDateTime(string name, DateTime value)
    {
        _parameters.Add(name, value, DbType.DateTime);

        return this;
    }

    public DbDapperAccessBuilder AddString(string name, string value)
    {
        _parameters.Add(name, value, DbType.String);

        return this;
    }

    public CommandDefinition Build(SqlTransaction? transaction, CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            _commandText,
            _parameters,
            transaction,
            _commandTimeout,
            _commandType,
            cancellationToken: cancellationToken);
    }
}