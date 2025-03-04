using Microsoft.EntityFrameworkCore;

namespace Keel.Infra.SqlServer;

public interface IDbLayer
{
    DbContext Orm { get; }
    DbDirectAccess Ado { get; }
    DbDapperAccess Dapper { get; }
}

public interface IDbLayer<out TDbContext> : IDbLayer where TDbContext : DbContext
{
    new TDbContext Orm { get; }
}