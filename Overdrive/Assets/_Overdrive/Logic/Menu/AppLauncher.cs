using System.Collections;
using UnityEngine;

/// <summary>
/// Places the Overdrive menu in front of the user ONCE at startup,
/// then leaves it completely fixed in world space. Walking toward it
/// brings you physically closer, just like a real screen.
/// </summary>
public class AppLauncher : MonoBehaviour
{
    [Header("References")]
    public Canvas    menuCanvas;
    public Transform centerEyeAnchor;

    [Header("Placement")]
    [Tooltip("Distance from the user's head in metres when it first appears")]
    public float spawnDistance  = 1.5f;

    [Tooltip("Vertical offset from eye height (0 = exact eye level)")]
    public float verticalOffset = 0f;

    [Header("Fade")]
    public float fadeInDuration  = 0.6f;
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

        SnapMenuInFront();   // place ONCE, then never touch it again
        yield return FadeIn();
    }

    // NOTE: no LateUpdate / no follow. The menu stays where it spawned.

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Instantly (re)place the menu in front of the user.</summary>
    public void SnapMenuInFront()
    {
        Transform head = GetHead();
        if (head == null || menuCanvas == null) return;

        Vector3 fwd = FlatForward(head.forward);
        menuCanvas.transform.position = head.position
                                      + fwd        * spawnDistance
                                      + Vector3.up * verticalOffset;
        menuCanvas.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }

    /// <summary>Toggle visibility. Re-snaps in front of you when shown again.</summary>
    public void ToggleMenu()
    {
        if (menuCanvas == null) return;

        if (menuCanvas.gameObject.activeSelf && _cg.alpha > 0.5f)
            StartCoroutine(FadeOut());
        else
        {
            SnapMenuInFront();
            StartCoroutine(FadeIn());
        }
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
        if (Camera.main     != null) return Camera.main.transform;
        return null;
    }

    private Transform FindCenterEye()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null) return rig.centerEyeAnchor;
        if (Camera.main != null) return Camera.main.transform;
        return null;
    }

    private static Vector3 FlatForward(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.001f ? v.normalized : Vector3.forward;
    }

    private IEnumerator FadeIn()
    {
        for (float e = 0; e < fadeInDuration; e += Time.deltaTime)
        { _cg.alpha = Mathf.Clamp01(e / fadeInDuration); yield return null; }
        _cg.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        for (float e = 0; e < fadeInDuration; e += Time.deltaTime)
        { _cg.alpha = 1f - Mathf.Clamp01(e / fadeInDuration); yield return null; }
        _cg.alpha = 0f;
    }
}
