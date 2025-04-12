using System.Net;

namespace Keel.Domain.CleanCode.Flow.Action;

public sealed class Output
{
    public static OperationResult NoContentResult { get; set; } = new NoContentResult();

    public OperationResult Result { get; set; } = NoContentResult;

    public bool IsSuccess => Result.IsSuccess;
    public bool IsFail => Result.IsFail;
}

public static class OutputExtensions
{
    public static void Ok(this Output output, object? data = null)
    {
        output.Result = new DataResult((int)HttpStatusCode.OK, data);
    }

    public static void Created(this Output output, object data)
    {
        output.Result = new DataResult((int)HttpStatusCode.Created, data);
    }

    public static void NoContent(this Output output)
    {
        output.Result = new NoContentResult();
    }

    public static void Error(this Output output, HttpStatusCode code)
    {
        output.Result = new ErrorResult((int)code);
    }

    public static void Error(this Output output, HttpStatusCode code, string errorMessage)
    {
        output.Result = new ErrorResult((int)code, new ErrorDetail(null, null, errorMessage));
    }

    public static void Error(this Output output, HttpStatusCode code, ErrorDetail errorDetail)
    {
        output.Result = new ErrorResult((int)code, errorDetail);
    }
}