using Keel.Domain.CleanCode.Flow.Action;
using Microsoft.AspNetCore.Mvc;
using NoContentResult = Keel.Domain.CleanCode.Flow.Action.NoContentResult;

namespace Keel.Infra.WebApi;

public class ApiController : ControllerBase
{
    protected IActionResult GetResult(Output output)
    {
        var result = output.Result;

        return result switch
            {
                DataResult dataResult => StatusCode(dataResult.Code, dataResult.Data),
                ErrorResult errorResult => StatusCode(errorResult.Code, errorResult.Error),
                NoContentResult => NoContent(),
                _ => Ok(result),
            };
    }

}