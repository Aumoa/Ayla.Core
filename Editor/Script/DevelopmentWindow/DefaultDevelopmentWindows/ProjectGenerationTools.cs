#nullable enable

using UnityEditor;
using UnityEngine;

namespace Ayla
{
    [SystemCategory, DefaultOrder(1)]
    public class ProjectGenerationTools : DevelopmentTools
    {
        public override string Title => ProjectGenerationToolsText.ProjectGenerationToolsTitle;

        protected override void OnGUI(in DrawingArgs drawingArgs)
        {
            DrawMarkdownSection();
            EditorGUILayout.Space(10);
            DrawPackagesSection();
        }

        private static void DrawMarkdownSection()
        {
            EditorGUILayout.LabelField(ProjectGenerationToolsText.MarkdownHeader, EditorStyles.boldLabel);

            bool enabled = MarkdownProjectPatcher.Enabled;
            using (GUIScope.Changed())
            {
                enabled = EditorGUILayout.Toggle(ProjectGenerationToolsText.Enabled, enabled);
                if (GUI.changed)
                {
                    MarkdownProjectPatcher.Enabled = enabled;
                }
            }
        }

        private static void DrawPackagesSection()
        {
            EditorGUILayout.LabelField(ProjectGenerationToolsText.PackagesHeader, EditorStyles.boldLabel);

            bool enabled = PackagesProjectPatcher.Enabled;
            using (GUIScope.Changed())
            {
                enabled = EditorGUILayout.Toggle(ProjectGenerationToolsText.Enabled, enabled);
                if (GUI.changed)
                {
                    PackagesProjectPatcher.Enabled = enabled;
                }
            }
        }
    }
}
