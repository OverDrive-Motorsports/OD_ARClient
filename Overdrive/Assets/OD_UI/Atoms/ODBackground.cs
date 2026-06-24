/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODBackground - Atom that applies a themed semi-transparent panel background color based on the chosen Style.
 ##
 */

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies one of four glassmorphism background colors from UITheme to the attached Image.
/// Also triggers ODBlurBackground on non-Subtle panels so frosted-glass effect stays current.
/// </summary>
[RequireComponent(typeof(Image))]
public class ODBackground : MonoBehaviour
{
    /// <summary>Determines which UITheme color slot is used for this panel.</summary>
    public enum Style
    {
        /// <summary>Primary frosted panel — UITheme.panelBackground.</summary>
        Card,
        /// <summary>Deeper modal surface — UITheme.surfaceColor.</summary>
        Modal,
        /// <summary>Low-emphasis overlay — UITheme.subtleColor (no blur).</summary>
        Subtle,
        /// <summary>Elevated panel variant — UITheme.panelBackgroundAlt.</summary>
        Alt
    }

    [Header("Style")]
    public Style backgroundStyle = Style.Card;

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        Apply();
    }

    private void OnEnable()
    {
        // Subtle panels are too dark/transparent for blur to add value; skip for performance
        if (backgroundStyle != Style.Subtle)
        {
            ODBlurBackground blur = GetComponent<ODBlurBackground>();
            if (blur == null) blur = gameObject.AddComponent<ODBlurBackground>();
            if (blur.autoRefreshOnEnable) blur.RefreshBlur();
        }
    }

    /// <summary>Reads UITheme.Instance and writes the correct color to the Image component.</summary>
    private void Apply()
    {
        if (_image == null) _image = GetComponent<Image>();
        UITheme theme = UITheme.Instance;
        if (theme == null) return;

        switch (backgroundStyle)
        {
            case Style.Card:   _image.color = theme.panelBackground;    break;
            case Style.Modal:  _image.color = theme.surfaceColor;       break;
            case Style.Subtle: _image.color = theme.subtleColor;        break;
            case Style.Alt:    _image.color = theme.panelBackgroundAlt; break;
        }
    }

    /// <summary>Overrides only the alpha channel without changing the theme color.</summary>
    public void SetAlpha(float a)
    {
        if (_image == null) _image = GetComponent<Image>();
        Color c = _image.color;
        c.a = a;
        _image.color = c;
    }

    /// <summary>Switches to a new style and immediately re-applies theme colors.</summary>
    public void SetStyle(Style style)
    {
        backgroundStyle = style;
        Apply();
    }
}
