namespace Keel.Domain.CleanCode.Flow.Action;

public abstract record OperationResult
{
    public abstract bool IsSuccess { get; }
    public bool IsFail => !IsSuccess;
}