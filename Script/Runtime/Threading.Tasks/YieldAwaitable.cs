using System;
using System.Threading;

namespace Ayla;

/// <summary>
/// Provides an awaitable structure that enables asynchronous methods to yield execution and resume later, allowing
/// cooperative multitasking without blocking the calling thread.
/// </summary>
/// <remarks>Use this type to yield control within an asynchronous method, typically to allow other queued work to
/// execute before resuming. The structure is designed to work with a queue of continuations and a cancellation token,
/// supporting scenarios where fine-grained control over task scheduling and cancellation is required. This is
/// particularly useful in custom task schedulers or advanced asynchronous frameworks.</remarks>
public readonly struct YieldAwaitable
{
    private readonly SpinlockConcurrentQueue<Action> m_Continuations;
    private readonly CancellationToken m_CancellationToken;

    internal YieldAwaitable(SpinlockConcurrentQueue<Action> continuations, CancellationToken cancellationToken)
    {
        m_Continuations = continuations;
        m_CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Creates and returns a YieldAwaiter for awaiting the completion of the associated asynchronous operation.
    /// </summary>
    /// <remarks>Use this method in asynchronous programming to yield control back to the calling context
    /// until the operation completes. This is typically used with the await keyword to enable non-blocking
    /// execution.</remarks>
    /// <returns>A YieldAwaiter instance that can be used to await the completion of the operation.</returns>
    public YieldAwaiter GetAwaiter()
    {
        return new YieldAwaiter(m_Continuations, m_CancellationToken);
    }
}
