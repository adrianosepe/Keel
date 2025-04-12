namespace Keel.Domain.CleanCode.Flow.Data;

public interface IInputWithCorrelationId
{
    Guid CorrelationId { get; }
}