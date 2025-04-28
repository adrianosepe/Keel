namespace Keel.Domain.CleanCode.Flow.Action;

public sealed class Output
{
    // ReSharper disable MemberCanBePrivate.Global
    public static OperationResult NoContentResult { get; set; } = new NoContentResult();
    // ReSharper restore MemberCanBePrivate.Global

    public OperationResult Result { get; set; } = NoContentResult;

    public bool IsSuccess => Result.IsSuccess;
    public bool IsFail => Result.IsFail;
}