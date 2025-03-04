using System.Data.Common;
using Keel.Infra.SqlServer.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable UnusedMember.Global

namespace Keel.Infra.SqlServer;

public abstract class DbLayer : IDbSharedContextProvider, IDbLayer
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

    protected abstract DbContext InternalCreateDbContext();

    async Task<DbSharedContext> IDbSharedContextProvider.GetContextAsync()
    {
        var connection = Orm.Database.GetDbConnection().CastTo<SqlConnection>();
        var transaction = Orm.Database.CurrentTransaction?.GetDbTransaction().CastTo<SqlTransaction?>();

        await connection.OpenAsync();

        return new DbSharedContext(
            connection.CastTo<SqlConnection>(),
            transaction?.CastTo<SqlTransaction?>(),
            false);
    }

    async Task<DbCommand> IDbSharedContextProvider.GetCommandAsync()
    {
        var connection = Orm.Database.GetDbConnection().CastTo<SqlConnection>();

        await connection.OpenAsync();

        return connection.CreateCommand();
    }
}

public class DbLayer<TDbContext>(TDbContext context) : DbLayer, IDbLayer<TDbContext>
    where TDbContext : DbContext, new()
{
    public new TDbContext Orm => context;

    protected override DbContext InternalCreateDbContext() => Orm;
}

