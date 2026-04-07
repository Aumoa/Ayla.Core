#nullable enable

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
        private SerializedProperty? m_ClassDefaultObjectProperty;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            TryCacheProperty(property);
            if (m_Rows == null || m_Selector == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            var currentSelector = m_Selector.intValue;
            if (currentSelector == -1)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + GatherPropertyHeight(currentSelector);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            TryCacheProperty(property);
            if (m_Rows == null || m_Selector == null)
            {
                EditorGUI.LabelField(position, label, GUIContent.none);
                return;
            }

            Rect labelRect = position.FillTop(EditorGUIUtility.singleLineHeight);
            Rect fieldRect = labelRect;
            labelRect.width = EditorGUIUtility.labelWidth + EditorGUIUtility.standardVerticalSpacing;
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

            int currentSelector = m_Selector.intValue;
            if (currentSelector == -1)
            {
                return;
            }

            using (EditorGUIScope.Indent())
            {
                position = position.MarginTop(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                var currentPosition = position.FillTop(EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(EditorGUI.IndentedRect(currentPosition), DataTableText.SelectorLabel, EditorStyles.boldLabel);
                SerializedProperty elementAt;

                if (currentSelector >= 0)
                {
                    elementAt = m_Rows!.GetArrayElementAtIndex(currentSelector);
                }
                else
                {
                    elementAt = m_ClassDefaultObjectProperty!.GetArrayElementAtIndex(0);
                }

                position = position.MarginTop(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                elementAt.Next(true);  // Key
                currentPosition = position.FillTop(EditorGUI.GetPropertyHeight(elementAt));
                EditorGUI.PropertyField(EditorGUI.IndentedRect(currentPosition), elementAt, true);

                position = position.MarginTop(currentPosition.height + EditorGUIUtility.standardVerticalSpacing);
                elementAt.Next(false);  // Value
                currentPosition = position.FillTop(EditorGUI.GetPropertyHeight(elementAt));
                EditorGUI.PropertyField(EditorGUI.IndentedRect(currentPosition), elementAt, true);
            }
        }

        private float GatherPropertyHeight(int selectorIndex)
        {
            float height = EditorGUIUtility.singleLineHeight;
            height += EditorGUIUtility.standardVerticalSpacing;
            SerializedProperty elementAt;

            if (selectorIndex >= 0)
            {
                elementAt = m_Rows!.GetArrayElementAtIndex(selectorIndex);
            }
            else
            {
                elementAt = m_ClassDefaultObjectProperty!.GetArrayElementAtIndex(0);
            }

            elementAt.Next(true);  // Key
            height += EditorGUI.GetPropertyHeight(elementAt);
            height += EditorGUIUtility.standardVerticalSpacing;
            elementAt.Next(false);  // Value
            height += EditorGUI.GetPropertyHeight(elementAt);

            return height;
        }

        private void TryCacheProperty(SerializedProperty property)
        {
            if (m_Property != property)
            {
                m_Property = property;
                if (m_Property != null)
                {
                    var propertyType = property.boxedValue.GetType();
                    if (propertyType.GetGenericTypeDefinition() != typeof(OrderedDictionary<,>))
                    {
                        m_Rows = null;
                        m_Selector = null;
                        m_ClassDefaultObjectProperty = null;
                        return;
                    }

                    var ga = propertyType.GetGenericArguments();
                    var keyType = ga[0];
                    var valueType = ga[1];

                    m_Rows = property.Copy();
                    m_Rows.Next(true);
                    m_Selector = m_Rows.Copy();
                    m_Selector.Next(false);
                    m_ClassDefaultObjectProperty = ClassDefaultObjectBuilder.NewClassDefaultObjectProperty(keyType, valueType);
                }
                else
                {
                    m_Rows = null;
                    m_Selector = null;
                    m_ClassDefaultObjectProperty = null;
                }
            }
        }
    }
}
