using Keel.Domain.CleanCode.Flow.Action;
using Keel.Domain.CleanCode.Web.Models;
using Microsoft.AspNetCore.Mvc;
using NoContentResult = Keel.Domain.CleanCode.Flow.Action.NoContentResult;
using NotFoundResult = Keel.Domain.CleanCode.Flow.Action.NotFoundResult;

namespace Keel.Domain.CleanCode.Web;

public class KeelApiController : ControllerBase
{
    protected IActionResult GetResult(Output output)
    {
        var result = output.Result;

        if (result is DataResult dataResult)
        {
            return StatusCode(dataResult.Code, dataResult.Data);
        }

        if (result is ErrorResult errorResult)
        {
            return StatusCode(
                errorResult.Code,
                new ErrorOutput(
                    new ErrorDetailOutput(
                        errorResult.Flag,
                        errorResult.Message
                    )
                )
            );
        }

        if (result is NoContentResult)
        {
            return NoContent();
        }

        if (result is NotFoundResult)
        {
            return NotFound();
        }

        return Ok(result);
    }
}