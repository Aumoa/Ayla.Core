#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace Ayla
{
    public partial class UnityIconExplorer : EditorWindow, IUnityIconExplorer
    {
        [SerializeField]
        private IconSize m_IconSize = IconSize._32;
        [SerializeField]
        private string m_SearchSymbol = string.Empty;
        [SerializeField]
        private float m_VerticalScroll;
        [SerializeField]
        private long m_Selected;

        IconSize IUnityIconExplorer.IconSize
        {
            get => m_IconSize;
            set => m_IconSize = value;
        }

        string IUnityIconExplorer.SearchSymbol
        {
            get => m_SearchSymbol ?? string.Empty;
            set => m_SearchSymbol = value ?? string.Empty;
        }

        float IUnityIconExplorer.VerticalScroll
        {
            get => Math.Max(m_VerticalScroll, 0);
            set => m_VerticalScroll = Math.Max(value, 0);
        }

        long IUnityIconExplorer.Selected
        {
            get => m_Selected;
            set => m_Selected = Math.Max(value, 0);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(IconExplorerText.Title);
        }

        private void OnGUI()
        {
            if (!UnityIconCollection.Initialized)
            {
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Loading Unity Icons...", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                return;
            }

            var drawingArgs = DrawingArgs.MakeRoot(this);
            HeaderLayout.OnGUI(this, drawingArgs);
            drawingArgs = drawingArgs.MarginTop(HeaderLayout.Height);

            HorizontalBorder.Draw(drawingArgs);
            drawingArgs = drawingArgs.MarginTop(1);

            Description.OnGUI(this, drawingArgs.FillBottom(Description.Height));
            drawingArgs = drawingArgs.MarginBottom(Description.Height);

            HorizontalBorder.Draw(drawingArgs.FillBottom(1));
            drawingArgs = drawingArgs.MarginBottom(1);

            CollectionLayout.OnGUI(this, drawingArgs);
        }

        [MenuItem("Window/Ayla/Unity Icon Explorer")]
        public static void OpenWindow()
        {
            GetWindow<UnityIconExplorer>();
        }
    }
}