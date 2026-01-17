using UnityEditor;
using UnityEngine;

namespace Ayla
{
    [CustomPropertyDrawer(typeof(OrderedDictionary<,>))]
    public class OrderedDictionaryPropertyDrawer : PropertyDrawer
    {
        private GUIContent m_Content;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 18;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_Content ??= new GUIContent("Not implemented yet.");
            EditorGUI.LabelField(position, label, m_Content);
        }
    }
}
