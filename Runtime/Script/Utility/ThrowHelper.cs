#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ayla
{
    public static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfArgumentIsNull<T>([NotNull] T? param, string paramName)
        {
            if (param is null)
            {
                throw new ArgumentNullException(paramName, "Parameter cannot be null.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull<T>([NotNull] T? item, string itemName)
        {
            if (item is null)
            {
                throw new InvalidOperationException($"The item '{itemName}' is null.");
            }
        }
    }
}
