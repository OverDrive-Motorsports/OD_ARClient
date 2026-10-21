/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODPointerDragThreshold - Drag threshold suited to rays and pinches. The
 ## ISDK canvas module measures pointer motion in screen pixels of a helper
 ## camera 1 m from the canvas, so Unity's default 10 px is about 7 mm on the
 ## window: the ray shake of a pinch or trigger press starts a drag on the
 ## scroll view under the button, and the module then cancels the click.
 ##
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class ODPointerDragThreshold
{
    /// <summary>Distance the pointer must travel on the window before a drag starts.</summary>
    const float DragMetres = .03f;

    /// <summary>Field of view of the PointableCanvasModule's helper camera (Unity's default).</summary>
    const float ModuleCameraFov = 60f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        Apply();
        SceneManager.sceneLoaded += (_, _) => Apply();
    }

    static void Apply()
    {
        var eventSystem = EventSystem.current != null ? EventSystem.current : Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null) return;

        // The helper camera sits 1 m from the canvas: its view is 2·tan(fov/2) metres tall over Screen.height pixels.
        float metresPerPixel = 2f * Mathf.Tan(ModuleCameraFov * .5f * Mathf.Deg2Rad) / Mathf.Max(1, Screen.height);
        eventSystem.pixelDragThreshold = Mathf.Max(eventSystem.pixelDragThreshold, Mathf.CeilToInt(DragMetres / metresPerPixel));
    }
}
