/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODIcon - Atom for displaying a Sprite icon with an optional UITheme-driven tint color.
 ##
 */

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wraps an Image with a Sprite and tint. When tint is left at Color.white at Start(),
/// it automatically adopts UITheme.accentGold — so icons are branded by default.
/// </summary>
[RequireComponent(typeof(Image))]
public class ODIcon : MonoBehaviour
{
    [Header("Icon")]
    /// <summary>The sprite to display. Assignable from the Inspector or via SetIcon().</summary>
    public Sprite icon;
    /// <summary>Color.white is treated as "use UITheme.accentGold". Any other value overrides the theme.</summary>
    [Tooltip("Leave white to auto-apply UITheme.accentGold at Start.")]
    public Color tint = Color.white;

    private Image _image;

    private void Start()
    {
        _image = GetComponent<Image>();

        // White is a sentinel for "use default theme tint" — avoids hardcoding gold here
        if (tint == Color.white)
        {
            UITheme theme = UITheme.Instance;
            if (theme != null) tint = theme.accentGold;
        }

        Apply();

        // Default to a sensible square size when no size is set in the scene
        RectTransform rt = GetComponent<RectTransform>();
        if (rt.sizeDelta == Vector2.zero)
            rt.sizeDelta = new Vector2(48f, 48f);
    }

    /// <summary>Pushes the current sprite and tint values to the Image component.</summary>
    private void Apply()
    {
        if (_image == null) _image = GetComponent<Image>();
        if (icon != null) _image.sprite = icon;
        _image.color = tint;
    }

    /// <summary>Replaces the displayed sprite and optionally overrides the tint.</summary>
    public void SetIcon(Sprite s, Color? color = null)
    {
        icon = s;
        if (color.HasValue) tint = color.Value;
        Apply();
    }
}
