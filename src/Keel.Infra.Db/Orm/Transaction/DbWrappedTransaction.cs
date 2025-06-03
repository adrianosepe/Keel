using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Keel.Infra.Db.Orm.Transaction;

internal class DbWrappedTransaction(bool transactionOwner, DatabaseFacade database) 
    : IDbWrappedTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return transactionOwner ? database.CommitTransactionAsync(cancellationToken) : Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return transactionOwner ? database.RollbackTransactionAsync(cancellationToken) : Task.CompletedTask;
    }
}