#nullable enable

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Ayla
{
    public partial class UnityIconExplorer
    {
        private static class CollectionLayout
        {
            private const float ScrollWidth = 14.0f;

            private static GUIContent? s_TempContent;
            private static UnityIconCollection.Icon[]? s_SearchIcons;
            private static string? s_SearchTextCache;

            public static void OnGUI(IUnityIconExplorer context, DrawingArgs drawingArgs)
            {
                float iconSize = (float)context.IconSize;

                CacheSearchIcons(context);

                int columns = (int)((drawingArgs.DrawingRect.width - ScrollWidth) / iconSize);
                int rows = (s_SearchIcons!.Length - 1) / columns + 1;
                float totalHeight = rows * iconSize;

                var current = Event.current;
                if (current.rawType == EventType.ScrollWheel && drawingArgs.DrawingRect.Clip(drawingArgs.ClippingRect).Contains(current.mousePosition))
                {
                    context.VerticalScroll += current.delta.y * iconSize;
                    current.Use();
                }

                using (GUIScope.Disabled(totalHeight < drawingArgs.DrawingRect.height))
                {
                    context.VerticalScroll = GUI.VerticalScrollbar(drawingArgs.DrawingRect.FillRight(ScrollWidth), context.VerticalScroll, Math.Min(drawingArgs.DrawingRect.height, totalHeight), 0, totalHeight);
                }
                drawingArgs = drawingArgs.MarginRight(ScrollWidth);

                using (drawingArgs.WithClipping(out _))
                {
                    int startRow = (int)(context.VerticalScroll / iconSize);
                    // 2 == last index + fill shrink
                    int endRow = startRow + (int)(drawingArgs.DrawingRect.height / iconSize) + 2;
                    int endIndex = Math.Min(endRow * columns, s_SearchIcons.Length);

                    for (int i = startRow * columns; i < endIndex; ++i)
                    {
                        var iconItem = s_SearchIcons[i];
                        int x = i % columns;
                        int y = i / columns;

                        var icon = GetTempContent(iconItem.Texture);
                        var iconRect = new Rect
                        {
                            x = iconSize * x,
                            y = (iconSize * y) - context.VerticalScroll,
                            width = iconSize,
                            height = iconSize
                        };

                        bool isSelected = context.Selected == iconItem.Id;
                        if (GUI.Toggle(iconRect, isSelected, icon, GUI.skin.button) && GUI.changed)
                        {
                            context.Selected = iconItem.Id;
                            GUI.changed = false;
                        }
                    }
                }
            }

            private static readonly char[] TokenSeparators = { ' ', '\t', '\n', '_', '-', '.', '?' };

            private static void CacheSearchIcons(IUnityIconExplorer context)
            {
                if (s_SearchIcons == null || s_SearchTextCache == null || s_SearchTextCache != context.SearchSymbol)
                {
                    string[] searchTokens = context.SearchSymbol.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
                    if (searchTokens.Length == 0)
                    {
                        s_SearchIcons = UnityIconCollection.Items.ToArray();
                        s_SearchTextCache = context.SearchSymbol;
                        return;
                    }
                    
                    s_SearchIcons = UnityIconCollection.Items.Where(SearchFunction).ToArray();
                    s_SearchTextCache = context.SearchSymbol;

                    bool SearchFunction(UnityIconCollection.Icon icon)
                    {
                        var tokens = ObjectNames.NicifyVariableName(icon.Texture.name).Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var token1 in tokens)
                        {
                            foreach (var token2 in searchTokens)
                            {
                                if (token1.Contains(token2, StringComparison.OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
                    }
                }

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static GUIContent GetTempContent(Texture? image) => GetTempContent(null, null, image);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static GUIContent GetTempContent(string? text, string? tooltip, Texture? image)
            {
                s_TempContent ??= new GUIContent();
                s_TempContent.text = text;
                s_TempContent.tooltip = tooltip;
                s_TempContent.image = image;
                return s_TempContent;
            }
        }
    }
}