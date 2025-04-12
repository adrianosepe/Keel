using Keel.Domain.CleanCode.Flow.Data;
using Microsoft.Extensions.Logging;

namespace Keel.Domain.CleanCode.Flow.UseCase;

public abstract class UseCaseWithCorrelationId<TInput>(
    ILogger<UseCaseWithCorrelationId<TInput>> logger)
    : UseCase<TInput>(logger) where TInput : class, IInputWithCorrelationId
{
    protected override void InternalBegin(TInput input, out IDictionary<string, object?>? scopedVariables)
    {
        scopedVariables = new Dictionary<string, object?>
            {
                { "CorrelationId", input.CorrelationId },
            };
    }
}