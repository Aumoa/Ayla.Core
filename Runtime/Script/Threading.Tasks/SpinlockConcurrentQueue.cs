#nullable enable

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Ayla
{
    internal class SpinlockConcurrentQueue<T>
    {
        private readonly SpinLock m_Lock = new(enableThreadOwnerTracking: false);
        private readonly List<T> m_Items = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T action)
        {
            bool lockTaken = false;
            m_Lock.Enter(ref lockTaken);
            try
            {
                m_Items.Add(action);
            }
            finally
            {
                m_Lock.Exit(useMemoryBarrier: false);
            }
        }

        public int Count => m_Items.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRangeFirst<U>(U items) where U : IEnumerable<T>
        {
            bool lockTaken = false;
            m_Lock.Enter(ref lockTaken);
            try
            {
                m_Items.InsertRange(0, items);
            }
            finally
            {
                m_Lock.Exit(useMemoryBarrier: false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToAndClear(List<T> outputBuffer)
        {
            bool lockTaken = false;
            m_Lock.Enter(ref lockTaken);
            try
            {
                outputBuffer.AddRange(m_Items);
                m_Items.Clear();
            }
            finally
            {
                m_Lock.Exit(useMemoryBarrier: false);
            }
        }
    }
}
