#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace Ayla
{
    public static class EditorGUIScope
    {
        public readonly struct VerticalScopeBuilder : IDisposable
        {
            private readonly bool m_Valid;

            public VerticalScopeBuilder(bool valid)
            {
                m_Valid = valid;
            }

            public void Dispose()
            {
                if (m_Valid)
                {
                    EditorGUILayout.EndVertical();
                }
            }
        }

        public static VerticalScopeBuilder Vertical(out Rect layoutRect, params GUILayoutOption[] options)
        {
            layoutRect = EditorGUILayout.BeginVertical(options);
            return new VerticalScopeBuilder(true);
        }

        public static VerticalScopeBuilder Vertical(GUIStyle style, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(style, options);
            return new VerticalScopeBuilder(true);
        }

        public static VerticalScopeBuilder Vertical(params GUILayoutOption[] options) => Vertical(out _, options);

        public readonly struct HorizontalScopeBuilder : IDisposable
        {
            private readonly bool m_Valid;

            public HorizontalScopeBuilder(bool valid)
            {
                m_Valid = valid;
            }

            public void Dispose()
            {
                if (m_Valid)
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        public static HorizontalScopeBuilder Horizontal(out Rect layoutRect)
        {
            layoutRect = EditorGUILayout.BeginHorizontal();
            return new HorizontalScopeBuilder(true);
        }

        public static HorizontalScopeBuilder Horizontal() => Horizontal(out _);

        public readonly struct LabelWidthScopeBuilder : IDisposable
        {
            private readonly float m_Width;

            public LabelWidthScopeBuilder(float newWidth)
            {
                m_Width = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = newWidth;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = m_Width;
            }
        }

        public static LabelWidthScopeBuilder LabelWidth(float newWidth) => new(newWidth);

        public readonly struct WideModeScopeBuilder : IDisposable
        {
            private readonly bool m_WideMode;

            public WideModeScopeBuilder(bool wideMode)
            {
                m_WideMode = EditorGUIUtility.wideMode;
                EditorGUIUtility.wideMode = wideMode;
            }

            public void Dispose()
            {
                EditorGUIUtility.wideMode = m_WideMode;
            }
        }

        public static WideModeScopeBuilder WideMode(bool wideMode) => new(wideMode);

        public readonly struct IndentScopeBuilder : IDisposable
        {
            private readonly int m_PreviousIndentLevel;

            public IndentScopeBuilder(int indentLevel)
            {
                m_PreviousIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = indentLevel;
            }
            public void Dispose()
            {
                EditorGUI.indentLevel = m_PreviousIndentLevel;
            }
        }

        public static IndentScopeBuilder Indent(int extraIndent = 1)
        {
            return new IndentScopeBuilder(EditorGUI.indentLevel + extraIndent);
        }
    }
}
