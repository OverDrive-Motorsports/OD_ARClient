using System.Collections;
using UnityEngine;

/// <summary>
/// Positions the Overdrive menu in front of the user at eye level on startup.
/// Auto-detects the OVR CenterEyeAnchor if not assigned manually.
/// </summary>
public class AppLauncher : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The OverdriveMenuCanvas (assign in Inspector or auto-found)")]
    public Canvas menuCanvas;

    [Tooltip("The OVR CenterEyeAnchor transform – auto-detected if left empty")]
    public Transform centerEyeAnchor;

    [Header("Placement")]
    [Tooltip("Distance from the user's head in metres")]
    public float spawnDistance = 1.5f;

    [Tooltip("Vertical offset from eye height (0 = exact eye level)")]
    public float verticalOffset = 0f;

    [Header("Fade-in")]
    public float fadeInDuration  = 0.6f;
    public float delayBeforeFade = 0.5f;

    private CanvasGroup _canvasGroup;

    // ── Awake: hide menu & grab references ───────────────────────────────────
    private void Awake()
    {
        // Auto-find canvas if not assigned
        if (menuCanvas == null)
        {
            var go = GameObject.Find("OverdriveMenuCanvas");
            if (go != null) menuCanvas = go.GetComponent<Canvas>();
        }

        if (menuCanvas == null)
        {
            Debug.LogError("[AppLauncher] No menuCanvas found!");
            return;
        }

        _canvasGroup = menuCanvas.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = menuCanvas.gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
        menuCanvas.gameObject.SetActive(true);
    }

    // ── Start: wait for tracking then place & fade ────────────────────────────
    private IEnumerator Start()
    {
        // Auto-find CenterEyeAnchor if not assigned
        if (centerEyeAnchor == null)
            centerEyeAnchor = FindCenterEyeAnchor();

        // Wait until we have a valid head position (OVR tracking init)
        yield return WaitForTracking();

        PlaceMenuInFrontOfUser();
        yield return FadeIn();
    }

    // ── Placement ─────────────────────────────────────────────────────────────
    public void PlaceMenuInFrontOfUser()
    {
        Transform head = GetHeadTransform();
        if (head == null || menuCanvas == null) return;

        // Flatten forward on XZ so the menu stays perfectly upright
        Vector3 forward = head.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 spawnPos = head.position
                         + forward  * spawnDistance
                         + Vector3.up * verticalOffset;

        menuCanvas.transform.position = spawnPos;
        menuCanvas.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    // ── Toggle (e.g. wrist button) ────────────────────────────────────────────
    public void ToggleMenu()
    {
        if (menuCanvas == null) return;

        bool visible = menuCanvas.gameObject.activeSelf && _canvasGroup.alpha > 0.5f;
        if (visible)
            StartCoroutine(FadeOut());
        else
        {
            PlaceMenuInFrontOfUser();
            StartCoroutine(FadeIn());
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Waits until the head camera has moved away from the world origin,
    /// meaning OVR tracking has kicked in. Times out after 3 s.
    /// </summary>
    private IEnumerator WaitForTracking()
    {
        float timeout = 3f;
        float elapsed = 0f;

        Transform head = GetHeadTransform();

        // Poll until the head is off the floor or we time out
        while (elapsed < timeout)
        {
            head = GetHeadTransform();
            if (head != null && head.position.y > 0.1f)
                break; // tracking is live and head is above floor

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Extra safety margin
        yield return new WaitForSeconds(delayBeforeFade);
    }

    private Transform GetHeadTransform()
    {
        if (centerEyeAnchor != null) return centerEyeAnchor;

        // Fallback: main camera
        if (Camera.main != null) return Camera.main.transform;

        return null;
    }

    private Transform FindCenterEyeAnchor()
    {
        // Try OVRCameraRig first
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null) return rig.centerEyeAnchor;

        // Fallback to main camera
        if (Camera.main != null) return Camera.main.transform;

        return null;
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
    }
}
