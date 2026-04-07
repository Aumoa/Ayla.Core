#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;

namespace Ayla
{
    internal static class PlayerLoopTimingExecutor<T>
    {
        private static readonly SpinlockConcurrentQueue<Action> s_Continuations = new();
        private static readonly List<Action> s_ExecutionBuffer = new();

        internal static void Call()
        {
            s_Continuations.CopyTo(s_ExecutionBuffer);

            int executions = 0;
            try
            {
                YieldExecutor.Call();

                foreach (var execution in s_ExecutionBuffer)
                {
                    ++executions;
                    execution();
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                s_ExecutionBuffer.RemoveRange(0, executions);
            }
        }

        public static YieldAwaitable GetAwaitable(CancellationToken cancellationToken = default)
        {
            return new YieldAwaitable(s_Continuations, cancellationToken);
        }

        public static SpinlockConcurrentQueue<Action> GetQueue() => s_Continuations;
    }
}
