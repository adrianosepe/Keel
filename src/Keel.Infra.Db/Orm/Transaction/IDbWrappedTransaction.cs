namespace Keel.Infra.Db.Orm.Transaction;

public interface IDbWrappedTransaction
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}