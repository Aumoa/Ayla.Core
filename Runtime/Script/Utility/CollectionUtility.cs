#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ayla
{
    public static class CollectionUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                collection.Add(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidIndex<T>(this IReadOnlyList<T> list, Index index)
        {
            int rawIndex = index.IsFromEnd ? list.Count - index.Value : index.Value;
            return IsValidIndex(list, rawIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidIndex<T>(this IReadOnlyList<T> list, int index)
        {
            return index >= 0 && index < list.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? GetOrDefaultAt<T>(this IReadOnlyList<T> list, Index index, T? @default = default)
        {
            int rawIndex = index.IsFromEnd ? list.Count - index.Value : index.Value;
            return GetOrDefaultAt(list, rawIndex, @default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? GetOrDefaultAt<T>(this IReadOnlyList<T> list, int index, T? @default = default)
        {
            return IsValidIndex(list, index) ? list[index] : @default;
        }
    }
}
