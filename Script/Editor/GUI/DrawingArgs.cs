#nullable enable

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Ayla
{
    public readonly struct DrawingArgs
    {
        public readonly Rect DrawingRect;
        public readonly Rect ClippingRect;

        public DrawingArgs(Rect drawingRect, Rect clippingRect)
        {
            DrawingRect = drawingRect;
            ClippingRect = clippingRect;
        }

        public bool Contains(Vector2 position)
        {
            return DrawingRect.Clip(ClippingRect).Contains(position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs Margin(float value)
            => Margin(value, value);

        public DrawingArgs Margin(RectOffset offset)
            => new DrawingArgs(
                DrawingRect.Margin(offset),
                ClippingRect
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs Margin(float horizontal, float vertical)
            => Margin(horizontal, vertical, horizontal, vertical);

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs Margin(float left, float top, float right, float bottom)
        {
            return new DrawingArgs(
                DrawingRect.Margin(left, top, right, bottom),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs MarginLeft(float value)
        {
            return new DrawingArgs(
                DrawingRect.MarginLeft(value),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs MarginTop(float value)
        {
            return new DrawingArgs(
                DrawingRect.MarginTop(value),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs MarginRight(float value)
        {
            return new DrawingArgs(
                DrawingRect.MarginRight(value),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs MarginBottom(float value)
        {
            return new DrawingArgs(
                DrawingRect.MarginBottom(value),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs MarginLeftTop(float left, float top)
        {
            return new DrawingArgs(
                DrawingRect.MarginLeftTop(left, top),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs MarginRightBottom(float right, float bottom)
        {
            return new DrawingArgs(
                DrawingRect.MarginRightBottom(right, bottom),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs MiddleVertical(float height)
        {
            return new DrawingArgs(
                DrawingRect.MiddleVertical(height),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs MiddleHorizontal(float width)
        {
            return new DrawingArgs(
                DrawingRect.MiddleHorizontal(width),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs FillLeft(float left)
        {
            return new DrawingArgs(
                DrawingRect.FillLeft(left),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs FillTop(float top)
        {
            return new DrawingArgs(
                DrawingRect.FillTop(top),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs FillRight(float right)
        {
            return new DrawingArgs(
                DrawingRect.FillRight(right),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs FillBottom(float bottom)
        {
            return new DrawingArgs(
                DrawingRect.FillBottom(bottom),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs FillLeftTop(float left, float top)
        {
            return new DrawingArgs(
                DrawingRect.FillLeftTop(left, top),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs FillRightBottom(float right, float bottom)
        {
            return new DrawingArgs(
                DrawingRect.FillRightBottom(right, bottom),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs MakeChild(Rect childRect)
        {
            return new DrawingArgs(
                ClippingRect.MakeChild(childRect.position, childRect.size),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs WithArea()
        {
            return new DrawingArgs(
                DrawingRect.ZeroPosition(),
                ClippingRect.ZeroPosition()
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs ScrollVertical(float value)
        {
            return new DrawingArgs(
                DrawingRect.MakeChild(new Vector2(0, -value)),
                ClippingRect
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DrawingArgs ScrollHorizontal(float value)
        {
            return new DrawingArgs(
                DrawingRect.MakeChild(new Vector2(-value, 0)),
                ClippingRect
            );
        }

        public static DrawingArgs MakeRoot(EditorWindow window)
        {
            return new DrawingArgs(window.position.ZeroPosition(), window.position.ZeroPosition());
        }

        public readonly struct AreaScope : IDisposable
        {
            public AreaScope(Rect clippingRect)
            {
                GUILayout.BeginArea(clippingRect);
            }

            public void Dispose()
            {
                GUILayout.EndArea();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public AreaScope WithClipping(out DrawingArgs childArgs)
        {
            var rect = DrawingRect.ZeroPosition();
            childArgs = new DrawingArgs(rect, rect);
            return new AreaScope(DrawingRect);
        }
    }
}