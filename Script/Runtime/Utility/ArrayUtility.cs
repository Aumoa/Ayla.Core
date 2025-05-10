using System;
using UnityEngine.Pool;

namespace Ayla.Core
{
    public static class ArrayUtility
    {
        public static T[] AddClone<T>(this T[] array, T item)
        {
            var newArray = new T[array.Length + 1];
            Array.Copy(array, newArray, array.Length);
            newArray[^1] = item;
            return newArray;
        }
        
        public static T[] RemoveWhereClone<T>(this T[] array, Predicate<T> predicate)
        {
            using var scope1 = ListPool<T>.Get(out var result);
            foreach (var item in array)
            {
                if (predicate(item) == false)
                {
                    result.Add(item);
                }
            }

            return result.ToArray();
        }
    }
}