using UnityEditor;
using UnityEngine;

namespace Ayla
{
    [CustomPropertyDrawer(typeof(OrderedDictionary<,>))]
    public class OrderedDictionaryPropertyDrawer : PropertyDrawer
    {
        private SerializedProperty? m_Property;
        private SerializedProperty? m_Rows;
        private SerializedProperty? m_Selector;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            TryCacheProperty(property);
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            TryCacheProperty(property);
            Rect labelRect = position;
            labelRect.width = EditorGUIUtility.labelWidth + EditorGUIUtility.standardVerticalSpacing;
            Rect fieldRect = position;
            fieldRect.x += labelRect.width;
            fieldRect.width -= labelRect.width;

            EditorGUI.PrefixLabel(labelRect, label);

            if (GUI.Button(fieldRect, OrderedDictionaryText.OpenEditor))
            {
                var editorWindow = ScriptableObject.CreateInstance<OrderedDictionaryEditor>();
                var targetObjects = property.serializedObject.targetObjects;
                var serializedObject = new SerializedObject(targetObjects);
                editorWindow.SelectProperty(serializedObject.FindProperty(property.propertyPath));
                editorWindow.Show();
            }
        }

        private void TryCacheProperty(SerializedProperty property)
        {
            if (m_Property != property)
            {
                m_Property = property;
                if (m_Property != null)
                {
                    m_Rows = property.Copy();
                    m_Rows.Next(true);
                    m_Selector = m_Rows.Copy();
                    m_Selector.Next(false);
                }
                else
                {
                    m_Rows = null;
                    m_Selector = null;
                }
            }
        }
    }
}
