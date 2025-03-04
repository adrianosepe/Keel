using System.Data.Common;

namespace Keel.Infra.SqlServer.Context;

public interface IDbSharedContextProvider
{
    Task<DbSharedContext> GetContextAsync();
    Task<DbCommand> GetCommandAsync();
}