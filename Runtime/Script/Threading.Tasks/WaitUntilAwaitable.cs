#nullable enable

using System;
using System.Threading;

namespace Ayla
{
    /// <summary>
    /// Represents an awaitable structure that enables asynchronous waiting until a specified condition is met.
    /// </summary>
    /// <remarks>Use this struct with the await keyword to asynchronously wait until the provided predicate returns
    /// <see langword="true"/>. The wait operation can be canceled using a cancellation token. This type is intended for
    /// scenarios where code execution should be suspended until a particular condition is satisfied, without blocking the
    /// calling thread.</remarks>
    public readonly struct WaitUntilAwaitable
    {
        private readonly Func<bool> m_Predicate;
        private readonly SpinlockConcurrentQueue<YieldAction> m_Continuations;
        private readonly double? m_TimeSlicing;
        private readonly CancellationToken m_CancellationToken;

        internal WaitUntilAwaitable(Func<bool> pred, SpinlockConcurrentQueue<YieldAction> continuations, double? timeSlicing, CancellationToken cancellationToken)
        {
            m_Predicate = pred;
            m_Continuations = continuations;
            m_TimeSlicing = timeSlicing;
            m_CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Creates and returns an awaiter that can be used to asynchronously wait until the specified condition is met.
        /// </summary>
        /// <remarks>Use this method to integrate custom wait conditions into asynchronous workflows. The returned
        /// awaiter allows code to pause execution until the predicate evaluates to <see langword="true"/> or the operation
        /// is canceled. Ensure that the predicate and any continuations are thread-safe and do not block the calling
        /// thread.</remarks>
        /// <returns>A <see cref="WaitUntilAwaiter"/> instance that enables awaiting the completion of the condition defined by the
        /// predicate.</returns>
        public WaitUntilAwaiter GetAwaiter()
        {
            return new WaitUntilAwaiter(m_Predicate, m_Continuations, m_TimeSlicing, m_CancellationToken);
        }

        /// <summary>
        /// Configures the awaitable with the specified time slicing behavior.
        /// </summary>
        /// <param name="timeSlicing"> A value indicating the time slicing duration for the continuation. </param>
        /// <returns> A configured <see cref="WaitUntilAwaitable"/> instance. </returns>
        public WaitUntilAwaitable ConfigureAwait(double? timeSlicing = null)
        {
            return new WaitUntilAwaitable(m_Predicate, m_Continuations, timeSlicing, m_CancellationToken);
        }
    }
}
