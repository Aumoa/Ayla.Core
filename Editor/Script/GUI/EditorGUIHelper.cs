#nullable enable

using UnityEditor;
using UnityEngine;

namespace Ayla
{
    public static partial class EditorGUIHelper
    {
        private static GUIContent? s_TempContent;

        public static GUIContent TempContent(string? text, string? tooltip = null, Texture? image = null)
        {
            s_TempContent ??= new GUIContent();
            s_TempContent.text = text;
            s_TempContent.tooltip = tooltip;
            s_TempContent.image = image;
            return s_TempContent;
        }

        public static GUIContent TempContent(Texture? image)
            => TempContent(null, null, image);

#if UNITY_EDITOR
        public static GUIContent TempIconContent(string iconName, string? text = null, string? tooltip = null)
        {
            var iconContent = EditorGUIUtility.IconContent(iconName);
            var content = TempContent(text, tooltip, iconContent.image);
            return content;
        }
#endif

        // UnityEditor.EditorGUI.kDefaultSpacing = 6
        public const float kDefaultSpacing = 6;
    }
}
