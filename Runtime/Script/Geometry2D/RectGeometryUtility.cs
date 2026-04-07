#nullable enable

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Ayla
{
    public static class RectGeometryUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect Margin(this Rect source, float value)
            => Margin(source, value, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect Margin(this Rect source, float horizontal, float vertical)
            => Margin(source, horizontal, vertical, horizontal, vertical);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect Margin(this Rect source, float left, float top, float right, float bottom)
        {
            float xMin = Math.Min(source.x + left, source.xMax);
            float yMin = Math.Min(source.y + top, source.yMax);
            float xMax = Math.Max(source.xMax - right, xMin);
            float yMax = Math.Max(source.yMax - bottom, yMin);
            return new(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect Margin(this Rect source, RectOffset offset)
            => Margin(source, offset.left, offset.top, offset.right, offset.bottom);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect MarginLeft(this Rect source, float value)
            => new(source.x + value, source.y, source.width - value, source.height);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect MarginTop(this Rect source, float value)
            => new(source.x, source.y + value, source.width, source.height - value);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect MarginRight(this Rect source, float value)
            => new(source.x, source.y, source.width - value, source.height);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect MarginBottom(this Rect source, float value)
            => new(source.x, source.y, source.width, source.height - value);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect MarginLeftTop(this Rect source, float left, float top)
            => Margin(source, left, top, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect MarginRightBottom(this Rect source, float right, float bottom)
            => Margin(source, 0, 0, right, bottom);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect MiddleVertical(this Rect source, float height)
        {
            var half = height * 0.5f;
            var y = source.center.y;
            return new Rect(source.x, y - half, source.width, height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect MiddleHorizontal(this Rect source, float width)
        {
            var half = width * 0.5f;
            var x = source.center.x;
            return new Rect(x - half, source.y, width, source.height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect FillLeft(this Rect source, float left)
            => new(source.x, source.y, left, source.height);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect FillTop(this Rect source, float top)
            => new(source.x, source.y, source.width, top);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect FillRight(this Rect source, float right)
            => new(source.xMax - right, source.y, right, source.height);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect FillBottom(this Rect source, float bottom)
            => new(source.x, source.yMax - bottom, source.width, bottom);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect FillLeftTop(this Rect source, float left, float top)
            => new(source.x, source.y, left, top);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect FillRightBottom(this Rect source, float right, float bottom)
            => new(
                source.xMax - right, source.yMax - bottom,
                source.width, source.height
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect SetLeft(this Rect source, float left)
        {
            source.xMin = Math.Min(left, source.xMax);
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect SetTop(this Rect source, float top)
        {
            source.yMin = Math.Min(top, source.yMax);
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect SetRight(this Rect source, float right)
        {
            source.xMax = Math.Max(right, source.xMin);
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect SetBottom(this Rect source, float bottom)
        {
            source.yMax = Math.Max(bottom, source.yMin);
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect SetLeftTop(this Rect source, float left, float top)
        {
            source.xMin = Math.Min(left, source.xMax);
            source.yMin = Math.Min(top, source.yMax);
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect SetRightBottom(this Rect source, float right, float bottom)
        {
            source.xMax = Math.Max(right, source.xMin);
            source.yMax = Math.Max(bottom, source.yMin);
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect MakeChild(this Rect source, Vector2 position)
        {
            var max = new Vector2(source.xMax, source.yMax);
            source.position = position;
            source.xMax = max.x;
            source.yMax = max.y;
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect MakeChild(this Rect source, Vector2 position, Vector2 size)
        {
            var max = new Vector2(source.xMax, source.yMax);
            source.position = position;
            source.xMax = Math.Min(position.x + size.x, max.x);
            source.yMax = Math.Min(position.y + size.y, max.y);
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect ZeroPosition(this Rect source)
        {
            return new Rect(Vector2.zero, source.size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public static Rect Clip(this Rect source, Rect clipping)
        {
            float xMin = Math.Max(source.xMin, clipping.xMin);
            float yMin = Math.Max(source.yMin, clipping.yMin);
            float xMax = Math.Min(source.xMax, clipping.xMax);
            float yMax = Math.Min(source.yMax, clipping.yMax);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}