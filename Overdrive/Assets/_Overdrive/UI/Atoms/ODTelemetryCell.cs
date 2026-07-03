/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTelemetryCell - Atom displaying a caption label and a large animated value for driver telemetry data.
 ##
 */

using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Two-line telemetry cell: a small caption label (e.g. "KM/H") above a large bold value (e.g. "312").
/// Used inside ODDriverCard's 2-column GridLayout. When a value changes, the value text
/// punches up in scale (1.0→1.15→1.0) to draw the viewer's eye to the update.
/// </summary>
public class ODTelemetryCell : MonoBehaviour
{
    [Header("References — wired by creator")]
    /// <summary>Caption label above the value — typically the metric name (e.g. "GEAR", "KM/H").</summary>
    public TextMeshProUGUI labelText;
    /// <summary>Large value text — the live data (e.g. "7", "312").</summary>
    public TextMeshProUGUI valueText;

    [Header("Content")]
    public string label = "LABEL";
    public string value = "—";

    private void Start()
    {
        Sync();
    }

    /// <summary>Pushes current label/value strings and theme styles to both TMP components.</summary>
    private void Sync()
    {
        UITheme theme = UITheme.Instance;

        if (labelText != null)
        {
            labelText.text      = label;
            labelText.fontStyle = FontStyles.Normal;
            if (theme != null)
            {
                labelText.fontSize = theme.captionSize;
                labelText.color    = theme.textSecondary;
            }
        }

        if (valueText != null)
        {
            valueText.text      = value;
            valueText.fontStyle = FontStyles.Bold;
            if (theme != null)
            {
                valueText.fontSize = theme.h2Size;
                valueText.color    = theme.textPrimary;
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>Updates the caption label text without triggering an animation.</summary>
    public void SetLabel(string text)
    {
        label = text;
        if (labelText != null) labelText.text = text;
    }

    /// <summary>Updates the value and plays the scale-punch animation to signal a data change.</summary>
    public void SetValue(string text)
    {
        value = text;
        if (valueText != null) valueText.text = text;
        StartCoroutine(AnimateValueChange(text));
    }

    /// <summary>
    /// Scales the value transform up to 1.15× then back to 1.0 over 0.18s using a sine arc.
    /// The new value string is applied mid-animation so the expansion happens on the fresh number.
    /// </summary>
    public IEnumerator AnimateValueChange(string newValue)
    {
        if (valueText == null) yield break;

        const float duration   = 0.18f;
        const float peakScale  = 1.15f;
        float       elapsed    = 0f;
        Transform   vt         = valueText.transform;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t     = elapsed / duration;
            float scale = Mathf.Lerp(1f, peakScale, Mathf.Sin(t * Mathf.PI));
            vt.localScale = Vector3.one * scale;
            yield return null;
        }

        vt.localScale  = Vector3.one;
        valueText.text = newValue;
    }
}
