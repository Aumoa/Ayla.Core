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
        InitializeThreadId();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void StaticAwake()
    {
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
