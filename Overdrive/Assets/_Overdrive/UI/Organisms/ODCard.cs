/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODCard - Reusable floating panel organism with title, optional divider, optional close button, and a free content area.
 ##
 */

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Modular floating card. The title, divider visibility, and close button can be
/// toggled at runtime without rebuilding the hierarchy. Drop any UI children
/// directly into ContentArea at design time or instantiate them at runtime.
/// Requires a CanvasGroup so ODModal can animate this card's alpha independently.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ODCard : MonoBehaviour
{
    [Header("Content")]
    public string title           = "Card Title";
    public bool   showDivider     = true;
    public bool   showCloseButton = false;

    [Header("References — wired by ODUIBuilder")]
    public ODLabel        titleLabel;
    public ODButton       closeButton;
    public GameObject     divider;
    /// <summary>RectTransform that acts as the parent for any content placed inside the card.</summary>
    public RectTransform  contentArea;

    private void Start()
    {
        Sync();
    }

    /// <summary>Pushes current field values to child references.</summary>
    private void Sync()
    {
        titleLabel?.SetText(title);
        if (divider     != null) divider.SetActive(showDivider);
        if (closeButton != null) closeButton.gameObject.SetActive(showCloseButton);
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>Changes the card title and updates the label immediately.</summary>
    public void SetTitle(string t)
    {
        title = t;
        titleLabel?.SetText(t);
    }

    /// <summary>Shows or hides the horizontal divider below the title.</summary>
    public void SetShowDivider(bool v)
    {
        showDivider = v;
        if (divider != null) divider.SetActive(v);
    }

    /// <summary>
    /// Shows or hides the close button. Optionally wires an action to the button's OnClick event.
    /// Removes existing listeners first to prevent duplicate callbacks on repeated calls.
    /// </summary>
    public void SetShowCloseButton(bool v, UnityAction onClose = null)
    {
        showCloseButton = v;
        if (closeButton == null) return;
        closeButton.gameObject.SetActive(v);
        if (onClose != null)
        {
            closeButton.OnClick.RemoveAllListeners();
            closeButton.OnClick.AddListener(onClose);
        }
    }
}
