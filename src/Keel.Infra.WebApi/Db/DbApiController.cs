using Keel.Infra.Db;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.ResponseModel;
using DevExtreme.AspNet.Mvc;
using DotNetAppBase.Std.Library.ComponentModel.Model.Business;
using DotNetAppBase.Std.Library.ComponentModel.Model.Svc;
using Keel.Infra.Db.Orm.Services;
using Microsoft.EntityFrameworkCore;

// ReSharper disable UnusedMember.Global

namespace Keel.Infra.WebApi.Db;

public abstract class DbApiController<TService>(IDbLayer dbLayer, TService service) : DbController(dbLayer)
    where TService : DbEntityService
{
    protected TService Svc => service;
}

public abstract class DbApiController<TModel, TService>(IDbLayer dbLayer, TService service)
    : DbController(dbLayer)
    where TModel : class, IEntity, new()
    where TService : DbEntityService<TModel>
{
    private static readonly TypedResult<TModel> NotFoundResult = TypedResult<TModel>.Error("Entidade indisponível!");

    protected TService Svc => service;

    protected async Task<bool> Exists(int key) => await Svc.GetQuery().AnyAsync(model => model.ID == key);

    protected async Task<TypedResult<TModel>> InternalDeleteAsync(int key, CancellationToken cancellationToken)
    {
        return await InternalSafeExecuteAsync(
            async () =>
            {
                var entity = await Svc.GetByIdAsync(key, cancellationToken);
                if (entity == null)
                {
                    return NotFoundResult;
                }

                try
                {
                    await Svc.DeleteAsync(entity, cancellationToken);

                    return TypedResult<TModel>.Success(entity);
                }
                catch (Exception e)
                {
                    return TypedResult<TModel>.Exception(e);
                }
            });
    }

    protected LoadResult InternalGet(DataSourceLoadOptions loadOptions) => DataSourceLoader.Load(Svc.GetQuery(), loadOptions);

    protected Task<TypedResult<IEnumerable<TModel>>> InternalGetAllAsync(CancellationToken cancellationToken) => InternalExecuteGetAsync(Svc.GetAllAsync(cancellationToken));

    protected virtual async Task<LoadResult> InternalGetAsync(DataSourceLoadOptions loadOptions)
    {
        try
        {
            var data = await Svc.GetQuery().ToArrayAsync();
            var result = await InternalExecuteGetAsync(Task.Run(() => data.AsQueryable()), loadOptions);

            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    protected async Task<TypedResult<TModel>> InternalGetByIdAsync(int key, CancellationToken cancellationToken)
    {
        return await InternalSafeExecuteAsync(
            async () =>
                {
                    var entity = await Svc.GetByIdAsync(key, cancellationToken);

                    return entity == null ? NotFoundResult : TypedResult<TModel>.Success(entity);
                });
    }

    protected async Task<LoadResult> InternalGetToComposeAsync(DataSourceLoadOptions loadOptions)
    {
        var query = Svc.GetQuery();
        var result = await DataSourceLoader.LoadAsync(query, loadOptions);
        return result;
    }

    protected async Task<TypedResult<TModel>> InternalInsertAsync(TModel entity, CancellationToken cancellationToken)
    {
        return await InternalSafeExecuteAsync(
            async () =>
            {
                var result = await Svc.InsertAsync(entity, cancellationToken);
                if (result.Fail)
                {
                    return TypedResult<TModel>.Error(result);
                }

                var entityFromDb = await Svc
                    .GetQuery()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(model => model.ID == entity.ID, cancellationToken)
                    .ConfigureAwait(false);

                return TypedResult<TModel>.Success(entityFromDb!);
            });
    }

    protected async Task<TypedResult<TModel>> InternalNewEntityAsync()
    {
        return await InternalSafeExecuteAsync(
            async () => TypedResult<TModel>.Success(await Svc.NewEntityAsync()));
    }

    protected async Task<TypedResult<TModel>> InternalUpdateAsync(int key, TModel entity, CancellationToken cancellationToken)
    {
        return await InternalSafeExecuteAsync(
            async () =>
            {
                var lResult = await InternalValidateOnUpdate(key, entity);
                if (lResult != null)
                {
                    return lResult;
                }

                try
                {
                    var result = await Svc.UpdateAsync(entity, cancellationToken);
                    if (result.Fail)
                    {
                        return TypedResult<TModel>.Error(result);
                    }

                    var entityFromDb = await Svc
                        .GetQuery()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(model => model.ID == key, cancellationToken);

                    return entityFromDb == null 
                        ? NotFoundResult 
                        : TypedResult<TModel>.Success(entityFromDb);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await Exists(key))
                    {
                        return NotFoundResult;
                    }

                    throw;
                }
            });
    }

    protected async Task<TypedResult<TModel>?> InternalValidateOnUpdate(int key, TModel entity)
    {
        return await InternalSafeExecuteAsync(
            async () =>
            {
                if (key != entity.ID)
                {
                    return TypedResult<TModel>.Error("Requisição mal formada.");
                }

                if (!await Exists(key))
                {
                    return NotFoundResult;
                }

                return null!;
            });
    }

    protected async Task<TypedResult<IEnumerable<TModel>>> InternalExecuteGetAsync(Task<IEnumerable<TModel>> asyncQuery)
    {
        return await InternalSafeExecuteAsync(
            async () => TypedResult<IEnumerable<TModel>>.Success(await asyncQuery));
    }

    protected LoadResult InternalExecuteGet(IQueryable<TModel> query, DataSourceLoadOptions loadOptions) => DataSourceLoader.Load(query, loadOptions);

    protected LoadResult InternalExecuteGet(IQueryable<dynamic> query, DataSourceLoadOptions loadOptions) => DataSourceLoader.Load(query, loadOptions);

    protected async Task<LoadResult> InternalExecuteGetAsync(Task<IQueryable<TModel>> asyncQuer, DataSourceLoadOptions loadOptions) => DataSourceLoader.Load(await asyncQuer, loadOptions);
}