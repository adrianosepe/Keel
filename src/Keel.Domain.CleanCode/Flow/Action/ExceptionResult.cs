namespace Keel.Domain.CleanCode.Flow.Action;

public record ExceptionResult(int Code, Exception Exception) : ErrorResult(Code, Exception.Message);