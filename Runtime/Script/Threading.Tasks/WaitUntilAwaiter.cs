#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Ayla
{
    /// <summary>
    /// Provides an awaiter that completes when a specified predicate returns true or cancellation is requested.
    /// </summary>
    public readonly struct WaitUntilAwaiter : ICriticalNotifyCompletion, INotifyCompletion, IYieldAwaiter
    {
        private readonly Func<bool> m_Predicate;
        private readonly SpinlockConcurrentQueue<YieldAction> m_Continuations;
        private readonly double? m_TimeSlicing;
        private readonly AwaiterExceptionHolder m_ExceptionHolder;
        private readonly CancellationToken m_CancellationToken;

        internal WaitUntilAwaiter(Func<bool> pred, SpinlockConcurrentQueue<YieldAction> continuations, double? timeSlicing, CancellationToken cancellationToken)
        {
            m_Predicate = pred;
            m_Continuations = continuations;
            m_TimeSlicing = timeSlicing;
            m_CancellationToken = cancellationToken;
            if (ApplicationMisc.IsInMainThread())
            {
                m_ExceptionHolder = AwaiterExceptionHolder.Get();
            }
            else
            {
                m_ExceptionHolder = new AwaiterExceptionHolder();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the operation has completed.
        /// </summary>
        public bool IsCompleted => m_CancellationToken.IsCancellationRequested;

        /// <summary>
        /// Gets the time slicing value.
        /// </summary>
        public double? TimeSlicing => m_TimeSlicing;

        /// <summary>
        /// Gets the result of the asynchronous operation.
        /// </summary>
        /// <remarks> Throws an exception if the operation was cancelled or failed. </remarks>
        public void GetResult()
        {
            var capture = m_ExceptionHolder.ConsumeCapture();
            if (m_ExceptionHolder.IsPooled)
            {
                Asserts.True(ApplicationMisc.IsInMainThread());
                AwaiterExceptionHolder.Release(m_ExceptionHolder);
            }
            m_CancellationToken.ThrowIfCancellationRequested();
            capture?.Throw();
        }

        /// <summary>
        /// Schedules the continuation action that's invoked when the operation completes.
        /// </summary>
        /// <param name="continuation"> The action to invoke when the operation completes. </param>
        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation to execute when the predicate returns true or cancellation is requested.
        /// </summary>
        /// <remarks> Does not capture the execution context. </remarks>
        /// <param name="continuation"> The action to invoke when the operation completes. </param>
        public void UnsafeOnCompleted(Action continuation)
        {
            if (m_CancellationToken.IsCancellationRequested)
            {
                continuation();
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var pred = m_Predicate;
                var exceptionHolder = m_ExceptionHolder;
                var token = m_CancellationToken;

                void Call()
                {
                    if (token.IsCancellationRequested)
                    {
                        continuation();
                        return;
                    }

                    bool result;
                    try
                    {
                        result = pred();
                    }
                    catch (Exception ex)
                    {
                        exceptionHolder.Capture(ex);
                        continuation();
                        return;
                    }

                    if (result)
                    {
                        continuation();
                    }
                    else
                    {
                        EditorApplication.delayCall += () => Call();
                    }
                }

                EditorApplication.delayCall += () => Call();
                return;
            }
#endif

            {
                var pred = m_Predicate;
                var continuations = m_Continuations;
                var exceptionHolder = m_ExceptionHolder;
                var token = m_CancellationToken;
                var timeSlicing = m_TimeSlicing;

#pragma warning disable IDE0039
                Action? call = null;
#pragma warning restore IDE0039
                call = () =>
                {
                    if (token.IsCancellationRequested)
                    {
                        continuation();
                        return;
                    }

                    bool result;
                    try
                    {
                        result = pred();
                    }
                    catch (Exception ex)
                    {
                        exceptionHolder.Capture(ex);
                        continuation();
                        return;
                    }

                    if (result)
                    {
                        continuation();
                    }
                    else
                    {
                        continuations.Add(new YieldAction(call!, timeSlicing));
                    }
                };

                continuations.Add(new YieldAction(call, m_TimeSlicing));
            }
        }
    }
}
