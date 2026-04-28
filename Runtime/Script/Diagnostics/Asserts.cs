#nullable enable

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace Ayla
{
    public static class Asserts
    {
        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fail()
        {
            Debug.LogErrorFormat("Assertion failed.");
            BreakIfPresent();
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fail(string message)
        {
            Debug.LogErrorFormat("Assertion failed: {0}", message);
            BreakIfPresent();
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BreakIfPresent()
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void True(bool condition, string message)
        {
            if (!condition)
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void True(bool condition)
        {
            if (!condition)
            {
                Fail("Expected condition to be true.");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void False(bool condition, string message)
        {
            if (condition)
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void False(bool condition)
        {
            if (condition)
            {
                Fail("Expected condition to be false.");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Equals<T>(T? v1, T? v2, string message)
        {
            if (!((v1?.Equals(v2) ?? v1 is null) == true))
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Equals<T>(T? v1, T? v2)
        {
            if (!((v1?.Equals(v2) ?? v1 is null) == true))
            {
                Fail($"Expected {v1} to equal {v2}.");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEquals<T>(T? v1, T? v2, string message)
        {
            if ((v1?.Equals(v2) ?? v2 is null) == true)
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEquals<T>(T? v1, T? v2)
        {
            if (v1?.Equals(v2) ?? v2 is null == true)
            {
                Fail($"Expected {v1} to not equal {v2}.");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Is<T>(T? v1, T? v2, string message)
        {
            if (!ReferenceEquals(v1, v2))
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Is<T>(T? v1, T? v2)
        {
            if (!ReferenceEquals(v1, v2))
            {
                Fail($"Expected {v1} to be the same instance as {v2}.");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNot<T>(T? v1, T? v2, string message)
        {
            if (ReferenceEquals(v1, v2))
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNot<T>(T? v1, T? v2)
        {
            if (ReferenceEquals(v1, v2))
            {
                Fail($"Expected {v1} to not be the same instance as {v2}.");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNull<T>(T? v, string message)
        {
            if (v is not null)
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNull<T>(T? v)
        {
            if (v is not null)
            {
                Fail($"Expected {v} to be null.");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNull<T>(T? v, string message)
        {
            if (v is null)
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNull<T>(T? v)
        {
            if (v is null)
            {
                Fail($"Expected {v} to not be null.");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Greater<T>(T a, T b, string message) where T : IComparable<T>
        {
            int result = a.CompareTo(b);
            if (result <= 0)
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Greater<T>(T a, T b) where T : IComparable<T>
        {
            int result = a.CompareTo(b);
            if (result <= 0)
            {
                Fail($"Expected {a} to be greater than {b}.");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GreaterEquals<T>(T a, T b, string message) where T : IComparable<T>
        {
            int result = a.CompareTo(b);
            if (result < 0)
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GreaterEquals<T>(T a, T b) where T : IComparable<T>
        {
            int result = a.CompareTo(b);
            if (result < 0)
            {
                Fail($"Expected {a} to be greater than or equal to {b}.");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Less<T>(T a, T b, string message) where T : IComparable<T>
        {
            int result = a.CompareTo(b);
            if (result >= 0)
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Less<T>(T a, T b) where T : IComparable<T>
        {
            int result = a.CompareTo(b);
            if (result >= 0)
            {
                Fail($"Expected {a} to be less than {b}.");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LessEquals<T>(T a, T b, string message) where T : IComparable<T>
        {
            int result = a.CompareTo(b);
            if (result > 0)
            {
                Fail(message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LessEquals<T>(T a, T b) where T : IComparable<T>
        {
            int result = a.CompareTo(b);
            if (result > 0)
            {
                Fail($"Expected {a} to be less than or equal to {b}.");
            }
        }
    }
}
