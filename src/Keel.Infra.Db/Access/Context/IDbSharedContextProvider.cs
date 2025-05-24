using System.Data.Common;

namespace Keel.Infra.Db.Access.Context;

public interface IDbSharedContextProvider
{
    Task<DbSharedContext> GetContextAsync(CancellationToken cancellationToken);
    Task<DbCommand> GetCommandAsync(CancellationToken cancellationToken);
}