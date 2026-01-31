using UnityEditor;
using UnityEngine;

namespace Ayla
{
    [CustomPropertyDrawer(typeof(OrderedDictionary<,>))]
    public class OrderedDictionaryPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
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
    }
}
