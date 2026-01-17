#nullable enable

using System;

namespace Ayla
{
    /// <summary>
    /// Provides commonly used utilities for text represented as a <see cref="string"/> class.
    /// </summary>
    public static class StringUtility
    {
        /*
         * In Unity C# Mono implementation, System.StringComparison is not fully supported.
         * System.GlobalizationMode.Invariant property always returns false.
         * Therefore, StringComparison supports only IgnoreCase, excluding other features.
         */

        /// <summary>
        /// Replaces the start of a string with a new value if it starts with the specified old value.
        /// </summary>
        /// <param name="source"> The source string to check and modify. </param>
        /// <param name="oldValue"> The string to compare at the start of the source string. </param>
        /// <param name="newValue"> The string to replace the start of the source string with, if the condition is met. Can be <c>null</c>. </param>
        /// <param name="comparison"> The type of string comparison to use when checking for <paramref name="oldValue"/> at the start of the source string. </param>
        /// <returns>
        /// A new string where the start of the source string is replaced with <paramref name="newValue"/> 
        /// if the source string starts with <paramref name="oldValue"/>; otherwise, the original source string.
        /// </returns>
        /// <remarks>
        /// This method performs a case-sensitive or case-insensitive comparison based on the specified <paramref name="comparison"/> value.
        /// If <paramref name="newValue"/> is <c>null</c>, the resulting string will have the start removed without replacement.
        /// </remarks>
        public static string ReplaceStart(this string source, string oldValue, string? newValue, StringComparison comparison = StringComparison.Ordinal)
        {
            if (source.StartsWith(oldValue, comparison))
            {
                return newValue + source[oldValue.Length..];
            }

            return source;
        }

        /// <summary>
        /// Replaces the end of a string with a new value if it ends with the specified old value.
        /// </summary>
        /// <param name="source">The source string to check and modify.</param>
        /// <param name="oldValue">The string to compare at the end of the source string.</param>
        /// <param name="newValue">The string to replace the end of the source string with, if the condition is met. Can be <c>null</c>.</param>
        /// <param name="comparison">The type of string comparison to use when checking for <paramref name="oldValue"/> at the end of the source string.</param>
        /// <returns>
        /// A new string where the end of the source string is replaced with <paramref name="newValue"/> 
        /// if the source string ends with <paramref name="oldValue"/>; otherwise, the original source string.
        /// </returns>
        /// <remarks>
        /// This method performs a case-sensitive or case-insensitive comparison based on the specified <paramref name="comparison"/> value.
        /// If <paramref name="newValue"/> is <c>null</c>, the resulting string will have the end removed without replacement.
        /// </remarks>
        public static string ReplaceEnd(this string source, string oldValue, string? newValue, StringComparison comparison = StringComparison.Ordinal)
        {
            if (source.EndsWith(oldValue, comparison))
            {
                return source[..^oldValue.Length] + newValue;
            }
            return source;
        }
    }
}