using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Ayla;

public static class ApplicationMisc
{
    private static int s_MainThreadId;
    private static CancellationTokenSource s_ApplicationCancellation = new();

    public static CancellationToken ApplicationCancellationToken => s_ApplicationCancellation.Token;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorStaticAwake()
    {
        s_MainThreadId = Environment.CurrentManagedThreadId;
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void StaticAwake()
    {
        s_MainThreadId = Environment.CurrentManagedThreadId;
        CancelAndResetCancellation();
        Application.quitting += OnApplicationQuit;
    }

    public static bool IsInMainThread()
    {
        return Environment.CurrentManagedThreadId == s_MainThreadId;
    }

    private static void OnApplicationQuit()
    {
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
