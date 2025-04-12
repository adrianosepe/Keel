namespace Keel.Domain.CleanCode.Flow.Action;

public record NoContentResult : OperationResult
{
    public override bool IsSuccess => true;
}