using System;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

// Android camera permission. In the editor it always reports "granted".
public static class CameraPermission
{
    public static bool IsGranted
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(Permission.Camera);
#else
            return true;
#endif
        }
    }

    // Shows Android's permission prompt. The callback may arrive on another thread,
    // so callers should only store the result and act on it from Update or a coroutine.
    public static void Request(Action<bool> onResult)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var callbacks = new PermissionCallbacks();
        // Every possible outcome must answer, or the caller would wait forever.
        callbacks.PermissionGranted += _ => onResult(true);
        callbacks.PermissionDenied += _ => onResult(false); // in Unity 6 this also covers "don't ask again"
        callbacks.PermissionRequestDismissed += _ => onResult(false);
        Permission.RequestUserPermission(Permission.Camera, callbacks);
#else
        onResult(true);
#endif
    }
}
