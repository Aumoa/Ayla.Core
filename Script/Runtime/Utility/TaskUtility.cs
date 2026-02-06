using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ayla;

/// <summary>
/// Provides utility methods for asynchronously waiting on tasks with support for cancellation tokens.
/// </summary>
/// <remarks>The methods in this class enable awaiting tasks while allowing the wait operation to be canceled,
/// which is useful for creating responsive applications that can react to cancellation requests. These utilities ensure
/// that the calling code can handle task completion, cancellation, or exceptions in a consistent manner.</remarks>
public static class TaskUtility
{
    /// <summary>
    /// Waits asynchronously for the specified task to complete, allowing the wait operation to be canceled using a
    /// cancellation token.
    /// </summary>
    /// <remarks>If the cancellation token is canceled before the task completes, the returned task will be
    /// canceled. If the specified task faults, the returned task will complete with the same exception.</remarks>
    /// <param name="task">The task to wait for completion. This parameter must not be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation. If cancellation is not supported, the method
    /// returns the original task.</param>
    /// <returns>A task that represents the asynchronous wait operation. The returned task completes when the specified task
    /// completes or is canceled if the cancellation token is triggered.</returns>
    public static Task WaitAsync(this Task task, CancellationToken cancellationToken = default)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            return task;
        }

        var tcs = new TaskCompletionSource<object?>(cancellationToken);
        _ = task.ContinueWith(r =>
        {
            try
            {
                r.GetAwaiter().GetResult();
                tcs.SetResult(null);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        }, cancellationToken);

        return tcs.Task;
    }

    /// <summary>
    /// Waits asynchronously for the specified task to complete, supporting cancellation via a provided token.
    /// </summary>
    /// <remarks>If the cancellation token is canceled before the task completes, the returned task will
    /// complete with a TaskCanceledException. This method is useful for scenarios where you need to await a task while
    /// allowing for cancellation.</remarks>
    /// <typeparam name="T">The type of the result produced by the task.</typeparam>
    /// <param name="task">The task to wait for completion. Must not be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation. If cancellation is not supported, the method
    /// returns the original task immediately.</param>
    /// <returns>A task that represents the asynchronous wait operation. The returned task completes with the result of the
    /// original task, or is canceled if the cancellation token is triggered before completion.</returns>
    public static Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken = default)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            return task;
        }

        var tcs = new TaskCompletionSource<T>(cancellationToken);
        _ = task.ContinueWith(r =>
        {
            try
            {
                var result = r.GetAwaiter().GetResult();
                tcs.SetResult(result);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        }, cancellationToken);

        return tcs.Task;
    }
}
