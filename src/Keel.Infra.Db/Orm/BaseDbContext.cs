using Keel.Infra.Db.Orm.Transaction;
using Microsoft.EntityFrameworkCore;

namespace Keel.Infra.Db.Orm;

public class BaseDbContext : DbContext, IDbUnitOfWork
{
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    
    public BaseDbContext() { }
    public BaseDbContext(DbContextOptions options) : base(options) { }
    
    public async Task<IDbWrappedTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            if (Database.CurrentTransaction != null)
            {
                return new DbWrappedTransaction(false, Database);
            }
            
            await Database.BeginTransactionAsync(cancellationToken);
            return new DbWrappedTransaction(true, Database);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}