#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Ayla
{
    public readonly struct WaitUntilAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        private readonly Func<bool> m_Predicate;
        private readonly SpinlockConcurrentQueue<Action> m_Continuations;
        private readonly CancellationToken m_CancellationToken;
        private readonly AwaiterExceptionHolder m_ExceptionHolder;

        internal WaitUntilAwaiter(Func<bool> pred, SpinlockConcurrentQueue<Action> continuations, CancellationToken cancellationToken)
        {
            m_Predicate = pred;
            m_Continuations = continuations;
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

        public bool IsCompleted => m_CancellationToken.IsCancellationRequested;

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

        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

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
                        continuations.Add(call!);
                    }
                };

                continuations.Add(call);
            }
        }
    }
}
