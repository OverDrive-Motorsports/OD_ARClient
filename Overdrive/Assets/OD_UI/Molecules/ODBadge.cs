/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODBadge - Molecule combining a rounded background and a label into a compact status badge with themed color variants.
 ##
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small pill badge with four semantic color variants: Default (grey), Gold, Danger (red), and Success (green).
/// Reads colors from UITheme so the badge adapts automatically when the theme is updated.
/// </summary>
public class ODBadge : MonoBehaviour
{
    /// <summary>Determines the background and text color pair for this badge.</summary>
    public enum BadgeVariant
    {
        /// <summary>Neutral grey — used for labels like "DNF" or "P10".</summary>
        Default,
        /// <summary>Championship gold — used for top positions like "P1".</summary>
        Gold,
        /// <summary>Red danger — used for warnings or penalties.</summary>
        Danger,
        /// <summary>Green success — used for positive status indicators.</summary>
        Success
    }

    [Header("Style")]
    public BadgeVariant variant = BadgeVariant.Default;

    [Header("References")]
    public ODBackground background;
    public ODLabel      label;

    private void Start()
    {
        if (background == null) background = GetComponentInChildren<ODBackground>();
        if (label == null)      label      = GetComponentInChildren<ODLabel>();
        Apply();
    }

    /// <summary>Applies the background and label colors for the current variant from UITheme.</summary>
    private void Apply()
    {
        UITheme theme = UITheme.Instance;
        if (theme == null) return;

        Image           bgImg = background != null ? background.GetComponent<Image>() : null;
        TextMeshProUGUI txt   = label      != null ? label.GetComponent<TextMeshProUGUI>() : null;

        switch (variant)
        {
            case BadgeVariant.Default:
                // Low-contrast grey: subtle on dark panels
                SetColors(bgImg, txt,
                    new Color(0.557f, 0.557f, 0.576f, 0.12f),
                    theme.textSecondary);
                break;
            case BadgeVariant.Gold:
                // Translucent gold background, full-opacity gold text
                SetColors(bgImg, txt,
                    new Color(theme.accentGold.r, theme.accentGold.g, theme.accentGold.b, 0.15f),
                    theme.accentGold);
                break;
            case BadgeVariant.Danger:
                SetColors(bgImg, txt,
                    new Color(theme.dangerColor.r, theme.dangerColor.g, theme.dangerColor.b, 0.15f),
                    theme.dangerColor);
                break;
            case BadgeVariant.Success:
                SetColors(bgImg, txt,
                    new Color(theme.successColor.r, theme.successColor.g, theme.successColor.b, 0.15f),
                    theme.successColor);
                break;
        }
    }

    private static void SetColors(Image bg, TextMeshProUGUI txt, Color bgColor, Color textColor)
    {
        if (bg  != null) bg.color  = bgColor;
        if (txt != null) txt.color = textColor;
    }

    /// <summary>Updates the badge label text.</summary>
    public void SetText(string text) => label?.SetText(text);

    /// <summary>Switches to a new variant and immediately repaints colors.</summary>
    public void SetVariant(BadgeVariant v)
    {
        variant = v;
        Apply();
    }
}
