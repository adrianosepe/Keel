namespace Keel.Domain.CleanCode.Flow.Action;

public record NotFoundResult : OperationResult
{
    public override bool IsSuccess => true;
}