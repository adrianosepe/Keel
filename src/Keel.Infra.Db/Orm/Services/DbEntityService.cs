using System.Collections;
using System.Linq.Expressions;
using DotNetAppBase.Std.Exceptions.Assert;
using DotNetAppBase.Std.Exceptions.Base;
using DotNetAppBase.Std.Exceptions.Bussines;
using DotNetAppBase.Std.Library.ComponentModel.Model.Business;
using DotNetAppBase.Std.Library.ComponentModel.Model.Business.Enums;
using DotNetAppBase.Std.Library.ComponentModel.Model.Svc;
using DotNetAppBase.Std.Library.ComponentModel.Model.Svc.Enums;
using DotNetAppBase.Std.Library.ComponentModel.Model.Validation;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Keel.Infra.Db.Orm.Services;

public abstract class DbEntityService(IDbLayer dbLayer) : DbEntityRepository(dbLayer) { }

public abstract class DbEntityService<TEntity> : DbEntityRepository<TEntity> where TEntity : class, IEntity, new()
{
    [UsedImplicitly] 
    public static ServiceResponse<TEntity> NotFound { get; } = new(entity: null!, "Entity not found");

    private readonly Lazy<DbSet<TEntity>> _lazyDbSet;

    protected DbEntityService(IDbLayer dbLayer) : base(dbLayer)
    {
        _lazyDbSet = new Lazy<DbSet<TEntity>>(
            () => Db.Orm.Set<TEntity>());

        ValidationHelper = new EntityValidationHelper<TEntity>();
    }

    public bool ReadOnly { get; set; }

    protected EntityValidationHelper<TEntity> ValidationHelper { get; }

    public virtual IQueryable<TEntity> GetToComposeQuery() => GetDirectQuery();
    
    protected override async Task<ServiceResponse<TEntity>> ExecuteDefaultDbAction(TEntity entity, EServiceActionType actionType, CancellationToken cancellationToken)
    {
        CheckReadOnlyAndThrowException(actionType.ToString());

        ExecuteBeforeDbAction(entity, actionType);

        var validationResult = ExecuteValidations(entity, actionType);
        if (!validationResult.HasViolations)
        {
            await ExecuteInTransactionContext(
                entity, 
                actionType,
                () => GetRepositoryAction(actionType)(entity),
                cancellationToken);
        }

        var response = new ServiceResponse<TEntity>(entity, validationResult);

        await ExecuteAfterDbAction(entity, actionType, validationResult);

        return response;
    }

    protected EntityValidationResult ExecuteValidations(TEntity entity, EServiceActionType actionType)
    {
        return actionType switch
            {
                EServiceActionType.Insert => ExecuteInsertValidations(entity),
                EServiceActionType.Update => ExecuteUpdateValidations(entity),
                EServiceActionType.Delete => ExecuteDeleteValidations(entity),

                _ => throw new ArgumentOutOfRangeException(nameof(actionType)),
            };
    }
    protected virtual EntityValidationResult ExecuteInsertValidations(TEntity entity) => InternalValidateEntity(entity, EServiceActionType.Insert, InternalExecuteCustomInsertValidation);
    protected virtual EntityValidationResult ExecuteUpdateValidations(TEntity entity) => InternalValidateEntity(entity, EServiceActionType.Update, InternalExecuteCustomUpdateValidation);
    protected virtual EntityValidationResult ExecuteDeleteValidations(TEntity entity) => InternalValidateEntity(entity, EServiceActionType.Delete, InternalExecuteCustomDeleteValidation);

    protected async Task ExecuteInTransactionContext(TEntity entity, EServiceActionType actionType, Action actionBeforeSave)
    {
        await ExecuteInTransactionContext(
            async (dbCtx) =>
            {
                actionBeforeSave();

                await dbCtx.SaveChangesAsync();

                await ExecuteInDbAction(entity, actionType);
            });
    }

    protected EntityValidationResult InternalValidateEntity(TEntity entity, EServiceActionType? actionType, Action<bool> customValidationsAction)
    {
        ValidationHelper.Begin(entity);

        if (actionType != EServiceActionType.Delete)
        {
            EntityValidator.Validate(entity, ValidationHelper.Validations);
        }

        if (!ValidationHelper.HasViolations)
        {
            if (actionType != null)
            {
                InternalExecuteCustomValidation(ValidationHelper.HasViolations, actionType.Value);
            }

            customValidationsAction?.Invoke(ValidationHelper.HasViolations);
        }

        return ValidationHelper.End();
    }
    protected virtual void InternalExecuteCustomValidation(bool hasValidation, EServiceActionType actionType) { }
    protected virtual void InternalExecuteCustomInsertValidation(bool hasValidation) { }
    protected virtual void InternalExecuteCustomUpdateValidation(bool hasValidation) { }
    protected virtual void InternalExecuteCustomDeleteValidation(bool hasValidation) { }
    
    private void CheckReadOnlyAndThrowException(string actionName)
    {
        if (!ReadOnly)
        {
            return;
        }

        throw XFlowException.Create($"This service {GetType().Name} was configured as ReadOnly, the action '{actionName}' can't be called directly.");
    }
}