/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODInputField - Molecule wrapping TMP_InputField with themed colors, a focus border animation, and a change event.
 ##
 */

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Themed text input. Wraps TMP_InputField with UITheme colors and a border animation:
/// on focus the thin border lerps from borderColor → accentGold over 0.15s; on deselect it reverses.
/// Exposes a UnityEvent&lt;string&gt; so consumer code does not need to wire TMP events directly.
/// </summary>
public class ODInputField : MonoBehaviour
{
    [Header("References")]
    public ODBackground background;
    public TMP_InputField inputField;
    /// <summary>Thin Image stretched over the background that animates between border and gold on focus.</summary>
    public Image borderImage;

    [Header("Events")]
    public UnityEvent<string> OnValueChanged;

    /// <summary>The current text value. Returns empty string when inputField is null.</summary>
    public string Value => inputField != null ? inputField.text : string.Empty;

    private void Awake()
    {
        if (background == null) background = GetComponentInChildren<ODBackground>();
        if (inputField == null) inputField = GetComponentInChildren<TMP_InputField>();

        SetupColors();

        if (inputField != null)
        {
            // Animate border on focus/blur instead of snapping to avoid a jarring pop in VR
            inputField.onSelect.AddListener(_ => StartCoroutine(AnimateBorder(true)));
            inputField.onDeselect.AddListener(_ => StartCoroutine(AnimateBorder(false)));
            inputField.onValueChanged.AddListener(v => OnValueChanged?.Invoke(v));
        }
    }

    /// <summary>Applies theme colors to placeholder, text, and the border image.</summary>
    private void SetupColors()
    {
        UITheme theme = UITheme.Instance;
        if (theme == null || inputField == null) return;

        if (inputField.placeholder is TextMeshProUGUI ph)
            ph.color = theme.textSecondary;
        if (inputField.textComponent is TextMeshProUGUI tc)
            tc.color = theme.textPrimary;
        if (borderImage != null)
            borderImage.color = theme.borderColor;
    }

    /// <summary>Changes the placeholder hint text.</summary>
    public void SetPlaceholder(string text)
    {
        if (inputField != null && inputField.placeholder is TextMeshProUGUI ph)
            ph.text = text;
    }

    /// <summary>Lerps the border color between borderColor and accentGold over 0.15s.</summary>
    private IEnumerator AnimateBorder(bool focused)
    {
        UITheme theme = UITheme.Instance;
        if (theme == null || borderImage == null) yield break;

        Color from = borderImage.color;
        Color to = focused ? theme.accentGold : theme.borderColor;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            borderImage.color = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        borderImage.color = to;
    }
}
