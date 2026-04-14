namespace Keel.Domain.CleanCode.Async;

public class AsyncLazy<T>(Func<Task<T>> factory)
{
    private readonly Lazy<Task<T>> _instance = new(() => Task.Run(factory));

    public Task<T> Value => _instance.Value;
}