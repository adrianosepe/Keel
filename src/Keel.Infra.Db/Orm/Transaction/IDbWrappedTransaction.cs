namespace Keel.Infra.Db.Orm.Transaction;

public interface IDbWrappedTransaction
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}