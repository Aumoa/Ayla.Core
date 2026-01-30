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

        public static VerticalScopeBuilder Vertical(out Rect layoutRect)
        {
            layoutRect = EditorGUILayout.BeginVertical();
            return new VerticalScopeBuilder(true);
        }

        public static VerticalScopeBuilder Vertical() => Vertical(out _);

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
    }
}
