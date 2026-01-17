using System;
using UnityEngine;

namespace Ayla
{
    public static class GUIScope
    {
        public readonly struct ColorScopeBuilder : IDisposable
        {
            private readonly Color m_PreviousColor;
            
            public ColorScopeBuilder(Color color)
            {
                m_PreviousColor = GUI.color;
                GUI.color = color;
            }

            public void Dispose()
            {
                GUI.color = m_PreviousColor;
            }
        }

        public static ColorScopeBuilder Color(Color color)
        {
            return new ColorScopeBuilder(color);
        }

        public readonly struct ChangedScopeBuilder : IDisposable
        {
            private readonly bool m_Changed;
            
            public ChangedScopeBuilder(bool changed)
            {
                m_Changed = GUI.changed;
                GUI.changed = changed;
            }

            public void Dispose()
            {
                GUI.changed = m_Changed;
            }
        }

        public static ChangedScopeBuilder Changed()
        {
            return new ChangedScopeBuilder(false);
        }

        public readonly struct DisabledScopeBuilder : IDisposable
        {
            private readonly bool m_Disabled;

            public DisabledScopeBuilder(bool disabled)
            {
                m_Disabled = !GUI.enabled;
                GUI.enabled = !disabled;
            }

            public void Dispose()
            {
                GUI.enabled = !m_Disabled;
            }
        }

        public static DisabledScopeBuilder Disabled(bool disabled = true)
        {
            return new DisabledScopeBuilder(disabled);
        }

        public readonly struct AreaScopeBuilder : IDisposable
        {
            public AreaScopeBuilder(Rect area)
            {
                GUILayout.BeginArea(area);
            }

            public void Dispose()
            {
                GUILayout.EndArea();
            }
        }

        public static AreaScopeBuilder Area(Rect area)
        {
            return new AreaScopeBuilder(area);
        }
    }
}