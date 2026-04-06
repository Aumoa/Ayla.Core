using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Ayla;

/// <summary>
/// Provides an awaiter that yields execution to allow asynchronous continuations, supporting cancellation and exception
/// propagation.
/// </summary>
/// <remarks>Use this awaiter to schedule continuations that should resume after yielding control, such as when
/// implementing custom asynchronous workflows. The awaiter supports cancellation via a provided cancellation token and
/// aggregates exceptions thrown during continuation execution. It implements both standard and critical notification
/// completion interfaces, enabling integration with the async/await pattern and advanced scheduling
/// scenarios.</remarks>
public readonly struct YieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion
{
    private readonly SpinlockConcurrentQueue<Action> m_Continuations;
    private readonly CancellationToken m_CancellationToken;
    private readonly List<Exception> m_Exceptions = new();

    internal YieldAwaiter(SpinlockConcurrentQueue<Action> continuations, CancellationToken cancellationToken)
    {
        m_Continuations = continuations;
        m_CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets a value indicating whether the operation has completed due to a cancellation request.
    /// </summary>
    /// <remarks>This property returns <see langword="true"/> if the associated cancellation token has
    /// requested cancellation, indicating that the operation should be considered complete. Use this property to check
    /// for cancellation completion when awaiting or polling asynchronous operations.</remarks>
    public bool IsCompleted
    {
        get
        {
            if (m_CancellationToken.IsCancellationRequested)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Throws any exceptions that were recorded during the associated asynchronous operation. If a single exception was
    /// recorded, that exception is thrown; if multiple exceptions were recorded, an AggregateException containing all
    /// exceptions is thrown.
    /// </summary>
    /// <remarks>This method is typically called to observe and propagate exceptions that occurred during an
    /// asynchronous operation. It enables callers to handle exceptions in a consolidated manner after the operation has
    /// completed.</remarks>
    /// <exception cref="AggregateException">Thrown when multiple exceptions have been recorded, encapsulating all of them in an AggregateException.</exception>
    public void GetResult()
    {
        if (m_Exceptions.Count == 1)
        {
            throw m_Exceptions[0];
        }
        else if (m_Exceptions.Count > 1)
        {
            throw new AggregateException(m_Exceptions);
        }

        m_CancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Schedules the specified action to be invoked when the asynchronous operation has completed.
    /// </summary>
    /// <remarks>If the operation supports cancellation, the cancellation token is checked before executing
    /// the continuation. In the Unity Editor when the application is not playing, the continuation is scheduled to run
    /// on the next editor frame. Any exceptions thrown by the continuation are captured for later retrieval.</remarks>
    /// <param name="continuation">The action to execute after the operation completes. Cannot be null.</param>
    public void OnCompleted(Action continuation)
    {
        UnsafeOnCompleted(continuation);
    }

    /// <summary>
    /// Schedules the specified continuation action to be invoked when the asynchronous operation completes, without
    /// capturing the current execution context.
    /// </summary>
    /// <remarks>If the operation supports cancellation, the cancellation token is checked before executing
    /// the continuation. In the Unity Editor outside of play mode, the continuation is scheduled to run on the next
    /// editor frame. Any exceptions thrown by the continuation are captured for later retrieval.</remarks>
    /// <param name="continuation">The action to execute when the operation has finished. This action is not invoked if the operation is canceled.</param>
    public void UnsafeOnCompleted(Action continuation)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var closureCancellationToken = m_CancellationToken;
            var closureExceptions = m_Exceptions;
            EditorApplication.delayCall += () =>
            {
                try
                {
                    closureCancellationToken.ThrowIfCancellationRequested();
                    continuation();
                }
                catch (Exception e)
                {
                    closureExceptions.Add(e);
                }
            };

            return;
        }
#endif

        if (m_CancellationToken.CanBeCanceled)
        {
            var closureCancellationToken = m_CancellationToken;
            var closureExceptions = m_Exceptions;
            m_Continuations.Add(() =>
            {
                try
                {
                    closureCancellationToken.ThrowIfCancellationRequested();
                    continuation();
                }
                catch (Exception e)
                {
                    closureExceptions.Add(e);
                }
            });
        }
        else
        {
            m_Continuations.Add(continuation);
        }
    }
}
