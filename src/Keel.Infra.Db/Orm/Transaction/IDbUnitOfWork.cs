namespace Keel.Infra.Db.Orm.Transaction;

public interface IDbUnitOfWork
{
    public Task<IDbWrappedTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}