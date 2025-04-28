namespace Keel.Domain.CleanCode.Flow.Action;

public record ValidationErrorResult(
    int Code, string Message, List<string> ValidationErrors, string? Flag = null)
    : ErrorResult(Code, Message, Flag)
{
}