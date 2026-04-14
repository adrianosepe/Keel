using System.Runtime.CompilerServices;

namespace Keel.Domain.CleanCode.Async;

public static class AsyncExtensions
{
    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        await using (cancellationToken.Register(s => ((TaskCompletionSource<bool>?)s)?.TrySetResult(result: true), tcs))
        {
            if (task != await Task.WhenAny(task, tcs.Task))
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }

        return task.Result;
    }

    public static ConfiguredTaskAwaitable NoSync(this Task task)
    {
        return task.ConfigureAwait(false);
    }
    
    public static ConfiguredTaskAwaitable<T> NoSync<T>(this Task<T> task)
    {
        return task.ConfigureAwait(false);
    }
    
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }
}