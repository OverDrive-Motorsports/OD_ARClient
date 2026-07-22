/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODMediaControls - Pill-shaped media control bar organism with transport buttons, live badge, and progress slider.
 ##
 */

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 400×72 pill-shaped media control bar. Contains transport buttons (Rewind / PlayPause / Forward),
/// an ODLiveBadge, and a Unity Slider for seek control. When live (isLive=true) the progress
/// slider is typically hidden or locked at 1.0; the badge shows "LIVE".
/// Unicode symbols are used for icons to avoid depending on a font atlas.
/// </summary>
public class ODMediaControls : MonoBehaviour
{
    [Header("References — wired by ODUIBuilder")]
    public ODButton      rewindBtn;
    public ODButton      playPauseBtn;
    public ODButton      forwardBtn;
    public ODLiveBadge   liveBadge;
    public Slider        progressSlider;
    /// <summary>Background Image of the pill. Tinted with panelBackground at Start.</summary>
    public Image         background;

    [Header("State")]
    public bool  isPlaying = false;
    public bool  isLive    = false;
    [Range(0f, 1f)]
    public float progress  = 0f;

    [Header("Events")]
    /// <summary>Fires when the user moves the progress slider. Value is normalized 0–1.</summary>
    public UnityEvent<float> OnSeek;

    // Unicode symbols used instead of icon sprites to keep the build asset-free
    private static readonly string PlayIcon    = "▶";
    private static readonly string PauseIcon   = "⏸";
    private static readonly string RewindIcon  = "↺";
    private static readonly string ForwardIcon = "↻";

    private void Start()
    {
        Sync();
        WireSlider();
    }

    /// <summary>Pushes current state values (play/live/progress) to all child references.</summary>
    private void Sync()
    {
        UpdatePlayPauseIcon();
        if (liveBadge      != null) liveBadge.SetLive(isLive);
        if (progressSlider != null) progressSlider.value = progress;

        UITheme theme = UITheme.Instance;
        if (background != null && theme != null)
            background.color = theme.panelBackground;
    }

    /// <summary>Attaches the slider's onValueChanged listener to forward seek events to consumers.</summary>
    private void WireSlider()
    {
        if (progressSlider == null) return;
        progressSlider.onValueChanged.RemoveAllListeners();
        progressSlider.onValueChanged.AddListener(v =>
        {
            progress = v;
            OnSeek?.Invoke(v);
        });
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>Toggles play/pause state and updates the PlayPause button icon.</summary>
    public void SetPlaying(bool playing)
    {
        isPlaying = playing;
        UpdatePlayPauseIcon();
    }

    /// <summary>Switches the live badge state.</summary>
    public void SetLive(bool live)
    {
        isLive = live;
        if (liveBadge != null) liveBadge.SetLive(live);
    }

    /// <summary>Moves the slider to the given normalized position (0–1).</summary>
    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        if (progressSlider != null) progressSlider.value = progress;
    }

    // ── Internal ─────────────────────────────────────────────────────────────────

    /// <summary>Sets the PlayPause button text to ▶ or ⏸ based on current isPlaying state.</summary>
    private void UpdatePlayPauseIcon()
    {
        if (playPauseBtn == null) return;
        var tmp = playPauseBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = isPlaying ? PauseIcon : PlayIcon;
    }
}
