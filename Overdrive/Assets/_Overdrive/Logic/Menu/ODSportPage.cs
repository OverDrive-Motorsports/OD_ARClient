using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Four presentation states. The three championship states come from the backend
/// (state decided by ODChampionshipDataService); when it can't be reached, a popup
/// offers to retry. The replay state is the broadcast page of a replay picked in
/// the gallery (header "Replays" button); its back button returns to the gallery. A backend replay is downloaded if needed and
/// read when the page opens; it starts from lights out when "Regarder le replay" is
/// pressed and stops when the page is left.
/// </summary>
public sealed class ODSportPage : MonoBehaviour
{
    public enum Mode { OffWeekend = 0, Weekend = 1, LiveSession = 2, Replay = 3 }
    public OverdriveMainMenu menu;
    public Mode currentMode = Mode.Weekend;
    public GameObject[] modePanels;
    public TextMeshProUGUI heading;
    public TextMeshProUGUI subtitle;
    public TextMeshProUGUI status;
    public RoundedImage statusSurface;
    public ODHomepageSurface background;
    public Button backButton;
    public Button replayButton;
    public VideoPlayerController television;
    public CircuitPreview circuitPreview;
    public Button[] circuitButtons;
    public ODChampionshipPanel championshipPanel;
    [Header("Selected replay (set by the gallery)")]
    public string replayId;
    public string replayGrandPrix;
    public string replaySession;
    public string replaySeason;
    private bool _bound;
    private ChampionshipSnapshot _snapshot;
    private int _loadVersion;
    private int _replayVersion;
    private Mode _modeBeforeReplay = Mode.Weekend;
    private ODReplayPlayer _player;
    private bool _replayOrderShown;
    private bool _watchRequested;   // "Regarder le replay" pressed before the replay was ready
    private Exception _pendingBackendError;

    private void OnEnable()
    {
        ODPassthroughBlur.Acquire(this);   // frosted room behind the glass (Quest 3, camera permission)
        Initialize();
        SetMode(currentMode);
        LoadFromBackend();
    }

    public void Initialize()
    {
        if (_bound) return;
        _bound = true;
        if (championshipPanel == null && !TryGetComponent(out championshipPanel))
            championshipPanel = gameObject.AddComponent<ODChampionshipPanel>();
        backButton?.onClick.AddListener(Back);
        replayButton?.onClick.AddListener(OpenReplayCatalog);
        if (circuitButtons != null)
            foreach (var button in circuitButtons) button?.onClick.AddListener(ToggleCircuits);
        television?.Initialize();
        if (!TryGetComponent(out _player)) _player = gameObject.AddComponent<ODReplayPlayer>();
        _player.OrderChanged += ShowReplayOrder;
        _player.ProgressChanged += championshipPanel.SetReplayProgress;
        championshipPanel.ReplayWatchPressed += WatchReplay;
    }

    public void OpenReplayCatalog()
    {
        if (menu != null) menu.ShowReplayCatalog();
    }

    public void SelectReplay(ODReplayCatalog.Entry entry)
    {
        if (entry == null) return;
        replayId = entry.id;
        replayGrandPrix = entry.grandPrix;
        replaySession = entry.session;
        replaySeason = entry.season;
        if (currentMode != Mode.Replay) _modeBeforeReplay = currentMode;
        SetMode(Mode.Replay);
    }

    /// <summary>Back to the championship state the replay was opened from (or the one the backend decides).</summary>
    public void LeaveReplay()
    {
        if (currentMode != Mode.Replay) return;
        StopReplay();
        SetMode(_snapshot != null ? ModeFor(_snapshot.state) : _modeBeforeReplay);

        var error = _pendingBackendError;
        _pendingBackendError = null;
        if (error != null)
            ShowBackendError(ODChampionshipDataService.Title(error), ODChampionshipDataService.Describe(error));
    }

    /// <summary>Header back arrow: the replay page returns to the gallery, the other states to the championships.</summary>
    public void Back()
    {
        if (currentMode == Mode.Replay) OpenReplayCatalog();
        else BackToCategories();
    }

    public void BackToCategories()
    {
        if (menu != null) menu.ShowCategories();
    }

    public void ToggleCircuits()
    {
        if (!isActiveAndEnabled || circuitPreview == null) return;
        if (circuitPreview.gameObject.activeSelf) circuitPreview.Hide();
        else circuitPreview.ShowBeside(menu != null ? (RectTransform)menu.transform : (RectTransform)transform, true);
    }

    private void OnDisable()
    {
        ODPassthroughBlur.Release(this);
        circuitPreview?.Hide();
        _loadVersion++;   // drops in-flight loads and stops live polling; OnEnable reloads
        StopReplay();     // OnEnable starts the replay again from lights out
    }

    public void SetMode(Mode mode)
    {
        if (currentMode != mode) circuitPreview?.Hide();
        if (mode != Mode.Replay) StopReplay();
        currentMode = (Mode)Mathf.Clamp((int)mode, 0, 3);
        int index = (int)currentMode;
        if (modePanels != null)
            for (int i = 0; i < modePanels.Length; i++)
                if (modePanels[i] != null) modePanels[i].SetActive(i == index);
        bool replay = currentMode == Mode.Replay;
        string[] modes = { "Hors week-end", "Week-end en cours", "Live", "Replay" };
        if (heading != null) heading.text = replay ? replayGrandPrix : "Formule 1";
        if (subtitle != null) subtitle.text = replay
            ? "Formule 1 · " + replaySession + " · Saison " + replaySeason
            : "";
        if (status != null) status.text = modes[index];
        // Background and status pill colours: ODChampionshipPanel / ODChampionshipTheme
        // (championship palette + state style), applied by ShowModeContent below.
        if (television != null) television.Configure(currentMode,
            replay ? "replay:" + replayId : "live:" + index,
            replay ? replayGrandPrix : "Formule 1");
        // The replay page has no "Replays" button: its back arrow returns to the gallery.
        if (replayButton != null) replayButton.gameObject.SetActive(!replay);
        ShowModeContent();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Data — backend
    // ═══════════════════════════════════════════════════════════════════════

    private void ShowModeContent()
    {
        if (championshipPanel == null) return;

        if (currentMode == Mode.Replay)
        {
            ShowReplayContent();
            return;
        }

        if (_snapshot == null || ModeFor(_snapshot.state) != currentMode)
        {
            championshipPanel.ShowLoading("Formule 1");
            return;
        }
        switch (_snapshot.state)
        {
            case ChampionshipViewState.OffSeason: championshipPanel.ShowOffSeason(_snapshot.offSeason); break;
            case ChampionshipViewState.Weekend: championshipPanel.ShowWeekend(_snapshot.weekend); break;
            case ChampionshipViewState.Live: ShowLiveData(_snapshot.live); break;
        }
    }

    private static Mode ModeFor(ChampionshipViewState state) =>
        state == ChampionshipViewState.Live ? Mode.LiveSession :
        state == ChampionshipViewState.Weekend ? Mode.Weekend : Mode.OffWeekend;

    /// <summary>Fetches the championship state; the panel switches to it unless a replay is open.</summary>
    private async void LoadFromBackend()
    {
        int version = ++_loadVersion;
        _pendingBackendError = null;
        try
        {
            var snapshot = await ODChampionshipDataService.LoadAsync();
            if (this == null || version != _loadVersion) return;

            if (!snapshot.hasData)
            {
                ShowBackendError("Aucune donnée",
                    "Le backend répond mais ne contient aucun événement pour ce championnat. Lance une ingestion OpenF1 puis réessaie.");
                return;
            }

            _snapshot = snapshot;
            if (currentMode == Mode.Replay) return;
            SetMode(ModeFor(snapshot.state));
            if (snapshot.state == ChampionshipViewState.Live) PollLive(version);
        }
        catch (Exception error)
        {
            if (this == null || version != _loadVersion) return;
            Debug.LogWarning("[ODSportPage] Backend load failed: " + error);
            // A downloaded replay plays without the backend: the error waits until the replay is left.
            if (currentMode == Mode.Replay) _pendingBackendError = error;
            else ShowBackendError(ODChampionshipDataService.Title(error), ODChampionshipDataService.Describe(error));
        }
    }

    /// <summary>No WebSocket on the backend yet: the live state is re-fetched every few seconds.</summary>
    private async void PollLive(int version)
    {
        int failures = 0;
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(1f, ODApiConfig.Instance.livePollSeconds)));
            if (this == null || version != _loadVersion || _snapshot == null) return;

            try
            {
                var live = await ODChampionshipDataService.LoadLiveAsync(_snapshot.liveEvent, _snapshot.liveSession);
                if (this == null || version != _loadVersion) return;
                _snapshot.live = live;
                failures = 0;
                if (currentMode == Mode.LiveSession) ShowLiveData(live);
            }
            catch (Exception error)
            {
                if (this == null || version != _loadVersion) return;
                Debug.LogWarning("[ODSportPage] Live refresh failed: " + error.Message);
                if (++failures < 3) continue;
                ShowBackendError("Connexion perdue", ODChampionshipDataService.Describe(error));
                return;
            }
        }
    }

    private void ShowReplayContent()
    {
        StopReplay();
        var summary = new ReplaySummary
        {
            id = replayId,
            championshipId = ODApiConfig.Instance.championshipCode,
            championshipName = "Formule 1",
            grandPrix = replayGrandPrix,
            sessionName = replaySession,
            season = replaySeason,
        };

        championshipPanel.ShowReplay(new ReplayDetail { summary = summary });   // header now, podium when loaded
        LoadReplay(summary);
    }

    /// <summary>Podium, then the replay itself: downloaded when it isn't on the headset, read, played from lights out.</summary>
    private async void LoadReplay(ReplaySummary summary)
    {
        int version = ++_replayVersion;
        try
        {
            var detail = await ODReplayService.LoadDetailAsync(summary);
            if (!IsCurrentReplay(version)) return;
            ShowReplayData(detail);

            if (!ODReplayService.IsDownloaded(summary.id))
            {
                championshipPanel.SetReplayProgress("Téléchargement du replay…");
                var received = new Progress<ulong>(bytes =>
                {
                    if (IsCurrentReplay(version)) championshipPanel.SetReplayProgress($"Téléchargement du replay · {bytes / (1024 * 1024)} Mo");
                });
                await ODReplayService.DownloadAsync(summary, received);
                if (!IsCurrentReplay(version)) return;
            }

            championshipPanel.SetReplayProgress("Préparation du replay…");
            var race = await ODReplayService.LoadRaceAsync(summary);
            if (!IsCurrentReplay(version)) return;
            _player.Load(race, summary, ODReplayTrackMap.For(championshipPanel.ReplayScreen));
            if (_watchRequested) BeginReplay();
            else championshipPanel.SetReplayProgress("Prêt · appuie sur « Regarder le replay »");
        }
        catch (Exception error)
        {
            if (this == null || version != _replayVersion) return;
            Debug.LogWarning("[ODSportPage] Replay load failed: " + error);
            ShowBackendError(ODChampionshipDataService.Title(error), ODChampionshipDataService.Describe(error));
        }
    }

    private bool IsCurrentReplay(int version) =>
        this != null && version == _replayVersion && currentMode == Mode.Replay;

    /// <summary>
    /// "Regarder le replay": starts the race from lights out, or as soon as it is ready
    /// when it is still downloading / being read.
    /// </summary>
    private void WatchReplay()
    {
        if (currentMode != Mode.Replay || _player == null || _player.IsPlaying) return;
        if (_player.IsLoaded) BeginReplay();
        else _watchRequested = true;
    }

    private void BeginReplay()
    {
        _watchRequested = false;
        _replayOrderShown = false;
        championshipPanel.BeginReplayPlayback();
        _player.Begin();
    }

    /// <summary>Drops an in-flight replay load and stops playback.</summary>
    private void StopReplay()
    {
        _replayVersion++;
        _watchRequested = false;
        if (_player != null) _player.Stop();
    }

    /// <summary>Playback: the running order at the replay time, in the table and on the podium.</summary>
    private void ShowReplayOrder(LiveSessionData frame)
    {
        championshipPanel.ShowReplayFrame(frame);
        string lap = frame.totalLaps > 0 ? $"Tour {frame.currentLap}/{frame.totalLaps}" : $"Tour {frame.currentLap}";
        Ranking?.ShowEntries(frame.entries, lap, "Replay · données téléchargées", animate: _replayOrderShown);
        _replayOrderShown = true;   // the first order is placed at once, the next ones slide like overtakes
    }

    /// <summary>Panel (header, podium, circuit) + the running order table on the right.</summary>
    private void ShowLiveData(LiveSessionData data)
    {
        championshipPanel.ShowLive(data);
        string lap = data == null || data.currentLap <= 0 ? ""
                   : data.totalLaps > 0 ? $"Tour {data.currentLap}/{data.totalLaps}" : $"Tour {data.currentLap}";
        Ranking?.ShowEntries(data?.entries, lap, "Données en direct du backend");
    }

    private void ShowReplayData(ReplayDetail detail)
    {
        championshipPanel.ShowReplay(detail);
        if (detail != null && detail.results.Count > 0)
            Ranking?.ShowEntries(detail.results, detail.totalLaps > 0 ? $"{detail.totalLaps} tours" : "",
                "Résultats du backend", animate: false);
    }

    private RaceRankingManager _ranking;
    private RaceRankingManager Ranking
    {
        get
        {
            if (_ranking == null) _ranking = GetComponentInChildren<RaceRankingManager>(true);
            return _ranking != null ? _ranking : null;
        }
    }

    private void ShowBackendError(string title, string message)
    {
        ODBackendErrorPopup.Show((RectTransform)transform, title, message, onRetry: () =>
        {
            if (currentMode == Mode.Replay) ShowReplayContent();
            else LoadFromBackend();
        });
    }

    private void OnDestroy()
    {
        if (!_bound) return;
        backButton?.onClick.RemoveListener(Back);
        replayButton?.onClick.RemoveListener(OpenReplayCatalog);
        if (circuitButtons != null)
            foreach (var button in circuitButtons) button?.onClick.RemoveListener(ToggleCircuits);
        if (_player != null)
        {
            _player.OrderChanged -= ShowReplayOrder;
            _player.ProgressChanged -= championshipPanel.SetReplayProgress;
        }
        if (championshipPanel != null) championshipPanel.ReplayWatchPressed -= WatchReplay;
    }
}
