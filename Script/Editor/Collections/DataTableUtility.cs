using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Ayla
{
    internal static class DataTableUtility
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var obj = EditorUtility.EntityIdToObject(instanceId);
            if (obj is DataTable)
            {
                var serializedObject = new SerializedObject(obj);
                var targetProperty = serializedObject.FindProperty("m_Dict");
                if (targetProperty == null)
                {
                    return false;
                }

                var window = ScriptableObject.CreateInstance<OrderedDictionaryEditor>();
                window.SelectProperty(targetProperty);
                window.Show();
                return true;
            }

            return false;
        }
    }
}
