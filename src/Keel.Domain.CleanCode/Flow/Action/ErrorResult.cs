namespace Keel.Domain.CleanCode.Flow.Action;

public record ErrorResult(int Code, string Message, string? Flag = null) : OperationResult
{
    public override bool IsSuccess => false;
}