#nullable enable

using UnityEditor;

namespace Ayla
{
    internal static class OrderedDictionarySelection
    {
        private static EditorWindow? m_Window;
        private static SerializedProperty? m_CurrentSelector;

        public static void Choose(EditorWindow window, SerializedProperty selector)
        {
            if (m_Window && m_CurrentSelector != null && m_CurrentSelector.intValue != -1)
            {
                m_CurrentSelector.serializedObject.Update();
                m_CurrentSelector.intValue = -1;
                m_CurrentSelector.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                m_Window.Repaint();
            }

            m_Window = window;
            m_CurrentSelector = selector;
        }

        public static void OnDestroy(EditorWindow window, SerializedProperty selector)
        {
            if (m_Window == window && m_CurrentSelector == selector)
            {
                m_Window = null;
                m_CurrentSelector = null;
            }
        }
    }
}
