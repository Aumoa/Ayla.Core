#nullable enable

using System;
using UnityEngine;

namespace Ayla
{
    public readonly struct FrameCache<T> : IEquatable<FrameCache<T>>
    {
        public readonly int m_FrameCount;
        public readonly T m_Value;

        public bool HasValue => m_FrameCount == Time.frameCount;

        public T Value
        {
            get
            {
                if (HasValue == false)
                {
                    throw new InvalidOperationException();
                }

                return m_Value;
            }
        }

        private FrameCache(int frameCount, T value)
        {
            m_FrameCount = frameCount;
            m_Value = value;
        }

        public FrameCache(T value) : this(Time.frameCount, value)
        {
        }

        public override string ToString()
        {
            if (HasValue)
            {
                return m_Value?.ToString() ?? string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }

        public override int GetHashCode()
        {
            if (HasValue)
            {
                return m_Value?.GetHashCode() ?? 0;
            }
            else
            {
                return 0;
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj is FrameCache<T> cache)
            {
                return Equals(cache);
            }
            else if (obj is T value)
            {
                if (HasValue == false)
                {
                    return false;
                }

                return m_Value?.Equals(obj) ?? obj == null;
            }

            return false;
        }

        public bool Equals(FrameCache<T> value)
        {
            bool b1 = HasValue;
            bool b2 = value.HasValue;

            if (b1)
            {
                if (b2)
                {
                    // has && has
                    return m_Value?.Equals(value.m_Value) ?? value.m_Value == null;
                }
                else
                {
                    // has && null
                    return false;
                }
            }
            else
            {
                if (b2)
                {
                    // null & has
                    return false;
                }
                else
                {
                    // null & null
                    return true;
                }
            }
        }

        public T? GetValueOrDefault()
        {
            if (HasValue)
            {
                return m_Value;
            }
            else
            {
                return default;
            }
        }

        public static implicit operator T(FrameCache<T> cache)
        {
            return cache.Value;
        }

        public static implicit operator FrameCache<T>(T value)
        {
            return new FrameCache<T>(value);
        }
    }
}