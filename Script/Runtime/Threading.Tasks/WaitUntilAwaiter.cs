using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Ayla;

public readonly struct WaitUntilAwaiter : ICriticalNotifyCompletion, INotifyCompletion
{
    private readonly Func<bool> m_Predicate;
    private readonly SpinlockConcurrentQueue<Action> m_Continuations;
    private readonly CancellationToken m_CancellationToken;
    private readonly List<Exception> m_Exceptions = new();

    internal WaitUntilAwaiter(Func<bool> pred, SpinlockConcurrentQueue<Action> continuations, CancellationToken cancellationToken)
    {
        m_Predicate = pred;
        m_Continuations = continuations;
        m_CancellationToken = cancellationToken;
    }

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

    public void GetResult()
    {
        m_CancellationToken.ThrowIfCancellationRequested();

        if (m_Exceptions.Count == 1)
        {
            throw m_Exceptions[0];
        }
        else if (m_Exceptions.Count > 1)
        {
            throw new AggregateException(m_Exceptions);
        }
    }

    public void OnCompleted(Action continuation)
    {
        UnsafeOnCompleted(continuation);
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        if (IsCompleted)
        {
            continuation();
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var closureCancellationToken = m_CancellationToken;
            var closurePredicate = m_Predicate;
            var closureExceptions = m_Exceptions;
            var closureContinuations = m_Continuations;
            EditorApplication.delayCall += () =>
            {
                try
                {
                    closureCancellationToken.ThrowIfCancellationRequested();
                    if (closurePredicate())
                    {
                        continuation();
                    }
                    else
                    {
                        // Predicate is still false, re-queue for next frame
                        var nextAwaiter = new WaitUntilAwaiter(closurePredicate, closureContinuations, closureCancellationToken);
                        nextAwaiter.UnsafeOnCompleted(continuation);
                    }
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
            var closurePredicate = m_Predicate;
            var closureExceptions = m_Exceptions;
            var closureContinuations = m_Continuations;
            m_Continuations.Add(() =>
            {
                try
                {
                    closureCancellationToken.ThrowIfCancellationRequested();
                    if (closurePredicate())
                    {
                        continuation();
                    }
                    else
                    {
                        // Predicate is still false, re-queue for next frame
                        var nextAwaiter = new WaitUntilAwaiter(closurePredicate, closureContinuations, closureCancellationToken);
                        nextAwaiter.UnsafeOnCompleted(continuation);
                    }
                }
                catch (Exception ex)
                {
                    closureExceptions.Add(ex);
                    continuation();
                }
            });
        }
        else
        {
            var closureCancellationToken = m_CancellationToken;
            var closurePredicate = m_Predicate;
            var closureExceptions = m_Exceptions;
            var closureContinuations = m_Continuations;
            m_Continuations.Add(() =>
            {
                try
                {
                    if (closurePredicate())
                    {
                        continuation();
                    }
                    else
                    {
                        // Predicate is still false, re-queue for next frame
                        var nextAwaiter = new WaitUntilAwaiter(closurePredicate, closureContinuations, closureCancellationToken);
                        nextAwaiter.UnsafeOnCompleted(continuation);
                    }
                }
                catch (Exception ex)
                {
                    closureExceptions.Add(ex);
                    continuation();
                }
            });
        }
    }
}
