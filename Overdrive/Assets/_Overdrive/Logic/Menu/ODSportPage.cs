using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Four presentation states. Live states come from the backend (state decided
/// by ODChampionshipDataService); when it can't be reached, a popup offers to
/// retry or to switch to the mobile mocks, where the demo button cycles the
/// three championship states. The replay state is the broadcast page of a
/// replay picked in the gallery (header "Replays" button, in both modes); its
/// back button returns to the gallery.
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
    public TextMeshProUGUI demoLabel;
    public RoundedImage statusSurface;
    public ODHomepageSurface background;
    public Button demoButton;
    public Button backButton;
    public Button replayButton;
    public VideoPlayerController television;
    public CircuitPreview circuitPreview;
    public Button[] circuitButtons;
    public ODChampionshipPanel championshipPanel;
    public string mockGrandPrix = "GP de Singapour";
    public string mockSession = "Course";
    [Header("Selected replay (catalogue demo)")]
    public string replayId;
    public string replayGrandPrix = "GP de Singapour";
    public string replaySession = "Course";
    public string replaySeason = "2024";
    private bool _bound;
    private ChampionshipSnapshot _snapshot;
    private int _loadVersion;
    private int _replayVersion;
    private Mode _modeBeforeReplay = Mode.Weekend;

    private void OnEnable()
    {
        ODPassthroughBlur.Acquire(this);   // frosted room behind the glass (Quest 3, camera permission)
        Initialize();
        SetMode(currentMode);
        if (!ODDataSource.UseMocks) LoadFromBackend();
    }

    public void Initialize()
    {
        if (_bound) return;
        _bound = true;
        if (championshipPanel == null && !TryGetComponent(out championshipPanel))
            championshipPanel = gameObject.AddComponent<ODChampionshipPanel>();
        demoButton?.onClick.AddListener(NextMode);
        backButton?.onClick.AddListener(Back);
        replayButton?.onClick.AddListener(OpenReplayCatalog);
        if (circuitButtons != null)
            foreach (var button in circuitButtons) button?.onClick.AddListener(ToggleCircuits);
        television?.Initialize();
    }

    /// <summary>Demo data only: hors week-end → week-end → live. The replay state is reached from the gallery.</summary>
    public void NextMode() => SetMode((Mode)(((int)currentMode + 1) % 3));

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
        _replayVersion++;   // drops an in-flight replay load
        SetMode(!ODDataSource.UseMocks && _snapshot != null ? ModeFor(_snapshot.state) : _modeBeforeReplay);
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
    }

    public void SetMode(Mode mode)
    {
        if (currentMode != mode) circuitPreview?.Hide();
        currentMode = (Mode)Mathf.Clamp((int)mode, 0, 3);
        int index = (int)currentMode;
        if (modePanels != null)
            for (int i = 0; i < modePanels.Length; i++)
                if (modePanels[i] != null) modePanels[i].SetActive(i == index);
        bool replay = currentMode == Mode.Replay;
        string[] modes = { "Hors week-end", "Week-end en cours", "Live - " + mockSession, "Replay" };
        if (heading != null) heading.text = replay ? replayGrandPrix : index == 0 ? "F1" : index == 1 ? mockGrandPrix : mockSession;
        if (subtitle != null) subtitle.text = replay
            ? "Formule 1 · " + replaySession + " · Saison " + replaySeason
            : "Formule 1 · " + mockGrandPrix;
        if (status != null) status.text = modes[index];
        if (demoLabel != null) demoLabel.text = "Mode démo · " + (index == 0 ? "Hors week-end" : index == 1 ? "Week-end" : "Live");
        // Background and status pill colours: ODChampionshipPanel / ODChampionshipTheme
        // (championship palette + state style), applied by ShowModeContent below.
        if (television != null) television.Configure(currentMode,
            replay ? "replay:" + replayId + ":" + replayGrandPrix : "live:" + index + ":" + mockGrandPrix + ":" + mockSession,
            replay ? replayGrandPrix : mockGrandPrix);
        // States are cycled by hand only on demo data; on the backend the data decides.
        // The replay page has neither: its back arrow returns to the gallery.
        if (demoButton != null) demoButton.gameObject.SetActive(ODDataSource.UseMocks && !replay);
        if (replayButton != null) replayButton.gameObject.SetActive(!replay);
        ShowModeContent();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Data — backend or mobile mocks
    // ═══════════════════════════════════════════════════════════════════════

    private void ShowModeContent()
    {
        if (championshipPanel == null) return;

        if (currentMode == Mode.Replay)
        {
            ShowReplayContent();
            return;
        }

        if (ODDataSource.UseMocks)
        {
            switch (currentMode)
            {
                case Mode.OffWeekend: championshipPanel.ShowOffSeason(ODOffSeasonMockData.Wec); break;
                case Mode.Weekend: championshipPanel.ShowWeekend(ODWeekendMockData.MotoGp); break;
                case Mode.LiveSession: ShowLiveData(ODLiveSessionMockData.Formula1); break;
            }
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
        try
        {
            var snapshot = await ODChampionshipDataService.LoadAsync();
            if (this == null || version != _loadVersion || ODDataSource.UseMocks) return;

            if (!snapshot.hasData)
            {
                ShowBackendError("Aucune donnée",
                    "Le backend répond mais ne contient aucun événement pour ce championnat. Lance une ingestion OpenF1, ou affiche les données de démo.");
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
            ShowBackendError(ODChampionshipDataService.Title(error), ODChampionshipDataService.Describe(error));
        }
    }

    /// <summary>No WebSocket on the backend yet: the live state is re-fetched every few seconds.</summary>
    private async void PollLive(int version)
    {
        int failures = 0;
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(1f, ODApiConfig.Instance.livePollSeconds)));
            if (this == null || version != _loadVersion || ODDataSource.UseMocks || _snapshot == null) return;

            try
            {
                var live = await ODChampionshipDataService.LoadLiveAsync(_snapshot.liveEvent, _snapshot.liveSession);
                if (this == null || version != _loadVersion || ODDataSource.UseMocks) return;
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
        var summary = new ReplaySummary
        {
            id = replayId,
            championshipId = ODDataSource.UseMocks ? "formula_1" : ODApiConfig.Instance.championshipCode,
            championshipName = "Formule 1",
            grandPrix = replayGrandPrix,
            sessionName = replaySession,
            season = replaySeason,
        };

        if (ODDataSource.UseMocks)
        {
            ShowReplayData(ODReplayMockData.Detail(replayId) ?? new ReplayDetail { summary = summary });
            return;
        }

        championshipPanel.ShowReplay(new ReplayDetail { summary = summary });   // header now, podium when loaded
        LoadReplay(summary);
    }

    private async void LoadReplay(ReplaySummary summary)
    {
        int version = ++_replayVersion;
        try
        {
            var detail = await ODChampionshipDataService.LoadReplayDetailAsync(summary);
            if (this == null || version != _replayVersion || currentMode != Mode.Replay || ODDataSource.UseMocks) return;
            ShowReplayData(detail);
        }
        catch (Exception error)
        {
            if (this == null || version != _replayVersion) return;
            Debug.LogWarning("[ODSportPage] Replay load failed: " + error);
            ShowBackendError(ODChampionshipDataService.Title(error), ODChampionshipDataService.Describe(error));
        }
    }

    /// <summary>Panel (header, podium, circuit) + the running order table on the right.</summary>
    private void ShowLiveData(LiveSessionData data)
    {
        championshipPanel.ShowLive(data);
        string lap = data == null || data.currentLap <= 0 ? ""
                   : data.totalLaps > 0 ? $"Tour {data.currentLap}/{data.totalLaps}" : $"Tour {data.currentLap}";
        Ranking?.ShowEntries(data?.entries, lap, ODDataSource.UseMocks ? "Données de démonstration (OD_Mobile)" : "Données en direct du backend");
    }

    private void ShowReplayData(ReplayDetail detail)
    {
        championshipPanel.ShowReplay(detail);
        if (detail != null && detail.results.Count > 0)
            Ranking?.ShowEntries(detail.results, detail.totalLaps > 0 ? $"{detail.totalLaps} tours" : "",
                ODDataSource.UseMocks ? "Données de démonstration (OD_Mobile)" : "Résultats du backend", animate: false);
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
        ODBackendErrorPopup.Show((RectTransform)transform, title, message,
            onRetry: () =>
            {
                if (currentMode == Mode.Replay) ShowReplayContent();
                else LoadFromBackend();
            },
            onUseMocks: () =>
            {
                ODDataSource.UseMocks = true;
                _loadVersion++;
                SetMode(currentMode);
            });
    }

    private void OnDestroy()
    {
        if (!_bound) return;
        demoButton?.onClick.RemoveListener(NextMode);
        backButton?.onClick.RemoveListener(Back);
        replayButton?.onClick.RemoveListener(OpenReplayCatalog);
        if (circuitButtons != null)
            foreach (var button in circuitButtons) button?.onClick.RemoveListener(ToggleCircuits);
    }
}
