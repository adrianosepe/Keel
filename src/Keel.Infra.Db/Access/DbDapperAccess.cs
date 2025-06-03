using System.Data;
using System.Data.Common;
using Dapper;
using Keel.Infra.Db.Access.Context;

namespace Keel.Infra.Db.Access;

public class DbDapperAccess(IDbSharedContextProvider sharedConnectionProvider)
{
    public async Task<T?> ReadOneAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken);
        var connection = context.Connection;

        return connection
            .QueryFirstOrDefault<T>(
                sql,
                param,
                commandType: CommandType.Text,
                transaction: context.Transaction);
    }
    public async Task<T?> ReadOneSpAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken);
        var connection = context.Connection;

        return connection.QueryFirstOrDefault<T>(
            sql,
            param,
            commandType: CommandType.StoredProcedure,
            transaction: context.Transaction);
    }

    public async Task<IEnumerable<T>> ReadAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken);
        var connection = context.Connection;

        return connection.Query<T>(
            sql, 
            param, 
            commandType: CommandType.Text, 
            transaction: context.Transaction);
    }

    public async Task<IEnumerable<T>> ReadSpAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken);
        var connection = context.Connection;

        return await connection.QueryAsync<T>(
            sql, 
            param, 
            commandType: CommandType.StoredProcedure, 
            transaction: context.Transaction);
    }
    public async Task<IEnumerable<T>> ReadSpAsync<T>(string sql, int commandTimeout, object? param = null, CancellationToken cancellationToken = default)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken);
        var connection = context.Connection;

        return await connection.QueryAsync<T>(
            sql,
            param,
            commandType: CommandType.StoredProcedure,
            transaction: context.Transaction,
            commandTimeout: commandTimeout);
    }

    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(Action<DbDapperAccessBuilder> config, CancellationToken cancellationToken = default)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken);
        var connection = context.Connection;

        var builder = new DbDapperAccessBuilder();

        config(builder);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return await connection.QueryAsync<TResult>(builder.Build(context.Transaction, cancellationToken));
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

    public CommandDefinition Build(DbTransaction? transaction, CancellationToken cancellationToken)
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