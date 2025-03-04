using Keel.Domain.CleanCode.Flow.Action;
using Microsoft.Extensions.Logging;

namespace Keel.Domain.CleanCode.Flow.UseCase;

public abstract class UseCase<TInput>(ILogger<UseCase<TInput>> logger) : IUseCase<TInput>
{
    private IDisposable? _scope;

    protected abstract void InternalBegin(TInput input, out IDictionary<string, object?>? scopedVariables);

    public async Task<Output> HandleAsync(TInput input, CancellationToken cancellationToken)
    {
        InternalBegin(input, out var scopedVariables);

        try
        {
            if (scopedVariables is not null)
            {
                _scope = logger.BeginScope(scopedVariables);
            }

            return await InternalHandleAsync(input, cancellationToken);
        }
        finally
        {
            _scope?.Dispose();
        }
    }

    protected abstract Task<Output> InternalHandleAsync(TInput input, CancellationToken cancellationToken);
}