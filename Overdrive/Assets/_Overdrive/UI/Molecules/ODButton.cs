/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODButton - Molecule combining background, label, and optional icon into a styled interactive button.
 ##
 */

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Themed interactive button with three style variants: Primary (gold fill), Ghost (outline), Danger (red fill).
/// Implements VR-compatible pointer events. Click plays a 0.1s scale-punch feedback coroutine.
/// A CanvasGroup on the root provides hover alpha without affecting child layout.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ODButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    /// <summary>Visual preset that controls background fill, text color, and optional outline.</summary>
    public enum ButtonStyle
    {
        /// <summary>Gold filled — primary call-to-action.</summary>
        Primary,
        /// <summary>Transparent with border outline — secondary action.</summary>
        Ghost,
        /// <summary>Red filled — destructive or warning action.</summary>
        Danger
    }

    [Header("Style")]
    public ButtonStyle buttonStyle = ButtonStyle.Primary;

    [Header("References")]
    public ODBackground background;
    public ODLabel label;
    /// <summary>Optional icon shown alongside the label. Disabled by default in the prefab.</summary>
    public ODIcon icon;

    [Header("Events")]
    public UnityEvent OnClick;

    private CanvasGroup _canvasGroup;

    private void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (background == null) background = GetComponentInChildren<ODBackground>();
        if (label == null) label = GetComponentInChildren<ODLabel>();
        if (icon == null) icon = GetComponentInChildren<ODIcon>(true);

        Apply();
    }

    /// <summary>Applies background color, text color, and outline based on the current ButtonStyle.</summary>
    private void Apply()
    {
        UITheme theme = UITheme.Instance;
        if (theme == null || background == null) return;

        Image bgImg = background.GetComponent<Image>();
        TextMeshProUGUI txt = label != null ? label.GetComponent<TextMeshProUGUI>() : null;
        Outline outline = background.GetComponent<Outline>();

        switch (buttonStyle)
        {
            case ButtonStyle.Primary:
                bgImg.color = theme.accentGold;
                if (txt != null) txt.color = Color.white;
                if (outline != null) outline.enabled = false;
                break;

            case ButtonStyle.Ghost:
                bgImg.color = Color.clear;
                if (txt != null) txt.color = theme.textPrimary;
                // Add Outline lazily so the prefab doesn't require one pre-attached
                if (outline == null) outline = background.gameObject.AddComponent<Outline>();
                outline.effectColor = theme.borderColor;
                outline.effectDistance = new Vector2(theme.borderWidth, -theme.borderWidth);
                outline.enabled = true;
                break;

            case ButtonStyle.Danger:
                bgImg.color = theme.dangerColor;
                if (txt != null) txt.color = Color.white;
                if (outline != null) outline.enabled = false;
                break;
        }
    }

    /// <summary>Changes the displayed label text.</summary>
    public void SetLabel(string text)
    {
        if (label == null) label = GetComponentInChildren<ODLabel>();
        label?.SetText(text);
    }

    /// <summary>Switches to a new style preset and immediately repaints the button.</summary>
    public void SetStyle(ButtonStyle style)
    {
        buttonStyle = style;
        Apply();
    }

    // ── VR pointer events ─────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Slight dim on hover to signal interactivity without a jarring color swap in VR
        if (_canvasGroup != null) _canvasGroup.alpha = 0.85f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        StartCoroutine(ClickPunch());
        OnClick?.Invoke();
    }

    /// <summary>
    /// Scales the button down to 96% then back over 0.1s total.
    /// Haptic-like visual feedback without requiring a gamepad.
    /// </summary>
    private IEnumerator ClickPunch()
    {
        Vector3 original = transform.localScale;
        Vector3 pressed = original * 0.96f;
        float half = 0.05f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(original, pressed, elapsed / half);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(pressed, original, elapsed / half);
            yield return null;
        }

        transform.localScale = original;
    }
}
