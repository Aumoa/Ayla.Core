using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Ayla;

public static class UnityIconCollection
{
    public struct Icon
    {
        public long Id;
        public Texture2D Texture;
        public string AssetPath;
    }

    private static ReadOnlyCollection<Icon>? m_Collection;
    private static readonly AssetBundle m_EditorAssetBundle;

    public static IReadOnlyList<Icon> Items => m_Collection
        ?? throw new InvalidOperationException("UnityIconCollection is not initialized yet.");

    public static bool Initialized => m_Collection != null;

    public static Icon GetIconSafe(long id)
    {
        --id;
        if (id >= 0 && id < Items.Count)
        {
            return Items[(int)id];
        }

        return default;
    }

    static UnityIconCollection()
    {
        var method_GetEditorAssetBundle = typeof(EditorGUIUtility).GetMethod("GetEditorAssetBundle", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Unity version not supported.");
        m_EditorAssetBundle = (AssetBundle)method_GetEditorAssetBundle.Invoke(null, null);
        RunOnAsync();
        return;

        async void RunOnAsync()
        {
            try
            {
                using (new TimerScope("Load Unity Icons tooks {0}"))
                {
                    using var scope1 = ListPool<Icon>.Get(out var icons);
                    long id = 0;
                    var collection = await YieldLoop.ForEach(m_EditorAssetBundle.GetAllAssetNames(), 14.0, name =>
                    {
                        var texture = m_EditorAssetBundle.LoadAsset<Texture2D>(name);
                        if (texture == null)
                        {
                            return default;
                        }

                        return new Icon
                        {
                            Id = ++id,
                            Texture = texture,
                            AssetPath = name
                        };
                    });

                    m_Collection = Array.AsReadOnly(collection.Where(i => i.Texture).ToArray());
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    public static Texture2D LoadIcon(string iconPath)
    {
        return m_EditorAssetBundle.LoadAsset<Texture2D>(iconPath);
    }
}