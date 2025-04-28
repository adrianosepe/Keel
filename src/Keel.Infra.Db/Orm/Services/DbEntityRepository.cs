using System.Collections;
using System.Linq.Expressions;
using DotNetAppBase.Std.Exceptions.Assert;
using DotNetAppBase.Std.Library.ComponentModel.Model.Business;
using DotNetAppBase.Std.Library.ComponentModel.Model.Business.Enums;
using DotNetAppBase.Std.Library.ComponentModel.Model.Svc;
using DotNetAppBase.Std.Library.ComponentModel.Model.Svc.Enums;
using DotNetAppBase.Std.Library.ComponentModel.Model.Validation;
using JetBrains.Annotations;
using Keel.Infra.Db.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Keel.Infra.Db.Orm.Services;

public abstract class DbEntityRepository(IDbLayer dbLayer)
{
    protected IDbLayer Db => dbLayer;
    protected DbContext Orm => dbLayer.Orm;
}

public abstract class DbEntityRepository<TEntity> : DbEntityRepository where TEntity : class, IEntity, new()
{
    private readonly Lazy<DbSet<TEntity>> _lazyDbSet;

    protected DbEntityRepository(IDbLayer dbLayer) : base(dbLayer)
    {
        _lazyDbSet = new Lazy<DbSet<TEntity>>(
            () => Db.Orm.Set<TEntity>());
    }

    [UsedImplicitly] 
    public string EntityName => nameof(TEntity);

    protected DbSet<TEntity> Set => _lazyDbSet.Value;

    protected virtual IQueryable<TEntity> GetDirectQuery() => Set;
    protected virtual IQueryable<TEntity> GetQuery() => GetDirectQuery();
    
    protected TypedResult<TEntity> Success(string reason) => TypedResult<TEntity>.Success(reason);
    protected TypedResult<TEntity> Success(TEntity entity, string? reason = null) => TypedResult<TEntity>.Success(entity, reason);
    protected TypedResult<TEntity> Error(string reason) => TypedResult<TEntity>.Error(reason);
    protected TypedResult<TEntity> Warning(string reason) => TypedResult<TEntity>.Warning(reason);
    protected TypedResult<TEntity> Exception(Exception ex) => TypedResult<TEntity>.Exception(ex);
    
    public Task<TEntity?> GetByIdAsync(int id) => GetQuery().FirstOrDefaultAsync(model => model.ID == id);
    public async Task<IEnumerable<TEntity>> GetAllAsync() => await GetQuery().ToArrayAsync();
    public async Task<bool> ExistsAsync(int id) => await GetQuery().AnyAsync(model => model.ID == id);

    public virtual async Task<TEntity> NewEntityAsync()
    {
        return await Task.Run(
            () =>
                {
                    var @new = new TEntity();

                    InternalInitializeNewEntity(@new);

                    return @new;
                });
    }
    public virtual Task<ServiceResponse<TEntity>> InsertAsync(TEntity entity, CancellationToken cancellationToken) => ExecuteDefaultDbAction(entity, EServiceActionType.Insert, cancellationToken);
    public virtual async Task<ServiceResponse<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await RemoveDetailEntitiesOnUpdate(entity);

        return await ExecuteDefaultDbAction(entity, EServiceActionType.Update, cancellationToken);
    }
    public virtual Task<ServiceResponse<TEntity>> DeleteAsync(TEntity entity, CancellationToken cancellationToken) => ExecuteDefaultDbAction(entity, EServiceActionType.Delete, cancellationToken);

    protected Task DirectDbAction(
        EServiceActionType actionType, TEntity entity, CancellationToken cancellationToken) => ExecuteInTransactionContext(entity, actionType, () => GetRepositoryAction(actionType)(entity), cancellationToken);
    protected Task DirectDbDelete(TEntity entity, CancellationToken cancellationToken) => DirectDbAction(EServiceActionType.Delete, entity, cancellationToken);
    protected Task DirectDbInsert(TEntity entity, CancellationToken cancellationToken) => DirectDbAction(EServiceActionType.Insert, entity, cancellationToken);
    protected Task DirectDbUpdate(TEntity entity, CancellationToken cancellationToken) => DirectDbAction(EServiceActionType.Update, entity, cancellationToken);

    protected virtual void ExecuteBeforeDbAction(TEntity entity, EServiceActionType actionType)
    {
        switch (actionType)
        {
            case EServiceActionType.Insert:
                ExecuteBeforeInsert(entity);
                break;

            case EServiceActionType.Update:
                ExecuteBeforeUpdate(entity);
                break;

            case EServiceActionType.Delete:
                ExecuteBeforeDelete(entity);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(actionType));
        }
    }
    protected virtual void ExecuteBeforeInsert(TEntity entity)
    {
        if (entity is ILogicalDelete ld)
        {
            ld.Situacao = ELogicalDelete.Active;
        }

        InternalBeforeDbAction(entity, EServiceActionType.Insert, InternalExecuteBeforeInsert);
    }
    protected virtual void ExecuteBeforeUpdate(TEntity entity) => InternalBeforeDbAction(entity, EServiceActionType.Update, InternalExecuteBeforeUpdate);
    protected virtual void ExecuteBeforeDelete(TEntity entity) => InternalBeforeDbAction(entity, EServiceActionType.Delete, InternalExecuteBeforeDelete);

    protected virtual async Task<ServiceResponse<TEntity>> ExecuteDefaultDbAction(TEntity entity, EServiceActionType actionType, CancellationToken cancellationToken)
    {
        ExecuteBeforeDbAction(entity, actionType);

        await ExecuteInTransactionContext(
            entity, 
            actionType,
            () => GetRepositoryAction(actionType)(entity),
            cancellationToken);

        var validationResult = new EntityValidationResult();
        var response = new ServiceResponse<TEntity>(entity, validationResult);

        await ExecuteAfterDbAction(entity, actionType, validationResult);

        return response;
    }

    protected virtual async Task ExecuteInDbAction(TEntity entity, EServiceActionType actionType)
    {
        switch (actionType)
        {
            case EServiceActionType.Insert:
                await ExecuteInsertion(entity);
                break;

            case EServiceActionType.Update:
                await ExecuteUpdating(entity);
                break;

            case EServiceActionType.Delete:
                await ExecuteDeletion(entity);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(actionType));
        }
    }
    protected virtual async Task ExecuteInsertion(TEntity entity) => await InternalInDbAction(entity, InternalExecuteInsertion);
    protected virtual async Task ExecuteUpdating(TEntity entity) => await InternalInDbAction(entity, InternalExecuteUpdating);
    protected virtual async Task ExecuteDeletion(TEntity entity) => await InternalInDbAction(entity, InternalExecuteDeletion);

    protected virtual async Task ExecuteAfterDbAction(TEntity entity, EServiceActionType actionType, EntityValidationResult validationResult)
    {
        switch (actionType)
        {
            case EServiceActionType.Insert:
                await ExecuteAfterInsert(entity, validationResult);
                break;

            case EServiceActionType.Update:
                await ExecuteAfterUpdate(entity, validationResult);
                break;

            case EServiceActionType.Delete:
                await ExecuteAfterDelete(entity, validationResult);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(actionType));
        }
    }
    protected virtual Task ExecuteAfterInsert(TEntity entity, EntityValidationResult validationResult) => InternalAfterDbAction(entity, validationResult, InternalExecuteAfterInsert);
    protected virtual Task ExecuteAfterUpdate(TEntity entity, EntityValidationResult validationResult) => InternalAfterDbAction(entity, validationResult, InternalExecuteAfterUpdate);
    protected virtual Task ExecuteAfterDelete(TEntity entity, EntityValidationResult validationResult) => InternalAfterDbAction(entity, validationResult, InternalExecuteAfterDelete);

    protected async Task<TResult> ExecuteInTransactionContext<TResult>(Func<DbContext, Task<TResult>> action)
    {
        var dbCtx = Db.Orm;
        var inTransaction = dbCtx.Database.CurrentTransaction != null;

        IDbContextTransaction? transaction = null;
        if (!inTransaction)
        {
            transaction = await dbCtx.Database.BeginTransactionAsync();
        }

        try
        {
            var result = await action(dbCtx);

            await (transaction?.CommitAsync() ?? Task.CompletedTask);

            return result;
        }
        catch (Exception ex)
        {
            await (transaction?.RollbackAsync() ?? Task.CompletedTask);

            XDebug.OnException(ex);

            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }
    protected async Task ExecuteInTransactionContext(Func<DbContext, Task> action)
    {
        var dbCtx = Db.Orm;
        var inTransaction = dbCtx.Database.CurrentTransaction != null;

        IDbContextTransaction? transaction = null;
        if (!inTransaction)
        {
            transaction = await dbCtx.Database.BeginTransactionAsync();
        }

        try
        {
            await action(dbCtx);

            await (transaction?.CommitAsync() ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            await (transaction?.RollbackAsync() ?? Task.CompletedTask);

            XDebug.OnException(ex);

            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }
    protected async Task ExecuteInTransactionContext(TEntity entity, EServiceActionType actionType, Action actionBeforeSave, CancellationToken cancellationToken)
    {
        await ExecuteInTransactionContext(
            async dbCtx =>
            {
                actionBeforeSave();

                await dbCtx.SaveChangesAsync(cancellationToken);

                await ExecuteInDbAction(entity, actionType);
            });
    }

    protected virtual void InternalBeforeDbAction(TEntity entity, EServiceActionType actionType, Action<TEntity> customAction)
    {
        customAction?.Invoke(entity);
    }
    protected virtual void InternalExecuteBeforeInsert(TEntity entity) { }
    protected virtual void InternalExecuteBeforeUpdate(TEntity entity) { }
    protected virtual void InternalExecuteBeforeDelete(TEntity entity) { }

    protected virtual Task InternalExecuteInsertion(TEntity entity) => Task.CompletedTask;
    protected virtual Task InternalExecuteUpdating(TEntity entity) => Task.CompletedTask;
    protected virtual Task InternalExecuteDeletion(TEntity entity) => Task.CompletedTask;

    protected virtual Task InternalAfterDbAction(TEntity entity, EntityValidationResult validationResult, Func<TEntity, EntityValidationResult, Task?> customAction) => customAction?.Invoke(entity, validationResult) ?? Task.CompletedTask;
    protected virtual Task? InternalExecuteAfterInsert(TEntity entity, EntityValidationResult actionValidationResult) => null;
    protected virtual Task? InternalExecuteAfterUpdate(TEntity entity, EntityValidationResult actionValidationResult) => null;
    protected virtual Task? InternalExecuteAfterDelete(TEntity entity, EntityValidationResult actionValidationResult) => null;

    protected virtual Task InternalInDbAction(TEntity entity, Func<TEntity, Task> customAction) => customAction?.Invoke(entity) ?? Task.CompletedTask;
    protected virtual void InternalInitializeNewEntity(TEntity @new) { }
    protected virtual IEnumerable<Expression<Func<TEntity?, IEnumerable>>> InternalIdentityDetailEntities() => Array.Empty<Expression<Func<TEntity?, IEnumerable>>>();

    protected virtual Action<TEntity> GetRepositoryAction(EServiceActionType actionType)
    {
        switch (actionType)
        {
            case EServiceActionType.Insert:
                return entity => { Set.Add(entity); };

            case EServiceActionType.Update:
                return entity => { Db.Orm.Update(entity); };

            case EServiceActionType.Delete:
                return entity =>
                {
                    if (entity is ILogicalDelete logicalDelete)
                    {
                        logicalDelete.Situacao = ELogicalDelete.Inactive;

                        Db.Orm.Update(entity);
                    }
                    else
                    {
                        Set.Remove(entity);
                    }
                };

            default:
                throw new ArgumentOutOfRangeException(nameof(actionType));
        }
    }

    private async Task RemoveDetailEntitiesOnUpdate(TEntity memEntity)
    {
        var detailEntities = InternalIdentityDetailEntities().Select(exp => exp.Compile()).ToArray();
        if (detailEntities.Length == 0)
        {
            return;
        }

        var dbEntity = await GetQuery().FirstOrDefaultAsync(e => e.ID == memEntity.ID);
        if (dbEntity == null)
        {
            return;
        }

        foreach (var mGet in detailEntities)
        {
            var fromDb = mGet.Invoke(dbEntity).OfType<IEntity>().ToArrayEfficient();
            var fromMem = mGet.Invoke(memEntity).OfType<IEntity>().ToArrayEfficient();

            fromDb
                .Where(e => fromMem.All(entity => entity.ID != e.ID))
                .ToArray()
                .ForEach(detailEntity =>
                {
                    Db.Orm.Attach(detailEntity);
                    Db.Orm.Entry(detailEntity).State = EntityState.Deleted;
                });
        }

        Db.Orm.Entry(dbEntity).State = EntityState.Detached;
    }
}