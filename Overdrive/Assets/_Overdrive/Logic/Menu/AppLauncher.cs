using System.Collections;
using UnityEngine;

/// <summary>
/// Places the Overdrive menu in front of the user when it appears (centred on
/// the headset, ODWindowPlacement), then leaves it fixed in world space.
/// Walking toward it brings you physically closer, just like a real screen.
/// </summary>
public class AppLauncher : MonoBehaviour
{
    [Header("References")]
    public Canvas menuCanvas;
    public Transform centerEyeAnchor;
    public Transform navigationWindow;
    public float navigationVerticalOffset = -0.47f;

    // Placement: ODWindowPlacement (distance and eye level are constants there, so stale
    // values serialized in the scene can't move the window).

    [Header("Fade")]
    public float fadeInDuration = 0.6f;
    public float delayBeforeFade = 0.5f;

    private CanvasGroup _cg;

    // ── Awake ────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (menuCanvas == null)
        {
            var go = GameObject.Find("OverdriveMenuCanvas");
            if (go != null) menuCanvas = go.GetComponent<Canvas>();
        }
        if (menuCanvas == null) { Debug.LogError("[AppLauncher] menuCanvas not found"); return; }

        // Required for correct VR depth rendering of world-space UI
        menuCanvas.worldCamera = Camera.main;

        // NOTE: never use ?? with Unity objects (fake-null) — explicit check
        _cg = menuCanvas.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = menuCanvas.gameObject.AddComponent<CanvasGroup>();

        _cg.alpha = 0f;
        menuCanvas.gameObject.SetActive(true);
    }

    // ── Start ────────────────────────────────────────────────────────────────
    private IEnumerator Start()
    {
        if (centerEyeAnchor == null)
            centerEyeAnchor = FindCenterEye();

        yield return WaitForTracking();

        SnapMenuInFront();   // placed when it appears, then left where the user drags it
        yield return FadeIn();
    }

    // NOTE: no LateUpdate / no follow. The menu stays where it spawned.

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Instantly (re)place the menu in front of the user: the visible window (not the larger
    /// canvas around it) centred on the headset's gaze, at eye height. The navigation window
    /// follows when it isn't on the wrist.
    /// </summary>
    public void SnapMenuInFront()
    {
        Transform head = GetHead();
        if (head == null || menuCanvas == null) return;

        // Through ODWindowMotion so a glide in progress (ODWindowLayout) is cancelled, not fought.
        var pose = ODWindowPlacement.RootPose(menuCanvas.transform, MenuWindow, ODWindowPlacement.Arc.InFront(head).At(0f));
        ODWindowMotion.MoveTo(menuCanvas.transform, pose, animate: false);
        FollowNavigation();
    }

    /// <summary>The floating navigation window, which moves with the menu, or null when it lives on the wrist.</summary>
    public Transform NavigationFollower =>
        navigationWindow != null && navigationWindow.GetComponentInParent<LeftWristNavigation>() == null ? navigationWindow : null;

    /// <summary>The menu as the user sees it (its canvas is larger).</summary>
    private RectTransform MenuWindow
    {
        get
        {
            var window = menuCanvas != null ? menuCanvas.GetComponentInChildren<OverdriveMainMenu>(true) : null;
            return window != null ? (RectTransform)window.transform : null;
        }
    }

    /// <summary>The floating navigation window stays under the menu, unless it lives on the wrist.</summary>
    private void FollowNavigation()
    {
        if (NavigationFollower == null) return;
        navigationWindow.SetPositionAndRotation(
            menuCanvas.transform.position + menuCanvas.transform.up * navigationVerticalOffset,
            menuCanvas.transform.rotation);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator WaitForTracking()
    {
        float timeout = 3f, elapsed = 0f;
        while (elapsed < timeout)
        {
            Transform h = GetHead();
            if (h != null && h.position.y > 0.1f) break;
            elapsed += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(delayBeforeFade);
    }

    private Transform GetHead()
    {
        if (centerEyeAnchor != null) return centerEyeAnchor;
        if (Camera.main != null) return Camera.main.transform;
        return null;
    }

    private Transform FindCenterEye()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null) return rig.centerEyeAnchor;
        if (Camera.main != null) return Camera.main.transform;
        return null;
    }

    private IEnumerator FadeIn()
    {
        for (float e = 0; e < fadeInDuration; e += Time.deltaTime)
        { _cg.alpha = Mathf.Clamp01(e / fadeInDuration); yield return null; }
        _cg.alpha = 1f;
    }
}
