using System;
using UnityEditor;
using UnityEngine;

namespace Ayla
{
    public static class ApplicationMisc
    {
        private static int s_MainThreadId;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EditorStaticAwake()
        {
            s_MainThreadId = Environment.CurrentManagedThreadId;
        }
#endif

        [RuntimeInitializeOnLoadMethod]
        private static void StaticAwake()
        {
            s_MainThreadId = Environment.CurrentManagedThreadId;
        }

        public static bool IsInMainThread()
        {
            return Environment.CurrentManagedThreadId == s_MainThreadId;
        }
    }
}
