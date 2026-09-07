/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODModal - Full-screen blocking modal organism that animates an ODCard in and out over a dim overlay.
 ##
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Blocking modal dialog. Show() animates the inner ODCard from scale 0.92→1.0 and alpha 0→1 (ease-out cubic).
/// Hide() reverses the animation (ease-in quad, faster). The overlay optionally forwards taps to Hide().
/// The GameObject starts inactive so it consumes no layout space when hidden.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ODModal : MonoBehaviour
{
    [Header("Content")]
    public string title = "Modal Title";
    /// <summary>When true, tapping anywhere on the dim overlay closes the modal.</summary>
    public bool closeOnOverlayTap = true;

    [Header("References — wired by ODUIBuilder")]
    /// <summary>Full-screen semi-transparent Image that dims the scene behind the modal.</summary>
    public Image overlay;
    public ODCard card;

    private CanvasGroup _cardGroup;

    private void Awake()
    {
        // Resolve card's CanvasGroup for per-card alpha animation independent of the root CanvasGroup
        if (card != null)
        {
            _cardGroup = card.GetComponent<CanvasGroup>();
            if (_cardGroup == null)
                _cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        }

        // Wire overlay tap to Hide() so users can dismiss without a dedicated close button
        if (overlay != null && closeOnOverlayTap)
        {
            Button overlayBtn = overlay.GetComponent<Button>();
            if (overlayBtn == null)
                overlayBtn = overlay.gameObject.AddComponent<Button>();
            overlayBtn.onClick.AddListener(Hide);
        }
    }

    private void Start()
    {
        card?.SetTitle(title);
        // Start hidden — activated by Show() to avoid blocking interaction while idle
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>Activates the modal and plays the scale + alpha ease-in animation.</summary>
    public void Show()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateIn());
    }

    /// <summary>Plays the close animation, then deactivates the modal on completion.</summary>
    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateOut());
    }

    /// <summary>Updates the modal's title and forwards it to the inner ODCard.</summary>
    public void SetTitle(string t)
    {
        title = t;
        card?.SetTitle(t);
    }

    // ── Animation ─────────────────────────────────────────────────────────────────
    // Delegates to UITransitions so every reveal/dismiss in the app (page opens,
    // dropdowns, tab switches) shares this same easing instead of duplicating it.

    private IEnumerator AnimateIn()
    {
        Transform cardT = card != null ? card.transform : null;
        yield return UITransitions.FadeScaleIn(_cardGroup, cardT);
    }

    private IEnumerator AnimateOut()
    {
        Transform cardT = card != null ? card.transform : null;
        yield return UITransitions.FadeScaleOut(_cardGroup, cardT);
        gameObject.SetActive(false);
    }
}
