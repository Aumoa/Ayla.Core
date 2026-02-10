using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Pool;

namespace Ayla;

public static class YieldLoop
{
    public static async ValueTask For(int fromInclusive, int toExclusive, double limitMilliseconds, Action<int> body, CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        for (int i = fromInclusive; i <= toExclusive; i++)
        {
            body(i);
            if (timer.Elapsed.TotalMilliseconds >= limitMilliseconds)
            {
                await TaskUtility.Yield(PlayerLoopTiming.Initialization, cancellationToken);
                timer.Restart();
            }
        }
    }

    public static async ValueTask<T[]> For<T>(int fromInclusive, int toExclusive, double limitMilliseconds, Func<int, T> body, CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        using var scope1 = ListPool<T>.Get(out var results);
        for (int i = fromInclusive; i <= toExclusive; i++)
        {
            results.Add(body(i));
            if (timer.Elapsed.TotalMilliseconds >= limitMilliseconds)
            {
                await TaskUtility.Yield(PlayerLoopTiming.Initialization, cancellationToken);
                timer.Restart();
            }
        }

        return results.ToArray();
    }

    public static async ValueTask ForEach<T>(IEnumerable<T> enumerable, double limitMilliseconds, Action<T> body, CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        foreach (var item in enumerable)
        {
            body(item);
            if (timer.Elapsed.TotalMilliseconds >= limitMilliseconds)
            {
                await TaskUtility.Yield(PlayerLoopTiming.Initialization, cancellationToken);
                timer.Restart();
            }
        }
    }

    public static async ValueTask<U[]> ForEach<T, U>(IEnumerable<T> enumerable, double limitMilliseconds, Func<T, U> body, CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        using var scope1 = ListPool<U>.Get(out var results);
        foreach (var item in enumerable)
        {
            results.Add(body(item));
            if (timer.Elapsed.TotalMilliseconds >= limitMilliseconds)
            {
                await TaskUtility.Yield(PlayerLoopTiming.Initialization, cancellationToken);
                timer.Restart();
            }
        }

        return results.ToArray();
    }
}
