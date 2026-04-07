#nullable enable

using System.Collections.Generic;

namespace Ayla
{
    public static class CollectionUtility
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                collection.Add(item);
            }
        }
    }
}
