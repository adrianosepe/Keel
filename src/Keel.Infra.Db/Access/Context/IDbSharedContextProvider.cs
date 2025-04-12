using System.Data.Common;

namespace Keel.Infra.Db.Access.Context;

public interface IDbSharedContextProvider
{
    Task<DbSharedContext> GetContextAsync();
    Task<DbCommand> GetCommandAsync();
}