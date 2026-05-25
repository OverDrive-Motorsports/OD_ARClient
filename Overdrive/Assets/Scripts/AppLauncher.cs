using System.Collections;
using UnityEngine;

/// <summary>
/// Positions the Overdrive menu in front of the user at app startup
/// and fades it in smoothly.
/// </summary>
public class AppLauncher : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The OverdriveMenuCanvas GameObject")]
    public Canvas menuCanvas;

    [Tooltip("The OVR CenterEyeAnchor transform (head camera)")]
    public Transform centerEyeAnchor;

    [Header("Placement")]
    [Tooltip("Distance from the user's head in metres")]
    public float spawnDistance = 1.5f;

    [Tooltip("Vertical offset from eye height (negative = slightly below)")]
    public float verticalOffset = -0.1f;

    [Header("Fade-in")]
    public float fadeInDuration = 0.6f;
    public float delayBeforeFade = 0.3f;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        // Hide the menu until we're ready
        _canvasGroup = menuCanvas.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = menuCanvas.gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
        menuCanvas.gameObject.SetActive(true);
    }

    private IEnumerator Start()
    {
        // Wait one frame so OVR tracking is initialised
        yield return null;
        yield return new WaitForSeconds(delayBeforeFade);

        PlaceMenuInFrontOfUser();

        yield return FadeIn();
    }

    /// <summary>
    /// Snaps the menu to 1.5 m in front of the user, facing them.
    /// </summary>
    public void PlaceMenuInFrontOfUser()
    {
        if (centerEyeAnchor == null || menuCanvas == null) return;

        // Forward direction, flattened on the XZ plane so the menu stays upright
        Vector3 forward = centerEyeAnchor.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 eyePos = centerEyeAnchor.position;
        Vector3 targetPos = eyePos
            + forward * spawnDistance
            + Vector3.up * verticalOffset;

        menuCanvas.transform.position = targetPos;

        // Rotate the menu to face the user
        menuCanvas.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
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

    /// <summary>
    /// Call this to hide and re-show the menu (e.g. from a wrist button).
    /// </summary>
    public void ToggleMenu()
    {
        bool isVisible = menuCanvas.gameObject.activeSelf && _canvasGroup.alpha > 0.5f;
        if (isVisible)
            StartCoroutine(FadeOut());
        else
        {
            PlaceMenuInFrontOfUser();
            StartCoroutine(FadeIn());
        }
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
