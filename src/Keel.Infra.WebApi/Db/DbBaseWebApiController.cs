using Keel.Infra.Db.Access;
using Keel.Infra.Db.Services;
using System;
using System.Linq;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.ResponseModel;
using DevExtreme.AspNet.Mvc;
using DotNetAppBase.Std.Library.ComponentModel.Model.Business;
using DotNetAppBase.Std.Library.ComponentModel.Model.Svc;
using Microsoft.EntityFrameworkCore;

// ReSharper disable UnusedMember.Global

namespace Keel.Infra.WebApi.Db;

public abstract class DbBaseWebApiController<TService>(IDbLayer sqlLayer, TService service) : DbBaseController(sqlLayer)
    where TService : DbEntityService
{
    protected TService Svc => service;
}

public abstract class DbBaseWebApiController<TModel, TService>(IDbLayer sqlLayer, TService service)
    : DbBaseController(sqlLayer)
    where TModel : class, IEntity, new()
    where TService : DbEntityService<TModel>
{
    private static readonly TypedResult<TModel> NotFoundResult = TypedResult<TModel>.Error("Entidade indisponível!");

    protected TService Svc => service;

    protected async Task<bool> Exists(int key) => await Svc.GetQuery().AnyAsync(model => model.ID == key);

    protected async Task<TypedResult<TModel>> InternalDeleteAsync(int key)
    {
        return await InternalSafeExecuteAsync(
            async () =>
            {
                var entity = await Svc.GetByIdAsync(key);
                if (entity == null)
                {
                    return NotFoundResult;
                }

                try
                {
                    await Svc.DeleteAsync(entity);

                    return TypedResult<TModel>.Success(entity);
                }
                catch (Exception e)
                {
                    return TypedResult<TModel>.Exception(e);
                }
            });
    }

    protected LoadResult InternalGet(DataSourceLoadOptions loadOptions) => DataSourceLoader.Load(Svc.GetQuery(), loadOptions);

    protected Task<TypedResult<IEnumerable<TModel>>> InternalGetAllAsync() => InternalExecuteGetAsync(Svc.GetAllAsync());

    protected virtual async Task<LoadResult> InternalGetAsync(DataSourceLoadOptions loadOptions) => await InternalExecuteGetAsync(Task.Run(() => Svc.GetQuery()), loadOptions);

    protected async Task<TypedResult<TModel>> InternalGetByIdAsync(int key)
    {
        return await InternalSafeExecuteAsync(
            async () =>
            {
                var entity = await Svc.GetByIdAsync(key);

                return entity == null ? NotFoundResult : TypedResult<TModel>.Success(entity);
            });
    }

    protected async Task<LoadResult> InternalGetToComposeAsync(DataSourceLoadOptions loadOptions)
    {
        var result = await Task.Run(() => DataSourceLoader.Load(Svc.GetToComposeQuery(), loadOptions));

        return result;
    }

    protected async Task<TypedResult<TModel>> InternalInsertAsync(TModel entity)
    {
        return await InternalSafeExecuteAsync(
            async () =>
            {
                var result = await Svc.InsertAsync(entity);
                if (result.Fail)
                {
                    return TypedResult<TModel>.Error(result);
                }

                return TypedResult<TModel>.Success(
                    (await Svc
                        .GetQuery()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(model => model.ID == entity.ID)
                        .ConfigureAwait(false))!);
            });
    }

    protected async Task<TypedResult<TModel>> InternalNewEntity()
    {
        return await InternalSafeExecuteAsync(
            async () => TypedResult<TModel>.Success(await Svc.NewEntityAsync()));
    }

    protected async Task<TypedResult<TModel>> InternalUpdateAsync(int key, TModel entity)
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
                    var result = await Svc.UpdateAsync(entity);
                    if (result.Fail)
                    {
                        return TypedResult<TModel>.Error(result);
                    }

                    var entityFromDb = await Svc
                        .GetQuery()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(model => model.ID == key);

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