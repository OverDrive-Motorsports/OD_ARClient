using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Race-window TV panel. Never replaces or hides the race window.</summary>
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
    public Slider scrubSlider;
    public TextMeshProUGUI timeLabel;
    // Kept for the historical Editor builder; navigation now belongs to the course.
    [System.Obsolete("Legacy builder reference; hiding TV no longer changes the menu canvas.")]
    public Canvas menuCanvas;
    public ScrollRect standingsScroll;
    public float volume = .7f;

    public bool IsVisible => videoPanel != null && videoPanel.activeSelf;
    public bool WantsPlayback => _wantsPlayback && (CurrentStream == null || CurrentStream.Loading || (CurrentStream.Ready && !CurrentStream.Finished));
    public string SessionId => _sessionId;
    public bool CatalogueLoading { get; private set; }
    public VideoPlayer ActivePlayer => CurrentStream != null ? CurrentStream.Player : videoPlayer;
    private bool _bound;
    private bool _wantsPlayback;
    private string _context;
    private string _sessionId;
    private ODRemoteVideoStream _mainStream;
    private int _catalogueVersion;
    private bool _catalogueRequested;
    private readonly Dictionary<int, ODRemoteVideoStream> _driverStreams = new Dictionary<int, ODRemoteVideoStream>();
    private ODVideoPlayerView View => videoPanel != null ? videoPanel.GetComponent<ODVideoPlayerView>() : null;
    private ODRemoteVideoStream CurrentStream => StreamForFeed(View != null ? View.PrimaryFeed : -1);

    private ODRemoteVideoStream StreamForFeed(int feed)
    {
        if (feed < 0) return _mainStream;
        int number = View != null ? View.DriverNumber(feed) : 0;
        return _driverStreams.TryGetValue(number, out var stream) ? stream : null;
    }
    public Texture TextureForFeed(int feed) => StreamForFeed(feed)?.Texture;
    public string StatusForFeed(int feed) => StreamForFeed(feed)?.Status ?? "Vidéo indisponible";

    private void Awake() => Initialize();

    public void Initialize()
    {
        if (_bound) return;
        _bound = true;
        if (View != null) View.Bind(this);
        if (tvButtons != null)
            foreach (var button in tvButtons) button?.onClick.AddListener(ToggleVideo);
        playPauseButton?.onClick.AddListener(TogglePlayPause);
        volumeSlider?.onValueChanged.AddListener(SetVolume);
        scrubSlider?.onValueChanged.AddListener(SeekNormalized);
        if (videoPlayer != null && Application.isPlaying)
        {
            _mainStream = new ODRemoteVideoStream(videoPlayer, false);
            _mainStream.Changed += SourcesChanged;
        }
        SetVolume(volume);
        HideVideo();
    }

    public void Configure(ODSportPage.Mode mode, string context, string title, string sessionId = null)
    {
        Initialize();
        bool session = mode == ODSportPage.Mode.LiveSession || mode == ODSportPage.Mode.Replay;
        if (_context != context || _sessionId != sessionId)
        {
            HideVideo();
            ++_catalogueVersion; CatalogueLoading = false; _catalogueRequested = false;
            if (View != null) View.ResetDriverCatalogue();
            if (videoPanel != null)
            {
                var view = videoPanel.GetComponent<ODVideoPlayerView>();
                if (view != null) view.ResetViews();
            }
            _mainStream?.Reset();
            DisposeDriverStreams();
            _context = context;
            _sessionId = sessionId;
            if (standingsScroll != null) standingsScroll.verticalNormalizedPosition = 1;
        }
        if (standingsPanel != null) standingsPanel.SetActive(session);
        if (standingsHeading != null) standingsHeading.text = mode == ODSportPage.Mode.Replay
            ? "Race standings" : "Session standings";
        if (titleLabel != null) titleLabel.text = videoPanel != null && videoPanel.GetComponent<ODVideoPlayerView>() != null
            ? title : (mode == ODSportPage.Mode.Replay ? "TV Replay" : "TV Live") + " · " + title;
        if (!session) HideVideo();
        RefreshView();
    }

    public void ToggleVideo()
    {
        if (videoPanel == null || course == null || !course.isActiveAndEnabled ||
            (course.currentMode != ODSportPage.Mode.LiveSession && course.currentMode != ODSportPage.Mode.Replay)) return;
        if (IsVisible) { HideVideo(); return; }
        videoPanel.SetActive(true);
        videoPanel.GetComponent<ODCourseVideoDock>()?.PlaceIfNeeded();
        var canvas = videoPanel.GetComponent<Canvas>();
        if (canvas != null) canvas.worldCamera = Camera.main;
        _wantsPlayback = true;
        LoadDrivers();
        if (View != null) View.SyncSources();
        StartRequestedStreams();
        UpdateControls();
    }

    public void HideVideo()
    {
        _wantsPlayback = false;
        _mainStream?.Suspend();
        foreach (var stream in _driverStreams.Values) stream.Suspend();
        if (videoPanel != null) videoPanel.SetActive(false);
        UpdateControls();
    }

    public void TogglePlayPause()
    {
        if (!IsVisible) return;
        _wantsPlayback = !WantsPlayback;
        if (_wantsPlayback) StartRequestedStreams(retryFailed: true);
        SetPlaybackOnStreams();
        UpdateControls();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(volume);
        if (volumeLabel != null) volumeLabel.text = Mathf.RoundToInt(volume * 100) + "%";
        ApplyVolume();
        RefreshView();
    }

    public void SeekNormalized(float value)
    {
        var player = ActivePlayer;
        if (player != null && player.isPrepared && player.canSetTime && player.length > 0)
            player.time = Mathf.Clamp01(value) * player.length;
    }

    public void SkipSeconds(float seconds)
    {
        var player = ActivePlayer;
        if (player != null && player.isPrepared && player.canSetTime)
            player.time = System.Math.Max(0, System.Math.Min(player.length, player.time + seconds));
    }

    [System.Obsolete("Use HideVideo; the course remains visible.")]
    public void BackToMenu() => HideVideo();

    private void Update()
    {
        if (!IsVisible) return;
        _mainStream?.Tick();
        foreach (var stream in _driverStreams.Values) stream.Tick();
        var player = ActivePlayer;
        bool ready = player != null && player.isPrepared;
        if (scrubSlider != null)
        {
            scrubSlider.interactable = ready && player.canSetTime && player.length > 0;
            scrubSlider.SetValueWithoutNotify(ready && player.length > 0 ? (float)(player.time / player.length) : 0);
        }
        if (timeLabel != null)
            timeLabel.text = ready ? System.TimeSpan.FromSeconds(System.Math.Max(0, player.time)).ToString(@"h\:mm\:ss") : "0:00:00";
    }

    private void ApplyVolume()
    {
        var current = CurrentStream;
        _mainStream?.SetVolume(current == _mainStream ? volume : 0);
        foreach (var stream in _driverStreams.Values) stream.SetVolume(stream == current ? volume : 0);
    }

    /// <summary>Only selected cameras allocate a decoder. Switching layout reuses the same streams.</summary>
    public void SyncViews(IEnumerable<int> feeds, int primary)
    {
        if (!Application.isPlaying || View == null) return;
        var numbers = new HashSet<int>(feeds.Append(primary).Where(i => i >= 0).Select(View.DriverNumber).Where(n => n > 0));
        foreach (int number in _driverStreams.Keys.Where(n => !numbers.Contains(n)).ToArray())
        { _driverStreams[number].Dispose(); _driverStreams.Remove(number); }
        foreach (int number in numbers)
        {
            if (_driverStreams.ContainsKey(number)) continue;
            var go = new GameObject("DriverBroadcast_" + number);
            go.transform.SetParent(transform, false);
            var stream = new ODRemoteVideoStream(go.AddComponent<VideoPlayer>(), true);
            stream.Changed += SourcesChanged; _driverStreams.Add(number, stream);
        }
        if (IsVisible && _wantsPlayback) StartRequestedStreams();
        UpdateControls();
    }

    private void StartRequestedStreams(bool retryFailed = false)
    {
        if (!Application.isPlaying || !IsVisible) return;
        if (_mainStream != null && (!_mainStream.Requested || (retryFailed && !_mainStream.Ready && !_mainStream.Loading)))
            _ = _mainStream.LoadAsync(_sessionId);
        foreach (var pair in _driverStreams)
            if (!pair.Value.Requested || (retryFailed && !pair.Value.Ready && !pair.Value.Loading))
                _ = pair.Value.LoadAsync(_sessionId, pair.Key);
        SetPlaybackOnStreams();
        ApplyVolume();
    }

    private async void LoadDrivers()
    {
        if (!Application.isPlaying || _catalogueRequested || string.IsNullOrWhiteSpace(_sessionId)) return;
        _catalogueRequested = true; CatalogueLoading = true;
        int version = ++_catalogueVersion;
        RefreshView();
        try
        {
            var drivers = await ODBackendApi.GetSessionDrivers(_sessionId);
            if (this == null || version != _catalogueVersion) return;
            if (drivers == null || drivers.Count == 0) _catalogueRequested = false;
            if (View != null) View.SetDrivers(drivers);
        }
        catch (System.Exception)
        {
            if (this == null || version != _catalogueVersion) return;
            _catalogueRequested = false; // Reopening retries; no invented backend participants.
        }
        if (this == null || version != _catalogueVersion) return;
        CatalogueLoading = false; RefreshView();
    }

    private void SetPlaybackOnStreams()
    {
        _mainStream?.SetPlayback(_wantsPlayback);
        foreach (var stream in _driverStreams.Values) stream.SetPlayback(_wantsPlayback);
    }
    private void SourcesChanged()
    {
        if (this == null) return;
        if (CurrentStream != null && CurrentStream.Finished) { _wantsPlayback = false; SetPlaybackOnStreams(); }
        ApplyVolume(); UpdateControls();
    }
    private void DisposeDriverStreams()
    {
        foreach (var stream in _driverStreams.Values) stream.Dispose();
        _driverStreams.Clear();
    }

    private void RefreshView()
    {
        // The independent TV can be destroyed before its course controller on scene shutdown.
        if (videoPanel == null) return;
        var view = videoPanel.GetComponent<ODVideoPlayerView>();
        if (view != null) view.Refresh();
    }

    private void UpdateControls()
    {
        if (playPauseLabel != null) playPauseLabel.text = WantsPlayback ? "Pause" : "Play";
        if (playbackStatus != null) playbackStatus.text = StatusForFeed(View != null ? View.PrimaryFeed : -1);
        RefreshView();
        if (tvButtons == null) return;
        foreach (var button in tvButtons)
        {
            if (button == null) continue;
            var colors = button.colors;
            // Neutral frosted white (ODChampionshipTheme): readable on every championship gradient.
            colors.normalColor = colors.selectedColor = IsVisible
                ? new Color(1f, 1f, 1f, .38f) : new Color(1f, 1f, 1f, .14f);
            button.colors = colors;
        }
    }

    private void OnDisable()
    {
        HideVideo();
        _mainStream?.Reset();
        DisposeDriverStreams();
        _context = null;
        _sessionId = null;
        ++_catalogueVersion; CatalogueLoading = false; _catalogueRequested = false;
    }

    private void OnDestroy()
    {
        if (!_bound) return;
        if (tvButtons != null)
            foreach (var button in tvButtons) if (button != null) button.onClick.RemoveListener(ToggleVideo);
        if (playPauseButton != null) playPauseButton.onClick.RemoveListener(TogglePlayPause);
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(SetVolume);
        if (scrubSlider != null) scrubSlider.onValueChanged.RemoveListener(SeekNormalized);
        _mainStream?.Dispose();
        DisposeDriverStreams();
    }
}
