namespace Keel.Infra.Db.Orm.Transaction;

public interface IDbUnitOfWork
{
    Task<IDbWrappedTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}