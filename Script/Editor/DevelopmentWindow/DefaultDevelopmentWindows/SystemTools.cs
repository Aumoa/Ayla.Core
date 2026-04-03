using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Ayla;

[SystemCategory, DefaultOrder(0)]
public class SystemTools : DevelopmentTools
{
    private const int kButtonWidth = 300;

    private static readonly Dictionary<string, Action> s_SystemActionCompilations = new()
    {
        [SystemToolsText.RequestScriptCompilation] = CompilationPipeline.RequestScriptCompilation,
        [SystemToolsText.RequestScriptReload] = EditorUtility.RequestScriptReload
    };

    private static readonly Dictionary<string, Action> s_SystemActionAssets = new()
    {
        [SystemToolsText.RefreshAssetDatabase] = AssetDatabase.Refresh,
        [SystemToolsText.SaveAllAssets] = AssetDatabase.SaveAssets
    };

    private static readonly Dictionary<string, Action> s_SystemActionsOther = new()
    {
        [SystemToolsText.ClearProgressBar] = EditorUtility.ClearProgressBar
    };

    public override string Title => SystemToolsText.UnityToolsTitle;

    protected override void OnGUI(in DrawingArgs drawingArgs)
    {
        bool firstPass = true;

        foreach (var dict in new[] { s_SystemActionCompilations, s_SystemActionAssets, s_SystemActionsOther })
        {
            if (firstPass)
            {
                firstPass = false;
            }
            else
            {
                GUILayout.Space(4);
            }

            GUILayout.BeginHorizontal();
            int widthAdvance = 0;
            try
            {
                foreach (var (name, action) in dict)
                {
                    if (GUILayout.Button(name))
                    {
                        action();
                    }

                    widthAdvance += kButtonWidth;
                    if (widthAdvance + kButtonWidth >= drawingArgs.DrawingRect.width)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        widthAdvance = 0;
                    }
                }
            }
            finally
            {
                GUILayout.EndHorizontal();
            }
        }
    }
}
