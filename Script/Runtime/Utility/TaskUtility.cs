using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ayla;

/// <summary>
/// Provides utility methods for asynchronously waiting on tasks with support for cancellation tokens.
/// </summary>
/// <remarks>The methods in this class enable awaiting tasks while allowing the wait operation to be canceled,
/// which is useful for creating responsive applications that can react to cancellation requests. These utilities ensure
/// that the calling code can handle task completion, cancellation, or exceptions in a consistent manner.</remarks>
public static class TaskUtility
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize()
    {
        var system = PlayerLoop.GetCurrentPlayerLoop();
        var initializationIndex = Array.FindIndex(system.subSystemList, s => s.type == typeof(Initialization));
        Debug.Assert(initializationIndex != -1);
        ref var initializationSubsystem = ref system.subSystemList[initializationIndex];
        initializationSubsystem.subSystemList = initializationSubsystem.subSystemList.Append(new PlayerLoopSystem
        {
            type = typeof(OnInitialization),
            updateDelegate = OnInitialization.Call
        }).ToArray();
        PlayerLoop.SetPlayerLoop(system);
    }

    public readonly struct OnInitialization
    {
        private static readonly List<Action> s_Continuations = new();
        private static readonly List<Action> s_ExecutionBuffer = new();

        internal static void Call()
        {
            lock (s_Continuations)
            {
                s_ExecutionBuffer.AddRange(s_Continuations);
                s_Continuations.Clear();
            }

            int executions = 0;
            try
            {
                foreach (var execution in s_ExecutionBuffer)
                {
                    execution();
                    ++executions;
                }
            }
            finally
            {
                s_ExecutionBuffer.RemoveRange(0, executions);
            }
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private readonly CancellationToken m_CancellationToken;

            public Awaiter(CancellationToken cancellationToken)
            {
                m_CancellationToken = cancellationToken;
            }

            public bool IsCompleted => false;

            public void GetResult()
            {
            }

            public void OnCompleted(Action continuation)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    var closureCancellationToken = m_CancellationToken;
                    EditorApplication.delayCall += () =>
                    {
                        closureCancellationToken.ThrowIfCancellationRequested();
                        continuation();
                    };

                    return;
                }
#endif

                lock (s_Continuations)
                {
                    if (m_CancellationToken.CanBeCanceled)
                    {
                        var closureCancellationToken = m_CancellationToken;
                        s_Continuations.Add(() =>
                        {
                            closureCancellationToken.ThrowIfCancellationRequested();
                            continuation();
                        });
                    }
                    else
                    {
                        s_Continuations.Add(continuation);
                    }
                }
            }

            public void UnsafeOnCompleted(Action continuation)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    var closureCancellationToken = m_CancellationToken;
                    EditorApplication.delayCall += () =>
                    {
                        closureCancellationToken.ThrowIfCancellationRequested();
                        continuation();
                    };

                    return;
                }
#endif

                lock (s_Continuations)
                {
                    if (m_CancellationToken.CanBeCanceled)
                    {
                        var closureCancellationToken = m_CancellationToken;
                        s_Continuations.Add(() =>
                        {
                            closureCancellationToken.ThrowIfCancellationRequested();
                            continuation();
                        });
                    }
                    else
                    {
                        s_Continuations.Add(continuation);
                    }
                }
            }
        }

        public readonly struct Awaitable
        {
            private readonly CancellationToken m_CancellationToken;

            public Awaitable(CancellationToken cancellationToken)
            {
                m_CancellationToken = cancellationToken;
            }

            public Awaiter GetAwaiter()
            {
                return new Awaiter(m_CancellationToken);
            }
        }
    }

    public static OnInitialization.Awaitable YieldInitialization(CancellationToken cancellationToken = default)
    {
        return new OnInitialization.Awaitable(cancellationToken);
    }

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
