#nullable enable

using System;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Ayla
{
    public static class ApplicationMisc
    {
        private static int s_MainThreadId;
        private static CancellationTokenSource s_ApplicationCancellation = new();
        private static bool s_TearingDown;

#if UNITY_EDITOR
        private static SystemLanguage s_Language;
        private static bool s_DomainReloading;
#endif

        public static CancellationToken ApplicationCancellationToken => s_ApplicationCancellation.Token;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EditorStaticAwake()
        {
            InitializeThreadId();
            s_TearingDown = false;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            s_Language = Application.systemLanguage;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        public static SystemLanguage EditorLanguage => s_Language;

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                s_TearingDown = false;
                s_Language = Application.systemLanguage;
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            s_DomainReloading = true;
        }

        private static void OnAfterAssemblyReload()
        {
            Application.quitting += OnApplicationQuit;
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void StaticAwake()
        {
            s_TearingDown = false;
            InitializeThreadId();
            CancelAndResetCancellation();
            Application.quitting += OnApplicationQuit;
        }

        private static void InitializeThreadId()
        {
            s_MainThreadId = Environment.CurrentManagedThreadId;
            var invocable = s_InitializeCallback;
            s_InitializeCallback = null;
            invocable?.Invoke();
        }

        private static Action? s_InitializeCallback;

        public static event Action InitializeCallback
        {
            add
            {
                if (s_MainThreadId == 0)
                {
                    s_InitializeCallback += value;
                }
                else
                {
                    value.Invoke();
                }
            }
            remove
            {
                if (s_MainThreadId == 0)
                {
                    s_InitializeCallback -= value;
                }
                else
                {
                    throw new InvalidOperationException("Cannot remove event handler when Initialize has already been called.");
                }
            }
        }

        public static bool IsInTearingDown()
        {
            return s_TearingDown;
        }

        public static bool IsInMainThread()
        {
            return Environment.CurrentManagedThreadId == s_MainThreadId;
        }

#if UNITY_EDITOR
        public static bool IsDomainReloading()
        {
            return s_DomainReloading;
        }
#endif

        private static void OnApplicationQuit()
        {
            s_TearingDown = true;
            CancelAndResetCancellation();
            Application.quitting -= OnApplicationQuit;
        }

        private static void CancelAndResetCancellation()
        {
            if (s_ApplicationCancellation != null)
            {
                s_ApplicationCancellation.Cancel();
                s_ApplicationCancellation.Dispose();
            }

            s_ApplicationCancellation = new CancellationTokenSource();
        }
    }
}
