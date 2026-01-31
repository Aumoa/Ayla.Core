#nullable enable
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Ayla
{
    public static class VerticalBorder
    {
        public const float Width = 1;
        public const float ShadowPixels = 7;
        private const float ShadowPow = 2f;
        private const float kInvShadowPixels = 1.0f / ShadowPixels;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Draw(DrawingArgs drawingArgs, Color color)
        {
            if (drawingArgs.DrawingRect.width >= 1.0f)
            {
                var a = color.a;
                for (int i = 0; i < ShadowPixels; ++i)
                {
                    float f = i * kInvShadowPixels;
                    color = color.WithAlpha(a * Mathf.Pow(1.0f - f, ShadowPow));
                    var rect = drawingArgs.DrawingRect.FillLeft(Width);
                    EditorGUI.DrawRect(rect, color);
                    drawingArgs = drawingArgs.MarginLeft(Width);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Draw(DrawingArgs args) => Draw(args, Color.black * 0.5f);
    }
}