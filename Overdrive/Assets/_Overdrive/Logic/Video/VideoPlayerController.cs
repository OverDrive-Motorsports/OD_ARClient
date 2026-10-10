using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Course-owned TV panel. Never replaces or hides the course window.</summary>
public sealed class VideoPlayerController : MonoBehaviour
{
    public ODSportPage course;
    public VideoPlayer videoPlayer;
    public GameObject videoPanel;
    public GameObject standingsPanel;
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI playPauseLabel;
    public TextMeshProUGUI playbackStatus;
    public TextMeshProUGUI standingsHeading;
    public TextMeshProUGUI volumeLabel;
    public Button playPauseButton;
    public Button[] tvButtons;
    public Slider volumeSlider;
    public ScrollRect standingsScroll;
    public float volume = .7f;

    public bool IsVisible => videoPanel != null && videoPanel.activeSelf;
    public bool WantsPlayback => _wantsPlayback;
    private bool _bound;
    private bool _wantsPlayback;
    private string _context;
    private double _resumeTime;

    private void Awake() => Initialize();

    public void Initialize()
    {
        if (_bound) return;
        _bound = true;
        if (tvButtons != null)
            foreach (var button in tvButtons) button?.onClick.AddListener(ToggleVideo);
        playPauseButton?.onClick.AddListener(TogglePlayPause);
        volumeSlider?.onValueChanged.AddListener(SetVolume);
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += Prepared;
            videoPlayer.errorReceived += PlaybackError;
            videoPlayer.loopPointReached += PlaybackEnded;
        }
        SetVolume(volume);
        HideVideo();
    }

    public void Configure(ODSportPage.Mode mode, string context, string title)
    {
        Initialize();
        bool session = mode == ODSportPage.Mode.LiveSession || mode == ODSportPage.Mode.Replay;
        if (_context != context)
        {
            HideVideo();
            if (Application.isPlaying && videoPlayer != null) videoPlayer.Stop();
            _resumeTime = 0;
            _context = context;
            if (standingsScroll != null) standingsScroll.verticalNormalizedPosition = 1;
        }
        if (standingsPanel != null) standingsPanel.SetActive(session);
        if (standingsHeading != null) standingsHeading.text = mode == ODSportPage.Mode.Replay
            ? "Classement de la course" : "Classement de la session";
        if (titleLabel != null) titleLabel.text = (mode == ODSportPage.Mode.Replay ? "TV Replay" : "TV Live") + " · " + title;
        if (!session) HideVideo();
    }

    public void ToggleVideo()
    {
        if (course == null || !course.isActiveAndEnabled ||
            (course.currentMode != ODSportPage.Mode.LiveSession && course.currentMode != ODSportPage.Mode.Replay)) return;
        if (IsVisible) { HideVideo(); return; }
        videoPanel.SetActive(true);
        videoPanel.GetComponent<ODCourseVideoDock>()?.PlaceIfNeeded();
        var canvas = videoPanel.GetComponent<Canvas>();
        if (canvas != null) canvas.worldCamera = Camera.main;
        _wantsPlayback = true;
        if (Application.isPlaying && videoPlayer != null)
        {
            if (videoPlayer.isPrepared) { ApplyVolume(); videoPlayer.Play(); }
            else videoPlayer.Prepare();
        }
        if (playbackStatus != null) playbackStatus.text = videoPlayer != null && videoPlayer.isPrepared
            ? "Vidéo mock · Pas de flux connecté" : "Vidéo mock · Chargement…";
        UpdateControls();
    }

    public void HideVideo()
    {
        _wantsPlayback = false;
        if (Application.isPlaying && videoPlayer != null && videoPlayer.isPrepared)
        {
            _resumeTime = videoPlayer.time >= videoPlayer.length - .1 ? 0 : videoPlayer.time;
            videoPlayer.Pause();
        }
        if (videoPanel != null) videoPanel.SetActive(false);
        UpdateControls();
    }

    public void TogglePlayPause()
    {
        if (!IsVisible) return;
        _wantsPlayback = !_wantsPlayback;
        if (Application.isPlaying && videoPlayer != null)
        {
            if (!_wantsPlayback) videoPlayer.Pause();
            else if (videoPlayer.isPrepared) videoPlayer.Play();
            else videoPlayer.Prepare();
        }
        UpdateControls();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        volumeSlider?.SetValueWithoutNotify(volume);
        if (volumeLabel != null) volumeLabel.text = Mathf.RoundToInt(volume * 100) + "%";
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        if (!Application.isPlaying || videoPlayer == null || !videoPlayer.isPrepared) return;
        for (ushort i = 0; i < videoPlayer.controlledAudioTrackCount; i++)
        {
            if (videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct) videoPlayer.SetDirectAudioVolume(i, volume);
            else if (videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
            {
                var source = videoPlayer.GetTargetAudioSource(i);
                if (source != null) source.volume = volume;
            }
        }
    }

    private void Prepared(VideoPlayer player)
    {
        ApplyVolume();
        if (_resumeTime > 0 && player.canSetTime) player.time = _resumeTime;
        if (IsVisible && _wantsPlayback) player.Play();
        if (playbackStatus != null) playbackStatus.text = "Vidéo mock · Pas de flux connecté";
        UpdateControls();
    }

    private void PlaybackError(VideoPlayer player, string message)
    {
        _wantsPlayback = false;
        if (playbackStatus != null) playbackStatus.text = "Vidéo indisponible · Réessayer avec Lecture";
        Debug.LogWarning("[Course TV] " + message);
        UpdateControls();
    }

    private void PlaybackEnded(VideoPlayer player) { _wantsPlayback = false; UpdateControls(); }

    private void UpdateControls()
    {
        if (playPauseLabel != null) playPauseLabel.text = _wantsPlayback ? "Pause" : "Lecture";
        if (tvButtons == null) return;
        foreach (var button in tvButtons)
        {
            if (button == null) continue;
            var colors = button.colors;
            colors.normalColor = colors.selectedColor = IsVisible
                ? new Color(.40f, .65f, .88f, .85f) : new Color(.42f, .46f, .54f, .60f);
            button.colors = colors;
        }
    }

    private void OnDisable()
    {
        HideVideo();
        if (Application.isPlaying && videoPlayer != null) videoPlayer.Stop();
        _context = null;
        _resumeTime = 0;
    }

    private void OnDestroy()
    {
        if (!_bound) return;
        if (tvButtons != null)
            foreach (var button in tvButtons) button?.onClick.RemoveListener(ToggleVideo);
        playPauseButton?.onClick.RemoveListener(TogglePlayPause);
        volumeSlider?.onValueChanged.RemoveListener(SetVolume);
        if (videoPlayer == null) return;
        videoPlayer.prepareCompleted -= Prepared;
        videoPlayer.errorReceived -= PlaybackError;
        videoPlayer.loopPointReached -= PlaybackEnded;
    }
}
