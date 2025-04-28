using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Keel.Infra.Db.Orm.Transaction;

public class DbWrappedTransaction(bool transactionOwner, DatabaseFacade database) 
    : IDbWrappedTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (!transactionOwner)
        {
            return Task.CompletedTask;
        }
        
        return database.CommitTransactionAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (!transactionOwner)
        {
            return Task.CompletedTask;
        }
        
        return database.RollbackTransactionAsync(cancellationToken);
    }
}