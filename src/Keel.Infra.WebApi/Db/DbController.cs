using DotNetAppBase.Std.Library.ComponentModel.Model.Svc;
using Keel.Infra.Db;
using Microsoft.AspNetCore.Mvc;

namespace Keel.Infra.WebApi.Db;

public abstract class DbController(IDbLayer dbLayer) : ControllerBase
{
    public IDbLayer Db => dbLayer;

    public string Name
    {
        get
        {
            var name = GetType().Name;
            return name.Replace("Controller", string.Empty);
        }
    }

    protected async Task<Result> InternalSafeExecuteAsync(Func<Task<Result>> funcTask)
    {
        try
        {
            return await funcTask();
        }
        catch (Exception e)
        {
            return Result.Exception(e);
        }
    }

    protected async Task<TypedResult<TArg>> InternalSafeExecuteAsync<TArg>(Func<Task<TypedResult<TArg>>> funcTask)
    {
        try
        {
            return await funcTask();
        }
        catch (Exception e)
        {
            return TypedResult<TArg>.Exception(e);
        }
    }

    protected async Task<TypedResult<TArg>> InternalSafeWrappedExecuteAsync<TArg>(Func<Task<TArg>> funcTask)
    {
        try
        {
            var arg = await funcTask();

            return TypedResult<TArg>.Success(arg);
        }
        catch (Exception e)
        {
            return TypedResult<TArg>.Exception(e);
        }
    }

    protected Result InternalTryExecute(Func<Result> funcTask)
    {
        try
        {
            return funcTask();
        }
        catch (Exception e)
        {
            return Result.Exception(e);
        }
    }

    protected TypedResult<TArg> InternalTryExecute<TArg>(Func<TypedResult<TArg>> funcTask)
    {
        try
        {
            return funcTask();
        }
        catch (Exception e)
        {
            return TypedResult<TArg>.Exception(e);
        }
    }
}