#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Ayla
{
    internal static class ExecutionTimingExecutor<T>
    {
        private static readonly SpinlockConcurrentQueue<YieldAction> s_Continuations = new();
        private static readonly List<YieldAction> s_ExecutionBuffer = new();
        private static readonly Stopwatch s_Stopwatch = Stopwatch.StartNew();

        internal static void Call()
        {
            var time = s_Stopwatch.Elapsed;
            s_Continuations.CopyToAndClear(s_ExecutionBuffer);

            try
            {
                YieldExecutor.Call(s_Stopwatch);

                for (int i = 0; i < s_ExecutionBuffer.Count; i++)
                {
                    var execution = s_ExecutionBuffer[i];
                    if (execution.TimeSlicing.HasValue == false || s_Stopwatch.Elapsed.TotalMilliseconds <= execution.TimeSlicing.Value)
                    {
                        execution.Work();
                        s_ExecutionBuffer.RemoveAt(i);
                        i--;
                    }
                }

                var deltaTime = s_Stopwatch.Elapsed - time;

                // Restart the stopwatch at the end to accurately measure the actual elapsed time for the next frame.
                s_Stopwatch.Restart();
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
