/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODLabel - Atom for themed text with H1, H2, Body, and Caption style presets driven by UITheme.
 ##
 */

using UnityEngine;
using TMPro;

/// <summary>
/// Wraps TextMeshProUGUI with a style preset that maps to UITheme font sizes and colors.
/// Apply() is called on Awake and whenever SetStyle() is invoked so the label stays in sync
/// with the theme even if UITheme is hot-reloaded in the Editor.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class ODLabel : MonoBehaviour
{
    /// <summary>Controls which UITheme size/color tier this label uses.</summary>
    public enum TextStyle
    {
        /// <summary>Large bold display text. UITheme.h1Size, textPrimary.</summary>
        H1,
        /// <summary>Section heading. UITheme.h2Size, textPrimary, bold.</summary>
        H2,
        /// <summary>Standard readable copy. UITheme.bodySize, textPrimary.</summary>
        Body,
        /// <summary>Small supporting text. UITheme.captionSize, textSecondary.</summary>
        Caption
    }

    [Header("Style")]
    public TextStyle textStyle = TextStyle.Body;

    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        Apply();
    }

    /// <summary>Reads UITheme.Instance and applies the correct size, weight, and color for the current style.</summary>
    private void Apply()
    {
        if (_text == null) _text = GetComponent<TextMeshProUGUI>();
        UITheme theme = UITheme.Instance;
        if (theme == null) return;

        switch (textStyle)
        {
            case TextStyle.H1:
                _text.fontSize = theme.h1Size;
                _text.fontStyle = FontStyles.Bold;
                _text.color = theme.textPrimary;
                break;
            case TextStyle.H2:
                _text.fontSize = theme.h2Size;
                _text.fontStyle = FontStyles.Bold;
                _text.color = theme.textPrimary;
                break;
            case TextStyle.Body:
                _text.fontSize = theme.bodySize;
                _text.fontStyle = FontStyles.Normal;
                _text.color = theme.textPrimary;
                break;
            case TextStyle.Caption:
                _text.fontSize = theme.captionSize;
                _text.fontStyle = FontStyles.Normal;
                // Caption uses secondary (grey) to de-emphasize supporting information
                _text.color = theme.textSecondary;
                break;
        }
    }

    /// <summary>Updates the displayed string without changing the current style.</summary>
    public void SetText(string text)
    {
        if (_text == null) _text = GetComponent<TextMeshProUGUI>();
        _text.text = text;
    }

    /// <summary>Switches to a new style preset and immediately re-applies theme values.</summary>
    public void SetStyle(TextStyle style)
    {
        textStyle = style;
        Apply();
    }
}
