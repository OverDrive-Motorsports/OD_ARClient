/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODHomePage - Home page of the main menu (nav tab "Accueil"). Loads the
 ## Home data from the backend each time the page opens, hands it to
 ## ODHomeView and routes its buttons to the other menu pages. While a
 ## session is live, its running order is re-fetched every few seconds. On a
 ## backend failure it shows the usual error popup (retry / demo data).
 ##
 */

using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed class ODHomePage : MonoBehaviour
{
    public OverdriveMainMenu menu;
    [Tooltip("Home shown with the demo data (picked from the backend error popup).")]
    public HomeState demoState = HomeState.NoRace;

    const string ReminderPrefix = "Overdrive.HomeReminder.";

    ODHomeView _view;
    readonly HomeData _data = new HomeData();
    bool _loaded;
    int _loadVersion;

    /// <summary>Creates the page in place of <paramref name="placeholder"/> (the scene's homepage slot), same size and placement.</summary>
    public static ODHomePage Create(RectTransform placeholder, OverdriveMainMenu menu)
    {
        var go = new GameObject("HomePage", typeof(RectTransform));
        go.SetActive(false);
        var rect = (RectTransform)go.transform;
        rect.SetParent(placeholder.parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetSiblingIndex(placeholder.GetSiblingIndex() + 1);

        var page = go.AddComponent<ODHomePage>();
        page.menu = menu;
        page._view = go.AddComponent<ODHomeView>();
        page.Bind();
        return page;
    }

    void Bind()
    {
        _view.SearchClicked += OpenCatalogue;
        _view.CatalogueClicked += OpenCatalogue;
        _view.CalendarClicked += () => menu?.ShowCalendar();
        _view.AllReplaysClicked += () => menu?.ShowReplayCatalog();
        _view.ReplayClicked += OpenReplay;
        _view.RemindClicked += ToggleReminder;
        // The championship page follows the configured championship and shows its live / weekend state.
        _view.WatchLiveClicked += OpenSport;
        _view.TelemetryClicked += OpenSport;
        _view.WeekendClicked += OpenSport;
        _view.AgendaClicked += () => menu?.ShowCalendar();
        // Notifications have no backend yet: the bell is shown as in the design but opens nothing.
    }

    void OnEnable()
    {
        ODPassthroughBlur.Acquire(this);   // frosted room behind the glass (Quest 3, camera permission)
        if (_view == null) return;
        _view.Build();
        if (ODDataSource.UseMocks)
        {
            ShowDemo();
            return;
        }
        if (_loaded) Repaint();   // last known data while the refresh runs
        else _view.ShowLoading();
        Load();
    }

    void OnDisable()
    {
        ODPassthroughBlur.Release(this);
        _loadVersion++;   // drops an in-flight load
    }

    async void Load()
    {
        int version = ++_loadVersion;
        try
        {
            await ODHomeDataService.LoadAsync(_data);
            if (this == null || version != _loadVersion || ODDataSource.UseMocks) return;
            _loaded = true;
            ODBackendErrorPopup.Hide((RectTransform)transform);
            Repaint();
            if (_data.state == HomeState.Live) PollLive(version);
        }
        catch (Exception error)
        {
            if (this == null || version != _loadVersion) return;
            Debug.LogWarning("[ODHomePage] Backend load failed: " + error);
            ODBackendErrorPopup.Show((RectTransform)transform, ODChampionshipDataService.Title(error), ODChampionshipDataService.Describe(error),
                Load, UseDemo);
        }
    }

    /// <summary>No WebSocket on the backend yet: the running order is re-fetched every few seconds.</summary>
    async void PollLive(int version)
    {
        int failures = 0;
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(1f, ODApiConfig.Instance.livePollSeconds)));
            if (this == null || version != _loadVersion || ODDataSource.UseMocks || _data.state != HomeState.Live) return;
            try
            {
                await ODHomeDataService.RefreshLiveAsync(_data);
                if (this == null || version != _loadVersion || ODDataSource.UseMocks) return;
                failures = 0;
                _view.ShowLive(_data.live);
            }
            catch (Exception error)
            {
                if (this == null || version != _loadVersion) return;
                Debug.LogWarning("[ODHomePage] Live refresh failed: " + error.Message);
                if (++failures >= 3) { Load(); return; }   // the session may be over: decide the Home again
            }
        }
    }

    void UseDemo()
    {
        ODDataSource.UseMocks = true;
        ShowDemo();
    }

    /// <summary>Demo data only: the Home for <see cref="demoState"/>.</summary>
    public void ShowDemo()
    {
        ODHomeDataService.LoadDemo(_data, demoState);
        Repaint();
    }

    void Repaint() => _view.Show(_data, IsReminderOn());

    // ── Actions ─────────────────────────────────────────────────────────────

    void OpenCatalogue() => menu?.ShowCategories();

    void OpenSport() => menu?.ShowSport();

    void OpenReplay(int index)
    {
        if (menu == null || index < 0 || index >= _data.replays.Count) return;
        var sport = menu.sportPanel != null ? menu.sportPanel.GetComponent<ODSportPage>() : null;
        if (sport == null) return;
        var summary = _data.replays[index].summary;
        sport.SelectReplay(new ODReplayCatalog.Entry
        {
            id = summary.id,
            grandPrix = summary.grandPrix,
            session = summary.sessionName,
            season = summary.season,
        });
        menu.ShowSport();
    }

    /// <summary>
    /// Remembered per event on the headset; there is no notification service to schedule it with yet.
    /// During a weekend the key also holds the next session, so each session has its own reminder.
    /// </summary>
    void ToggleReminder()
    {
        string key = ReminderKey();
        if (key == null) return;
        PlayerPrefs.SetInt(key, IsReminderOn() ? 0 : 1);
        PlayerPrefs.Save();
        Repaint();
    }

    bool IsReminderOn()
    {
        string key = ReminderKey();
        return key != null && PlayerPrefs.GetInt(key, 0) == 1;
    }

    string ReminderKey()
    {
        var next = _data.nextEvent;
        if (next == null) return null;
        string key = ReminderPrefix + next.championshipId + "." + next.eventName + "." + next.startsAt.ToString("yyyyMMdd");
        if (_data.state == HomeState.Weekend)
        {
            var session = next.sessions.Find(s => !s.finished);
            if (session.name != null) key += "." + session.name;
        }
        return key;
    }
}
