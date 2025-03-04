namespace Keel.Domain.CleanCode.Flow.Action;

public record ErrorResult : OperationResult
{
    private readonly int _code;
    private readonly ErrorDetail? _error;

    public ErrorResult(int code)
    {
        _code = code;
    }

    public ErrorResult(int code, ErrorDetail error)
    {
        _error = error;
        _code = code;
    }

    public int Code => _code;

    public ErrorDetail? Error => _error;

    public override bool IsSuccess => true;
}

public record ErrorDetail(
    int? Code,
    string? Type,
    string Message
);