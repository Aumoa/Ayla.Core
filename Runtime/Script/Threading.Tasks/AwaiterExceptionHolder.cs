#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Ayla
{
    internal class AwaiterExceptionHolder
    {
        private static readonly List<AwaiterExceptionHolder> s_Pool = new();

        private ExceptionDispatchInfo? m_Exception;

        public bool IsPooled { get; }

        private AwaiterExceptionHolder(bool isPooled)
        {
            IsPooled = isPooled;
        }

        public AwaiterExceptionHolder() : this(false)
        {
        }

        public void Capture(Exception e)
        {
            Asserts.True(m_Exception == null);
            m_Exception = ExceptionDispatchInfo.Capture(e);
        }

        public ExceptionDispatchInfo? ConsumeCapture()
        {
            var capture = m_Exception;
            m_Exception = null;
            return capture;
        }

        public static AwaiterExceptionHolder Get()
        {
            Asserts.True(ApplicationMisc.IsInMainThread());
            if (s_Pool.Count > 0)
            {
                var index = s_Pool.Count - 1;
                var inst = s_Pool[index];
                s_Pool.RemoveAt(index);
                return inst;
            }
            return new AwaiterExceptionHolder(true);
        }

        public static void Release(AwaiterExceptionHolder holder)
        {
            Asserts.True(ApplicationMisc.IsInMainThread());
            Asserts.True(holder.m_Exception == null);
            s_Pool.Add(holder);
        }
    }
}
