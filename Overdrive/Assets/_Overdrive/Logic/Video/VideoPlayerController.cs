using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Controls the floating video player window: play/pause, ±10s skip,
/// scrub bar, and back-to-menu. Opened by clicking a race thumbnail
/// in the main menu grid. The window is draggable via WindowHandle.
/// </summary>
public class VideoPlayerController : MonoBehaviour
{
    public static VideoPlayerController Instance { get; private set; }

    [Header("References (auto-wired by builder)")]
    public VideoPlayer videoPlayer;
    public Slider scrubSlider;
    public TextMeshProUGUI playPauseLabel;
    public TextMeshProUGUI timeLabel;
    public TextMeshProUGUI titleLabel;
    public Canvas menuCanvas;     // main menu to return to

    [Header("Live Standings")]
    [Tooltip("The RankingWidget shown next to the video (auto-found if empty)")]
    public GameObject rankingWidget;
    [Tooltip("Horizontal offset of the ranking widget from the player (metres)")]
    public float rankingSideOffset = 1.0f;

    private CanvasGroup _cg;
    private bool _seeking;

    private void Awake()
    {
        Instance = this;
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();

        if (menuCanvas == null)
        {
            var go = GameObject.Find("OverdriveMenuCanvas");
            if (go != null) menuCanvas = go.GetComponent<Canvas>();
        }

        // Find the ranking widget even if it is inactive in the scene
        if (rankingWidget == null)
        {
            var mgr = FindFirstObjectByType<RaceRankingManager>(FindObjectsInactive.Include);
            if (mgr != null) rankingWidget = mgr.gameObject;
        }

        if (scrubSlider != null)
            scrubSlider.onValueChanged.AddListener(OnScrub);

        // Buttons must be wired at RUNTIME: editor-time AddListener calls are
        // not serialized into the scene, so they'd be dead in a build.
        WireButton("VideoSurface/BackButton", BackToMenu);
        WireButton("PlayPause", TogglePlayPause);
        WireButton("Rewind10", () => SkipSeconds(-10f));
        WireButton("Forward10", () => SkipSeconds(+10f));

        gameObject.SetActive(false);
    }

    private void WireButton(string path, UnityEngine.Events.UnityAction action)
    {
        var t = transform.Find(path);
        if (t == null) { Debug.LogWarning("[VideoPlayer] Button not found: " + path); return; }
        var btn = t.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    // ── open / close ─────────────────────────────────────────────────────────
    public void Open(string title)
    {
        gameObject.SetActive(true);

        if (titleLabel != null) titleLabel.text = title;

        // Take the menu's place in space, then hide the menu
        if (menuCanvas != null)
        {
            transform.position = menuCanvas.transform.position;
            transform.rotation = menuCanvas.transform.rotation;
            menuCanvas.gameObject.SetActive(false);
        }
        else
        {
            PlaceInFrontOfUser();
        }

        if (videoPlayer != null)
        {
            videoPlayer.time = 0;
            videoPlayer.Play();
        }
        UpdatePlayLabel();
        ShowRanking();

        // Fade only (no scale): the root's own localScale is the baked 0.001
        // world-space canvas scale, so UITransitions must not touch transform here.
        StopAllCoroutines();
        StartCoroutine(UITransitions.FadeScaleIn(_cg, null));
    }

    public void BackToMenu()
    {
        StopAllCoroutines();
        StartCoroutine(BackToMenuRoutine());
    }

    private IEnumerator BackToMenuRoutine()
    {
        yield return UITransitions.FadeScaleOut(_cg, null);

        if (videoPlayer != null) videoPlayer.Stop();

        if (rankingWidget != null) rankingWidget.SetActive(false);

        if (menuCanvas != null)
        {
            menuCanvas.transform.position = transform.position;
            menuCanvas.transform.rotation = transform.rotation;
            menuCanvas.gameObject.SetActive(true);
        }
        gameObject.SetActive(false);
    }

    /// <summary>Places the standings widget beside the video and shows it.</summary>
    private void ShowRanking()
    {
        if (rankingWidget == null) return;

        rankingWidget.transform.position =
            transform.position + transform.right * rankingSideOffset;
        rankingWidget.transform.rotation = transform.rotation;
        rankingWidget.SetActive(true);
    }

    // ── transport controls ────────────────────────────────────────────────────
    public void TogglePlayPause()
    {
        if (videoPlayer == null) return;
        if (videoPlayer.isPlaying) videoPlayer.Pause();
        else videoPlayer.Play();
        UpdatePlayLabel();
    }

    public void SkipSeconds(float delta)
    {
        if (videoPlayer == null || videoPlayer.length <= 0) return;
        videoPlayer.time = Mathf.Clamp((float)videoPlayer.time + delta,
                                       0f, (float)videoPlayer.length - 0.1f);
    }

    // ── scrub bar ─────────────────────────────────────────────────────────────
    private void OnScrub(float normalized)
    {
        if (_seeking || videoPlayer == null || videoPlayer.length <= 0) return;
        _seeking = true;
        videoPlayer.time = normalized * videoPlayer.length;
        _seeking = false;
    }

    private void Update()
    {
        if (videoPlayer == null || videoPlayer.length <= 0) return;

        float norm = (float)(videoPlayer.time / videoPlayer.length);

        // SetValueWithoutNotify avoids seek feedback loop while playing
        if (scrubSlider != null && !_seeking)
            scrubSlider.SetValueWithoutNotify(norm);

        if (timeLabel != null)
            timeLabel.text = Fmt(videoPlayer.time) + " / " + Fmt(videoPlayer.length);
    }

    // ── helpers ───────────────────────────────────────────────────────────────
    private void UpdatePlayLabel()
    {
        if (playPauseLabel != null)
            playPauseLabel.text = (videoPlayer != null && videoPlayer.isPlaying) ? "❚❚" : "▶";
    }

    private static string Fmt(double t)
    {
        int m = (int)(t / 60), s = (int)(t % 60);
        return $"{m:0}:{s:00}";
    }

    private void PlaceInFrontOfUser()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 fwd = cam.transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        fwd.Normalize();
        transform.position = cam.transform.position + fwd * 1.6f;
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }
}
