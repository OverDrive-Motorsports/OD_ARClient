/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODLiveBadge - Atom displaying a pulsing LIVE or OFFLINE indicator pill badge.
 ##
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pill-shaped status badge that shows "LIVE" with a pulsing red dot, or "OFFLINE" with a grey dot.
/// Child references (background, dot, label) are wired by ODUIBuilder.
/// </summary>
public class ODLiveBadge : MonoBehaviour
{
    [Header("References — wired by ODUIBuilder")]
    /// <summary>Pill background Image — tinted red at low opacity when live, grey when offline.</summary>
    public Image background;
    /// <summary>Small circular dot Image that animates scale when live.</summary>
    public Image dot;
    /// <summary>Text label that reads "LIVE" or "OFFLINE".</summary>
    public TextMeshProUGUI badgeLabel;

    [Header("State")]
    public bool isLive = true;

    private void Start()
    {
        Apply();
    }

    /// <summary>Applies the correct colors, label text, and starts or stops the dot pulse animation.</summary>
    private void Apply()
    {
        UITheme theme = UITheme.Instance;
        bool live = isLive;

        if (background != null && theme != null)
            // Semi-transparent tinted pill: red when live, grey when offline
            background.color = live
                ? new Color(theme.dangerColor.r, theme.dangerColor.g, theme.dangerColor.b, 0.20f)
                : new Color(theme.textSecondary.r, theme.textSecondary.g, theme.textSecondary.b, 0.15f);

        if (dot != null && theme != null)
            dot.color = live ? theme.dangerColor : theme.textTertiary;

        if (badgeLabel != null && theme != null)
        {
            badgeLabel.text = live ? "LIVE" : "OFFLINE";
            badgeLabel.color = live ? theme.textPrimary : theme.textSecondary;
            badgeLabel.fontSize = theme.captionSize;
            badgeLabel.fontStyle = FontStyles.Bold;
        }

        // Restart pulse only when transitioning states; StopAll prevents ghost coroutines
        StopAllCoroutines();
        if (live && dot != null)
            StartCoroutine(DotPulse());
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>Switches between LIVE and OFFLINE states and updates all visuals immediately.</summary>
    public void SetLive(bool live)
    {
        isLive = live;
        Apply();
    }

    // ── Animation ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Scales the dot 1.0→1.4→1.0 on a sine curve over a 1.2s period.
    /// Uses localScale so it doesn't disturb the layout.
    /// </summary>
    private IEnumerator DotPulse()
    {
        const float period = 1.2f;
        const float minScale = 1.0f;
        const float maxScale = 1.4f;

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < period)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / period;
                float scale = Mathf.Lerp(minScale, maxScale, Mathf.Sin(t * Mathf.PI));
                dot.transform.localScale = Vector3.one * scale;
                yield return null;
            }
        }
    }
}
