#nullable enable

using System.Reflection;
using System;
using UnityEditor;
using UnityEngine.Pool;
using UnityEngine;
using System.Collections.Generic;

namespace Ayla
{
    public static class UnityIconCollection
    {
        public struct Icon
        {
            public long Id;
            public Texture2D Texture;
            public string AssetPath;
        }

        private static readonly Icon[] m_Collection;
        private static readonly AssetBundle m_EditorAssetBundle;

        public static IReadOnlyList<Icon> Items => m_Collection;

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
            var method_GetEditorAssetBundle = typeof(EditorGUIUtility).GetMethod("GetEditorAssetBundle", BindingFlags.NonPublic | BindingFlags.Static);
            if (method_GetEditorAssetBundle == null)
            {
                throw new InvalidOperationException("Unity version not supported.");
            }

            m_EditorAssetBundle = (AssetBundle)method_GetEditorAssetBundle.Invoke(null, null);

            using var scope1 = ListPool<Icon>.Get(out var icons);
            long id = 0;
            foreach (var name in m_EditorAssetBundle.GetAllAssetNames())
            {
                var texture = m_EditorAssetBundle.LoadAsset<Texture2D>(name);
                if (texture == null)
                {
                    continue;
                }

                icons.Add(new Icon
                {
                    Id = ++id,
                    Texture = texture,
                    AssetPath = name
                });
            }

            m_Collection = icons.ToArray();
        }

        public static Texture2D LoadIcon(string iconPath)
        {
            return m_EditorAssetBundle.LoadAsset<Texture2D>(iconPath);
        }
    }
}