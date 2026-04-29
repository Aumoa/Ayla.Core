#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Ayla
{
    /// <summary>
    /// Provides an awaiter that yields execution to the next player loop frame, supporting cancellation.
    /// </summary>
    /// <remarks>Use this awaiter to suspend an asynchronous method and resume it on the next player loop iteration.
    /// Cancellation is observed synchronously via <see cref="GetResult"/>. It implements both standard and critical
    /// notification completion interfaces, enabling integration with the async/await pattern.</remarks>
    public readonly struct YieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        private readonly SpinlockConcurrentQueue<Action> m_Continuations;
        private readonly CancellationToken m_CancellationToken;

        internal YieldAwaiter(SpinlockConcurrentQueue<Action> continuations, CancellationToken cancellationToken)
        {
            m_Continuations = continuations;
            m_CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets a value indicating whether the operation has already completed synchronously due to a cancellation request.
        /// </summary>
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
        /// Completes the await operation, throwing if cancellation was requested.
        /// </summary>
        /// <exception cref="OperationCanceledException">Thrown when the associated cancellation token has been cancelled.</exception>
        public void GetResult()
        {
            m_CancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Schedules the continuation to be invoked on the next player loop frame.
        /// </summary>
        /// <param name="continuation">The action to execute on the next frame.</param>
        public void OnCompleted(Action continuation)
        {
            UnsafeOnCompleted(continuation);
        }

        /// <summary>
        /// Schedules the continuation to be invoked on the next player loop frame, without capturing the execution context.
        /// </summary>
        /// <remarks>In the Unity Editor outside of play mode, the continuation is scheduled via
        /// <see cref="EditorApplication.delayCall"/> to run on the next editor frame.</remarks>
        /// <param name="continuation">The action to execute on the next frame.</param>
        public void UnsafeOnCompleted(Action continuation)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += () => continuation();

                return;
            }
#endif

            m_Continuations.Add(continuation);
        }
    }
}
