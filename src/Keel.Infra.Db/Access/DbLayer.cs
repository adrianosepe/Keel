using System.Data;
using System.Data.Common;
using Keel.Infra.Db.Access.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable UnusedMember.Global

namespace Keel.Infra.Db.Access;

public abstract class DbLayer : IDbLayer, IDbSharedContextProvider
{
    private readonly Lazy<DbContext> _lazyOrm;
    private readonly Lazy<DbDirectAccess> _lazyAdo;
    private readonly Lazy<DbDapperAccess> _lazyDapper;

    protected DbLayer()
    {
        _lazyOrm = new Lazy<DbContext>(InternalCreateDbContext);

        _lazyAdo = new Lazy<DbDirectAccess>(
            () => new DbDirectAccess(this));

        _lazyDapper = new Lazy<DbDapperAccess>(
            () => new DbDapperAccess(this));
    }

    public DbContext Orm => _lazyOrm.Value;
    public DbDirectAccess Ado => _lazyAdo.Value;
    public DbDapperAccess Dapper => _lazyDapper.Value;

    public async Task<DbConnection> GetConnectionAsync()
    {
        var context = await this
            .CastTo<IDbSharedContextProvider>()
            .GetContextAsync()
            .ConfigureAwait(false);

        return context.Connection;
    }

    protected abstract DbContext InternalCreateDbContext();

    async Task<DbSharedContext> IDbSharedContextProvider.GetContextAsync()
    {
        var transaction = Orm.Database.CurrentTransaction?.GetDbTransaction();

        var connection = Orm.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return new DbSharedContext(
            connection,
            transaction,
            false);
    }

    async Task<DbCommand> IDbSharedContextProvider.GetCommandAsync()
    {
        var connection = Orm.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection.CreateCommand();
    }
}

public class DbLayer<TDbContext>(TDbContext context) : DbLayer, IDbLayer<TDbContext>
    where TDbContext : DbContext
{
    public new TDbContext Orm => context;

    protected override DbContext InternalCreateDbContext() => Orm;
}

