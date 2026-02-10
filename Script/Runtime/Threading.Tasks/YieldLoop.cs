using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Ayla;

/// <summary>
/// Provides frame-aware loop execution with automatic yielding to maintain target frame rate.
/// </summary>
/// <remarks>
/// All methods yield control back to Unity's player loop when execution time approaches the frame budget,
/// preventing frame rate drops while processing large datasets or time-consuming operations.
/// Default yield time is calculated from Application.targetFrameRate or screen refresh rate.
/// </remarks>
public static class YieldLoop
{
    private static double s_DefaultYield;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize()
    {
        using (new TimeLogScope("Initialize default yield tooks {0}"))
        {
            double fps = Application.targetFrameRate;
            if (fps <= 0)
            {
                fps = Screen.currentResolution.refreshRateRatio.value;
            }

            s_DefaultYield = 1000.0 / fps;
        }
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void InitializeEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Initialize();
        }
    }
#endif

    /// <summary>
    /// Executes an action for each integer in the range, yielding when time limit is exceeded.
    /// </summary>
    /// <param name="fromInclusive">Starting index (inclusive).</param>
    /// <param name="toExclusive">Ending index (exclusive).</param>
    /// <param name="limitMilliseconds">Maximum execution time before yielding (ms).</param>
    /// <param name="body">Action to execute for each index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async ValueTask For(int fromInclusive, int toExclusive, double limitMilliseconds, Action<int> body, CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        for (int i = fromInclusive; i < toExclusive; i++)
        {
            body(i);
            if (timer.Elapsed.TotalMilliseconds >= limitMilliseconds)
            {
                await TaskUtility.Yield(PlayerLoopTiming.Initialization, cancellationToken);
                timer.Restart();
            }
        }
    }

    /// <summary>
    /// Executes an action for each integer in the range, using default frame-based yield timing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask For(int fromInclusive, int toExclusive, Action<int> body, CancellationToken cancellationToken = default)
        => For(fromInclusive, toExclusive, s_DefaultYield, body, cancellationToken);

    /// <summary>
    /// Executes a function for each integer in the range, collecting results into an array.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="limitMilliseconds">Maximum execution time before yielding (ms).</param>
    /// <param name="body">Function to execute for each index.</param>
    /// <returns>Array of results.</returns>
    public static async ValueTask<T[]> For<T>(int fromInclusive, int toExclusive, double limitMilliseconds, Func<int, T> body, CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        using var scope1 = ListPool<T>.Get(out var results);
        for (int i = fromInclusive; i < toExclusive; i++)
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

    /// <summary>
    /// Executes a function for each integer in the range, using default frame-based yield timing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<T[]> For<T>(int fromInclusive, int toExclusive, Func<int, T> body, CancellationToken cancellationToken = default)
        => For(fromInclusive, toExclusive, s_DefaultYield, body, cancellationToken);

    /// <summary>
    /// Executes an action for each element in the collection, yielding when time limit is exceeded.
    /// </summary>
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

    /// <summary>
    /// Executes an action for each element, using default frame-based yield timing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask ForEach<T>(IEnumerable<T> enumerable, Action<T> body, CancellationToken cancellationToken = default)
        => ForEach(enumerable, s_DefaultYield, body, cancellationToken);

    /// <summary>
    /// Executes a function for each element in the collection, collecting results into an array.
    /// </summary>
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

    /// <summary>
    /// Executes a function for each element, using default frame-based yield timing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<U[]> ForEach<T, U>(IEnumerable<T> enumerable, Func<T, U> body, CancellationToken cancellationToken = default)
        => ForEach(enumerable, s_DefaultYield, body, cancellationToken);
}
