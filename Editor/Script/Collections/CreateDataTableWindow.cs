#nullable enable

using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ayla
{
    public class CreateDataTableWindow : EditorWindow
    {
        private static class ConstructorArgs
        {
            public static bool CreatedBy { get; set; }
            public static string? CreateAt { get; set; }
        }

        private static Type[]? s_DataTableTypes;
        private static string[]? s_DataTableTypeNames;

        private int m_SelectedTypeIndex;
        private string? m_AssetName;
        private string? m_CreateAt;

        private void Awake()
        {
            if (!ConstructorArgs.CreatedBy)
            {
                Close();
                throw new InvalidOperationException();
            }

            titleContent = new GUIContent(DataTableText.CreateTitle);

            if (s_DataTableTypes == null)
            {
                s_DataTableTypes = ReflectionUtility.Types
                    .Where(t => t.IsImplements(typeof(DataTable<,>)))
                    .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                    .Where(t => t.GetCustomAttribute<ClassDefaultObjectAttribute>() == null)
                    .ToArray();
                s_DataTableTypeNames = s_DataTableTypes.Select(t => t.Name).ToArray();
            }

            if (string.IsNullOrEmpty(ConstructorArgs.CreateAt))
            {
                throw new ArgumentException(nameof(ConstructorArgs.CreateAt));
            }

            m_CreateAt = ConstructorArgs.CreateAt!;
            m_AssetName = "DataTable.asset";
        }

        private void OnGUI()
        {
            if (s_DataTableTypes == null || s_DataTableTypes.Length == 0)
            {
                EditorGUILayout.LabelField(DataTableText.SuitableNotFoundMessage);
                return;
            }

            m_SelectedTypeIndex = EditorGUILayout.Popup(DataTableText.TableType, m_SelectedTypeIndex, s_DataTableTypeNames);
            m_AssetName = EditorGUILayout.TextField(DataTableText.AssetName, m_AssetName);
            EditorGUILayout.Space();
            if (GUILayout.Button(DataTableText.Create))
            {
                string path = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(m_CreateAt, m_AssetName));
                var asset = (DataTable)CreateInstance(s_DataTableTypes[m_SelectedTypeIndex]);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
                Close();
            }
        }

        public static CreateDataTableWindow Create(string createAt)
        {
            ConstructorArgs.CreatedBy = true;
            ConstructorArgs.CreateAt = createAt;

            try
            {
                return CreateInstance<CreateDataTableWindow>();
            }
            finally
            {
                ConstructorArgs.CreatedBy = false;
            }
        }
    }
}