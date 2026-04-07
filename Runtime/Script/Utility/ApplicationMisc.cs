#nullable enable

using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Ayla
{
    public static class ApplicationMisc
    {
        private static int s_MainThreadId;
        private static CancellationTokenSource s_ApplicationCancellation = new();
        private static bool s_TearingDown;

        public static CancellationToken ApplicationCancellationToken => s_ApplicationCancellation.Token;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EditorStaticAwake()
        {
            InitializeThreadId();
            s_TearingDown = false;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                s_TearingDown = false;
            }
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
