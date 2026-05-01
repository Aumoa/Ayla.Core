#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Ayla
{
    internal static class YieldExecutor
    {
        private static readonly SpinlockConcurrentQueue<YieldAction> s_Continuations = new();
        private static readonly List<YieldAction> s_ExecutionBuffer = new();

        internal static void Call(Stopwatch timer)
        {
            s_Continuations.CopyToAndClear(s_ExecutionBuffer);

            try
            {
                for (int i = 0; i < s_ExecutionBuffer.Count; i++)
                {
                    var execution = s_ExecutionBuffer[i];
                    if (execution.TimeSlicing.HasValue == false || timer.Elapsed.TotalMilliseconds <= execution.TimeSlicing.Value)
                    {
                        execution.Work();
                        s_ExecutionBuffer.RemoveAt(i);
                        i--;
                    }
                }
            }
            finally
            {
                s_Continuations.AddRangeFirst(s_ExecutionBuffer);
                s_ExecutionBuffer.Clear();
            }
        }

        public static YieldAwaitable GetAwaitable(CancellationToken cancellationToken = default)
        {
            return new YieldAwaitable(s_Continuations, null, cancellationToken);
        }

        public static SpinlockConcurrentQueue<YieldAction> GetQueue() => s_Continuations;
    }
}
