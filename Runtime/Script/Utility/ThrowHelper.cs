#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace Ayla
{
    public static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull<T>(T? param, string paramName)
        {
            if (param is null)
            {
                throw new ArgumentNullException(paramName, "Parameter cannot be null.");
            }
        }
    }
}
