#nullable enable

using UnityEditor;
using UnityEngine;

namespace Ayla
{
    public partial class UnityIconExplorer
    {
        private static class HeaderLayout
        {
            public const float Height = 20;

            private const float DefaultVerticalMargin = 1;
            private const float DefaultHorizontalMargin = 2;

            public static void OnGUI(IUnityIconExplorer context, DrawingArgs drawingArgs)
            {
                drawingArgs = drawingArgs.FillTop(Height);
                drawingArgs = drawingArgs.MarginLeft(DrawIconSizeButton(context, drawingArgs));
                drawingArgs = drawingArgs.MarginLeft(DrawSerachLabel(drawingArgs));

                DrawSearchTextField(context, drawingArgs);
            }

            private static float DrawSerachLabel(DrawingArgs drawingArgs)
            {
                const float Width = 54;
                GUI.Label(drawingArgs.MarginLeft(4).FillLeft(50).DrawingRect, IconExplorerText.Search, EditorStyles.boldLabel);
                return Width;
            }

            private static float DrawIconSizeButton(IUnityIconExplorer context, DrawingArgs drawingArgs)
            {
                const float Width = 45.0f;
                context.IconSize = (IconSize)EditorGUI.EnumPopup(drawingArgs.FillLeft(Width).Margin(DefaultHorizontalMargin, DefaultVerticalMargin).DrawingRect, context.IconSize);
                return Width;
            }

            private static void DrawSearchTextField(IUnityIconExplorer context, DrawingArgs drawingArgs)
            {
                context.SearchSymbol = EditorGUI.TextField(drawingArgs.Margin(DefaultHorizontalMargin, DefaultVerticalMargin).DrawingRect, context.SearchSymbol);
            }
        }
    }
}