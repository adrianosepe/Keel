using System.Net;
using FluentValidation.Results;

namespace Keel.Domain.CleanCode.Flow.Action;

public static class OutputExtensions
{
    public static Output Ok(this Output output, object? data = null)
    {
        output.Result = new DataResult((int)HttpStatusCode.OK, data);
        return output;
    }

    public static Output Created(this Output output, object data)
    {
        output.Result = new DataResult((int)HttpStatusCode.Created, data);
        return output;
    }
    
    public static Output NoContent(this Output output)
    {
        output.Result = new NoContentResult();
        return output;
    }
    
    public static Output Error(this Output output, HttpStatusCode code)
    {
        output.Result = new ErrorResult((int)code, $"An error occurred with code {code}");
        return output;
    }
    
    public static Output Error(this Output output, string errorMessage)
    {
        output.Result = new ErrorResult(-1, errorMessage);
        return output;
    }
    
    public static Output Error(this Output output, HttpStatusCode code, string errorMessage)
    {
        output.Result = new ErrorResult((int)code, errorMessage);
        return output;
    }
    
    public static Output Error(this Output output, Exception ex, int? code = null)
    {
        output.Result = new ErrorResult(code ?? (int)HttpStatusCode.FailedDependency, ex.Message);
        return output;
    }
    
    public static Output Error(this Output output, ValidationResult validationResult)
    {
        output.Result = new ValidationErrorResult(
            (int)HttpStatusCode.BadRequest, 
            "Validation failed",
            validationResult.Errors.Select(e => e.ErrorMessage).ToList());
       
        return output;
    }
}