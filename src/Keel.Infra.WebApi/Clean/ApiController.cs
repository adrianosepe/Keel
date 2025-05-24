using Keel.Domain.CleanCode.Flow.Action;
using Microsoft.AspNetCore.Mvc;
using NoContentResult = Keel.Domain.CleanCode.Flow.Action.NoContentResult;
using NotFoundResult = Keel.Domain.CleanCode.Flow.Action.NotFoundResult;

namespace Keel.Infra.WebApi.Clean;

public class ApiController : ControllerBase
{
    protected IActionResult GetResult(Output output)
    {
        var result = output.Result;

        return result switch
        {
            DataResult dataResult => StatusCode(dataResult.Code, dataResult.Data),
            ValidationErrorResult validationErrorResult => StatusCode(
                validationErrorResult.Code,
                new
                {
                    Error = validationErrorResult.Message,
                    Flag = validationErrorResult.Flag,
                    Errors = validationErrorResult.ValidationErrors,
                }),
            ErrorResult errorResult => StatusCode(
                errorResult.Code,
                new
                {
                    Error = errorResult.Message,
                    Flag = errorResult.Flag,
                }),
            NoContentResult => NoContent(),
            NotFoundResult => NotFound(),
            _ => Ok(result),
        };
    }

}