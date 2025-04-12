using Keel.Domain.CleanCode.Flow.Action;

namespace Keel.Domain.CleanCode.Flow.UseCase;

public interface IUseCase<in TInput>
{
    Task<Output> HandleAsync(TInput input, CancellationToken cancellationToken);
}