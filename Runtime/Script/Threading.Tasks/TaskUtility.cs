#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Ayla
{
    /// <summary>
    /// Provides utility methods for asynchronously waiting on tasks with support for cancellation tokens.
    /// </summary>
    /// <remarks>The methods in this class enable awaiting tasks while allowing the wait operation to be canceled,
    /// which is useful for creating responsive applications that can react to cancellation requests. These utilities ensure
    /// that the calling code can handle task completion, cancellation, or exceptions in a consistent manner.</remarks>
    public static class TaskUtility
    {
        internal readonly struct TimeUpdateExecutor
        {
        }

        internal readonly struct InitializationExecutor
        {
        }

        internal readonly struct EarlyUpdateExecutor
        {
        }

        internal readonly struct PreUpdateExecutor
        {
        }
    
        internal readonly struct UpdateExecutor
        {
        }

        internal readonly struct FixedUpdateExecutor
        {
        }

        internal readonly struct PreLateUpdateExecutor
        {
        }

        internal readonly struct PostLateUpdateExecutor
        {
        }

        private struct PlayerLoopSystemHelper
        {
            public Type Type;
            public IList<PlayerLoopSystem> SubSystemList;
            public PlayerLoopSystem.UpdateFunction UpdateDelegate;
            public IntPtr UpdateFunction;
            public IntPtr LoopConditionFunction;

            public PlayerLoopSystemHelper(PlayerLoopSystem source)
            {
                Type = source.type;
                SubSystemList = source.subSystemList;
                UpdateDelegate = source.updateDelegate;
                UpdateFunction = source.updateFunction;
                LoopConditionFunction = source.loopConditionFunction;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            using (new TimeLogScope("Initialize update loops took {0}"))
            {
                var system = PlayerLoop.GetCurrentPlayerLoop();
                for (int i = 0; i < system.subSystemList.Length; ++i)
                {
                    ref var s = ref system.subSystemList[i];
                    if (s.type == typeof(TimeUpdate))
                    {
                        AddExecutor<TimeUpdateExecutor>(ref s);
                    }
                    else if (s.type == typeof(Initialization))
                    {
                        AddExecutor<InitializationExecutor>(ref s);
                    }
                    else if (s.type == typeof(EarlyUpdate))
                    {
                        AddExecutor<EarlyUpdateExecutor>(ref s);
                    }
                    else if (s.type == typeof(PreUpdate))
                    {
                        AddExecutor<PreUpdateExecutor>(ref s);
                    }
                    else if (s.type == typeof(Update))
                    {
                        AddExecutor<UpdateExecutor>(ref s);
                    }
                    else if (s.type == typeof(FixedUpdate))
                    {
                        AddExecutor<FixedUpdateExecutor>(ref s);
                    }
                    else if (s.type == typeof(PreLateUpdate))
                    {
                        AddExecutor<PreLateUpdateExecutor>(ref s);
                    }
                    else if (s.type == typeof(PostLateUpdate))
                    {
                        AddExecutor<PostLateUpdateExecutor>(ref s);
                    }
                }
                PlayerLoop.SetPlayerLoop(system);

                return;

                static void AddExecutor<TExecutor>(ref PlayerLoopSystem system)
                {
                    var oldArray = system.subSystemList;
                    var newArray = new PlayerLoopSystem[oldArray.Length + 1];
                    Array.Copy(oldArray, newArray, oldArray.Length);
                    newArray[oldArray.Length] = new PlayerLoopSystem
                    {
                        type = typeof(PlayerLoopTimingExecutor<TExecutor>),
                        updateDelegate = PlayerLoopTimingExecutor<TExecutor>.Call
                    };
                    system.subSystemList = newArray;
                }
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
        /// Returns an awaitable object that yields execution to allow other tasks to run, enabling asynchronous code to
        /// resume later on the current context.
        /// </summary>
        /// <remarks>Use this method in asynchronous programming to temporarily yield control, allowing the
        /// scheduler to process other work before resuming execution. This is useful for improving responsiveness and
        /// avoiding blocking the current thread.</remarks>
        /// <param name="cancellationToken">An optional cancellation token that can be used to cancel the yield operation. If not specified, the default
        /// value is used.</param>
        /// <returns>A <see cref="YieldAwaitable"/> instance that represents the yield operation.</returns>
        public static YieldAwaitable Yield(CancellationToken cancellationToken = default)
        {
            return YieldExecutor.GetAwaitable(cancellationToken);
        }

        /// <summary>
        /// Returns an awaitable object that yields execution to the player loop at the specified timing.
        /// </summary>
        /// <remarks>Use this method to schedule asynchronous operations to resume at a specific point in the
        /// Unity player loop. This is useful for controlling execution flow in game logic or systems that require
        /// synchronization with Unity's update cycles.</remarks>
        /// <param name="timing">The player loop timing at which to yield control. Must be a valid value of the PlayerLoopTiming enumeration.</param>
        /// <param name="cancellationToken">An optional cancellation token that can be used to cancel the yield operation before it resumes.</param>
        /// <returns>A YieldAwaitable that represents the asynchronous operation of yielding control to the player loop.</returns>
        /// <exception cref="ArgumentException">Thrown if timing is not a valid PlayerLoopTiming value.</exception>
        public static YieldAwaitable Yield(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return timing switch
            {
                PlayerLoopTiming.TimeUpdate => PlayerLoopTimingExecutor<TimeUpdateExecutor>.GetAwaitable(cancellationToken),
                PlayerLoopTiming.Initialization => PlayerLoopTimingExecutor<InitializationExecutor>.GetAwaitable(cancellationToken),
                PlayerLoopTiming.EarlyUpdate => PlayerLoopTimingExecutor<EarlyUpdateExecutor>.GetAwaitable(cancellationToken),
                PlayerLoopTiming.FixedUpdate => PlayerLoopTimingExecutor<FixedUpdateExecutor>.GetAwaitable(cancellationToken),
                PlayerLoopTiming.PreUpdate => PlayerLoopTimingExecutor<PreUpdateExecutor>.GetAwaitable(cancellationToken),
                PlayerLoopTiming.Update => PlayerLoopTimingExecutor<UpdateExecutor>.GetAwaitable(cancellationToken),
                PlayerLoopTiming.PreLateUpdate => PlayerLoopTimingExecutor<PreLateUpdateExecutor>.GetAwaitable(cancellationToken),
                PlayerLoopTiming.PostLateUpdate => PlayerLoopTimingExecutor<PostLateUpdateExecutor>.GetAwaitable(cancellationToken),
                _ => throw new ArgumentException(nameof(timing)),
            };
        }

        /// <summary>
        /// Asynchronously waits until the specified predicate returns <see langword="true"/> or the operation is canceled.
        /// </summary>
        /// <remarks>This method repeatedly evaluates the predicate and yields control to allow other operations
        /// to run while waiting. It is useful for scenarios where code execution should pause until a condition is met,
        /// without blocking the calling thread.</remarks>
        /// <param name="pred">A function that determines whether the wait should end. The method continues waiting until this function returns
        /// <see langword="true"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation. The default value is <see
        /// cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        public static WaitUntilAwaitable WaitUntil(Func<bool> pred, CancellationToken cancellationToken = default)
        {
            return new WaitUntilAwaitable(pred, YieldExecutor.GetQueue(), cancellationToken);
        }

        /// <summary>
        /// Waits asynchronously while the specified predicate returns <see langword="true"/>, supporting cancellation.
        /// </summary>
        /// <remarks>Use this method to asynchronously wait for a condition to become <see langword="false"/>. The
        /// operation can be cancelled by providing a cancellation token that is signaled before the condition is
        /// met.</remarks>
        /// <param name="pred">A function that is evaluated repeatedly; the wait continues as long as this function returns <see
        /// langword="true"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation.</param>
        /// <returns>A <see cref="WaitUntilAwaitable"/> that represents the asynchronous wait operation.</returns>
        public static WaitUntilAwaitable WaitWhile(Func<bool> pred, CancellationToken cancellationToken = default)
        {
            return WaitUntil(() => !pred(), cancellationToken);
        }

        /// <summary>
        /// Asynchronously waits until the specified predicate returns <see langword="true"/>, yielding control at each
        /// interval determined by the provided timing context.
        /// </summary>
        /// <remarks>Use this method to implement polling mechanisms that require periodic checks for a condition
        /// to become <see langword="true"/>. Ensure that the predicate function is side-effect free to avoid unintended
        /// behavior. The method yields control according to the specified timing, which can help maintain responsiveness in
        /// event-driven or game loop scenarios.</remarks>
        /// <param name="timing">The timing context that specifies when to yield control during the wait operation.</param>
        /// <param name="pred">A function that evaluates to <see langword="true"/> when the wait should end. The method continues waiting while
        /// this function returns <see langword="false"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation before the predicate returns <see
        /// langword="true"/>.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        public static WaitUntilAwaitable WaitUntil(PlayerLoopTiming timing, Func<bool> pred, CancellationToken cancellationToken = default)
        {
            var queue = timing switch
            {
                PlayerLoopTiming.TimeUpdate => PlayerLoopTimingExecutor<TimeUpdateExecutor>.GetQueue(),
                PlayerLoopTiming.Initialization => PlayerLoopTimingExecutor<InitializationExecutor>.GetQueue(),
                PlayerLoopTiming.EarlyUpdate => PlayerLoopTimingExecutor<EarlyUpdateExecutor>.GetQueue(),
                PlayerLoopTiming.FixedUpdate => PlayerLoopTimingExecutor<FixedUpdateExecutor>.GetQueue(),
                PlayerLoopTiming.PreUpdate => PlayerLoopTimingExecutor<PreUpdateExecutor>.GetQueue(),
                PlayerLoopTiming.Update => PlayerLoopTimingExecutor<UpdateExecutor>.GetQueue(),
                PlayerLoopTiming.PreLateUpdate => PlayerLoopTimingExecutor<PreLateUpdateExecutor>.GetQueue(),
                PlayerLoopTiming.PostLateUpdate => PlayerLoopTimingExecutor<PostLateUpdateExecutor>.GetQueue(),
                _ => throw new ArgumentException(nameof(timing)),
            };
            return new WaitUntilAwaitable(pred, queue, cancellationToken);
        }

        /// <summary>
        /// Asynchronously waits until the specified predicate evaluates to false, using the provided player loop timing.
        /// </summary>
        /// <remarks>Use this method to pause asynchronous execution until a condition is no longer met, such as
        /// waiting for a resource to become available or for a process to finish. The predicate is evaluated on each
        /// iteration of the specified player loop timing. If the cancellation token is triggered before the predicate
        /// returns false, the wait operation is canceled.</remarks>
        /// <param name="timing">The timing configuration that determines when the wait operation is evaluated within the player loop.</param>
        /// <param name="pred">A function that returns a Boolean value. The wait continues while this function returns true and completes when
        /// it returns false.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation before the predicate evaluates to false. The
        /// default value is none.</param>
        /// <returns>A awaitable object that completes when the predicate returns false or the operation is canceled.</returns>
        public static WaitUntilAwaitable WaitWhile(PlayerLoopTiming timing, Func<bool> pred, CancellationToken cancellationToken = default)
        {
            return WaitUntil(timing, () => !pred(), cancellationToken);
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

        public static async ValueTask Create(Func<ValueTask> func)
        {
            await func();
        }

        public static async ValueTask Create(Func<CancellationToken, ValueTask> func, CancellationToken cancellationToken)
        {
            await func(cancellationToken);
        }

        public static async ValueTask WhenAll<T>(T tasks) where T : IEnumerable<ValueTask>
        {
            Debug.Assert(ApplicationMisc.IsInMainThread());
            using var scope1 = ListPool<Exception>.Get(out var exceptions);
            foreach (var task in tasks)
            {
                try
                {
                    await task;
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            switch (exceptions.Count)
            {
                case 1:
                    throw exceptions[0];
                case > 1:
                    throw new AggregateException(exceptions);
            }
        }

        public static async ValueTask<T[]> WhenAll<T>(IEnumerable<ValueTask<T>> tasks)
        {
            Debug.Assert(ApplicationMisc.IsInMainThread());
            using var scope1 = ListPool<T>.Get(out var results);
            using var scope2 = ListPool<Exception>.Get(out var exceptions);

            foreach (var task in tasks)
            {
                try
                {
                    var result = await task;
                    results.Add(result);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            return exceptions.Count switch
            {
                1 => throw exceptions[0],
                > 1 => throw new AggregateException(exceptions),
                _ => results.ToArray(),
            };
        }

        public static async ValueTask WhenAll(params ValueTask[] tasks)
        {
            Debug.Assert(ApplicationMisc.IsInMainThread());
            using var scope1 = ListPool<Exception>.Get(out var exceptions);

            foreach (var task in tasks)
            {
                try
                {
                    await task;
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            switch (exceptions.Count)
            {
                case 1:
                    throw exceptions[0];
                case > 1:
                    throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// Asynchronously waits for the specified task to complete without awaiting its result, effectively "forgetting" the task.
        /// </summary>
        public static async void Forget(this ValueTask task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                if (e is not OperationCanceledException)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static async ValueTask<T[]> WaitAsync<T>(this AsyncInstantiateOperation<T> op, CancellationToken cancellationToken = default) where T : Object
        {
            using (cancellationToken.Register(() => op.Cancel()))
            {
                return await op;
            }
        }

        public static Task WaitAsync(this AsyncOperation op, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<object?> tcs = new();
            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                Create(async () =>
                {
                    try
                    {
                        await op;
                        tcs.TrySetResult(null);
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetException(e);
                    }
                }).Forget();
            }

            return tcs.Task;
        }
    }
}
