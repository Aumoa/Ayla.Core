#nullable enable

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Ayla
{
    public static class MonoScriptUtility
    {
        private static readonly Dictionary<Type, MonoScript> s_MonoScripts = new();

        public static MonoScript? GetMonoScript(this Type type)
        {
            Debug.Assert(ApplicationMisc.IsInMainThread());
            Debug.Assert(type != null);

            if (s_MonoScripts.TryGetValue(type!, out var monoScript))
            {
                return monoScript;
            }

            if (type!.IsAssignableTo(typeof(ScriptableObject)))
            {
                var scriptableObject = ScriptableObject.CreateInstance(type);
                try
                {
                    monoScript = MonoScript.FromScriptableObject(scriptableObject);
                }
                finally
                {
                    Object.DestroyImmediate(scriptableObject);
                }
            }
            else if (type!.IsAssignableTo(typeof(MonoBehaviour)))
            {
                var gameObject = new GameObject("MonoScriptUtility Temp Object", type);
                try
                {
                    monoScript = MonoScript.FromMonoBehaviour((MonoBehaviour)gameObject.GetComponent(type));
                }
                finally
                {
                    Object.DestroyImmediate(gameObject);
                }
            }

            s_MonoScripts.Add(type!, monoScript);
            return monoScript;
        }
    }
}
