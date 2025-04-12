namespace Keel.Domain.CleanCode.Flow.Action;

public record DataResult(int Code, object? Data) : OperationResult
{
    public override bool IsSuccess => true;

}