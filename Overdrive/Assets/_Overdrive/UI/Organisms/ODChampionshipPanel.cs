/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODChampionshipPanel - Display side of the single championship panel
 ## (visionOS look: see ODChampionshipTheme)
 ## (OverdriveF1Session prefab). One panel, three live states (off-season,
 ## event weekend, live session) plus the replay view. Each has its own Show
 ## method that fills the shared header and that state's section from data.
 ## Lists are built at runtime: one row per entry, one standings table per
 ## category.
 ##
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Lives on the root of the championship panel. Every reference is resolved
/// by child name when left empty, so the existing prefab works without being
/// rebuilt. Which state section is visible stays owned by ODSportPage
/// (modePanels) — this component only fills content. Display only: buttons
/// raise events, Logic decides what they do.
///
/// The live running order (CourseStandings) is deliberately not touched: it is
/// driven by RaceRankingManager until the panel is connected to live timing.
/// </summary>
public partial class ODChampionshipPanel : MonoBehaviour
{
    [Header("Shared header — resolved by child name when left empty")]
    public TextMeshProUGUI heading;         // "Heading"
    public TextMeshProUGUI subtitle;        // "Subtitle"
    public TextMeshProUGUI status;          // "StatusBadge/Label"
    public Graphic         statusPill;      // "StatusBadge"
    public ODHomepageSurface background;    // this object's surface

    [Header("State sections — resolved by child name when left empty")]
    public RectTransform offSeasonSection;  // "OffWeekend"
    public RectTransform weekendSection;    // "Weekend"
    public RectTransform liveSection;       // "LiveSession"
    public RectTransform replaySection;     // "Replay"

    [Header("Events — listened to by Logic")]
    public UnityEvent onViewEvent     = new UnityEvent();
    public UnityEvent onFullStandings = new UnityEvent();
    public UnityEvent onFollowLive    = new UnityEvent();

    enum State { None, OffSeason, Weekend, Live, Replay }

    static readonly string[] FrenchDays   = { "Dim", "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam" };
    static readonly string[] FrenchMonths = { "JAN", "FÉV", "MAR", "AVR", "MAI", "JUIN", "JUIL", "AOÛT", "SEPT", "OCT", "NOV", "DÉC" };

    /// <summary>A standings card: its own heading + a scrollable list of tables.</summary>
    sealed class StandingsView
    {
        public RectTransform card;
        public RectTransform content;
        public float         padPx;
        public bool          wide;
        public bool          Narrow => !wide;
        /// <summary>Pilotes / Équipes choice of each category, kept across data refreshes.</summary>
        public readonly Dictionary<string, StandingType> types = new Dictionary<string, StandingType>();
        /// <summary>Controls placed on the card's title line (category button, switch), rebuilt with the list.</summary>
        public readonly List<GameObject> headerControls = new List<GameObject>();
        /// <summary>Categories with entries, and the one shown (one table at a time).</summary>
        public List<SeasonStandingCategory> categories = new List<SeasonStandingCategory>();
        public int categoryIndex;
        public StandingType preferred;
    }

    bool          _resolved;
    bool          _glassApplied;
    State         _state;
    float         _nextRefresh;

    // Off-season
    bool            _offSeasonWired;
    OffSeasonData   _offSeason;
    RectTransform   _nextEventCard;
    RectTransform   _calendarRows;
    StandingsView   _offSeasonStandings;

    // Weekend
    bool            _weekendWired;
    WeekendData     _weekend;
    RectTransform   _circuitWeatherCard;
    RectTransform   _nextSessionCard;
    RectTransform   _programContent;
    StandingsView   _weekendStandings;

    // ═══════════════════════════════════════════════════════════════════════
    // Public API — one Show method per state
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Header placeholder while Logic waits for the backend.</summary>
    public void ShowLoading(string title)
    {
        Resolve();
        _state = State.None;
        SetLabel(heading, title);
        SetLabel(subtitle, "Connexion au backend…");
        ApplyTheme(null, ODStatusStyle.Loading, "Chargement");
        AfterStatus(null);
    }

    /// <summary>Off-season ("HORS DIRECT"): next event countdown, calendar, season standings.</summary>
    public void ShowOffSeason(OffSeasonData data)
    {
        Resolve();
        WireOffSeason();
        _state     = State.OffSeason;
        _offSeason = data;

        SetLabel(heading, data != null ? data.headline : "");
        ApplyTheme(data?.championshipId, ODStatusStyle.OffSeason, "Hors direct");
        AfterStatus(null);

        OffSeasonEvent? next = data != null ? data.nextEvent : null;
        SetText(_nextEventCard, "MockEvent", next.HasValue ? next.Value.name : "Inter-saison");
        SetText(_nextEventCard, "Sport",     next.HasValue ? next.Value.location : "Aucun événement programmé");
        _nextEventCard?.Find("ViewEvent")?.gameObject.SetActive(next.HasValue);

        RefreshOffSeasonCountdown();
        BindCalendar(data != null ? data.calendar : null);
        RenderStandings(_offSeasonStandings, data != null ? data.standings : null, StandingType.Drivers);
    }

    /// <summary>Event weekend ("LIVE · WEEK-END"): circuit/weather, next session, programme, standings.</summary>
    public void ShowWeekend(WeekendData data)
    {
        Resolve();
        WireWeekend();
        _state   = State.Weekend;
        _weekend = data;

        WeekendCircuit? circuit = data != null ? data.circuit : null;
        SetLabel(heading, data != null ? data.eventName : "");
        SetLabel(subtitle, circuit.HasValue ? JoinNonEmpty(" · ", circuit.Value.name, circuit.Value.location) : "");
        ApplyTheme(data?.championshipId, ODStatusStyle.Weekend, "Live · Week-end");
        AfterStatus(null);
        StyleNextSession(_nextSessionCard);

        BindCircuitWeather(circuit, data != null ? data.weather : null);
        RefreshWeekendSchedule();

        var standings = data != null ? data.standings : null;
        RenderStandings(_weekendStandings, standings, StandingType.Drivers);
    }

    /// <summary>Live session ("LIVE · COURSE"): lap progress, podium, track status.</summary>
    public void ShowLive(LiveSessionData data)
    {
        Resolve();
        _state = State.Live;

        string session = data != null && !string.IsNullOrEmpty(data.sessionName) ? data.sessionName : "Session";
        string lap     = data == null || data.currentLap <= 0 ? ""
                       : data.totalLaps > 0 ? $"Tour {data.currentLap} / {data.totalLaps}"
                                            : $"Tour {data.currentLap}";

        LayoutLive(liveSection);
        LayoutCourseStandings();
        SetCourseColumns(replay: false);
        SetLabel(heading, data != null && !string.IsNullOrEmpty(data.circuitName) ? data.circuitName : session);
        SetLabel(subtitle, data != null ? JoinNonEmpty(" · ", ChampionshipName(data.championshipId), session, data.conditions) : session);
        ApplyTheme(data?.championshipId, ODStatusStyle.Live, "Live · " + session);
        AfterStatus(lap);

        var circuit = Child(liveSection, "Circuit");
        BindPodium(Child(liveSection, "Podium"), data != null ? data.entries : null);
        bool known = data != null && data.totalLaps > 0;
        float ratio = known ? Mathf.Clamp01((float)data.currentLap / data.totalLaps) : 0f;
        BindLapProgress(circuit, lap, known ? $"{Mathf.RoundToInt(ratio * 100)}%" : "", ratio);
        BindTrackStatus(circuit, data != null ? data.trackStatus : null);
    }

    /// <summary>Replay broadcast page: video card (watch / telemetry), final podium, circuit line, final classification.</summary>
    public void ShowReplay(ReplayDetail data)
    {
        Resolve();
        _state = State.Replay;
        if (data == null) return;

        ReplaySummary s = data.summary;
        LayoutReplay(replaySection);
        LayoutCourseStandings();
        SetCourseColumns(replay: true);
        SetLabel(heading, s.grandPrix);
        SetLabel(subtitle, JoinNonEmpty(" · ", ChampionshipName(s.championshipId) ?? s.championshipName, s.sessionName, string.IsNullOrEmpty(s.season) ? null : "Saison " + s.season, data.location));
        ApplyTheme(s.championshipId, ODStatusStyle.Replay, "Replay");
        AfterStatus(null);

        var circuit = Child(replaySection, "Circuit");
        BindPodium(Child(replaySection, "Podium"), data.results);
        BindLapProgress(circuit, JoinNonEmpty(" · ", data.location, data.totalLaps > 0 ? $"{data.totalLaps} tours" : null), "", 1f);
        BindTrackStatus(circuit, null);
    }

    /// <summary>Background from the championship, pill from the state, cards as glass.</summary>
    void ApplyTheme(string championshipId, ODStatusStyle style, string statusText)
    {
        ODChampionshipTheme.ApplyBackground(background, ODChampionshipTheme.For(championshipId));
        if (!_glassApplied)
        {
            ODChampionshipTheme.ApplyGlassCards(transform);
            ODChampionshipTheme.ApplyNeutralButtons(transform);
            _glassApplied = true;
        }
        ODChampionshipTheme.ApplyTypography(transform);
        ODChampionshipTheme.ScaleType(transform);   // builder labels, once (runtime labels are created scaled)
        ODChampionshipTheme.ApplyStatus(statusPill, status, statusText, style);
    }

    void Update()
    {
        if ((_state != State.OffSeason && _state != State.Weekend) || ODTime.Now < _nextRefresh) return;
        _nextRefresh = ODTime.Now + 5f;

        if (_state == State.OffSeason) RefreshOffSeasonCountdown();
        else                           RefreshWeekendSchedule();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Reference resolution
    // ═══════════════════════════════════════════════════════════════════════

    void Resolve()
    {
        if (_resolved) return;
        _resolved = true;

        if (heading  == null) heading  = transform.Find("Heading")?.GetComponent<TextMeshProUGUI>();
        if (subtitle == null) subtitle = transform.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
        if (status   == null) status   = transform.Find("StatusBadge/Label")?.GetComponent<TextMeshProUGUI>();
        if (statusPill == null) statusPill = transform.Find("StatusBadge")?.GetComponent<Graphic>();
        if (background == null) background = GetComponent<ODHomepageSurface>();
        if (offSeasonSection == null) offSeasonSection = Child(transform, "OffWeekend");
        if (weekendSection   == null) weekendSection   = Child(transform, "Weekend");
        if (liveSection      == null) liveSection      = Child(transform, "LiveSession");
        if (replaySection    == null) replaySection    = Child(transform, "Replay");

        LayoutHeader();
        // Every state on the design grid from the start (loading / error states show them too).
        WireOffSeason();
        WireWeekend();
        LayoutLive(liveSection);
        LayoutReplay(replaySection);
        LayoutCourseStandings();
    }

    static string ChampionshipName(string id)
    {
        switch (ODDesign.Normalise(id))
        {
            case "f1": return "Formule 1";
            case "wec": return "World Endurance";
            case "motogp": return "MotoGP";
            case "indycar": return "IndyCar";
            case "fe": return "Formula E";
            case "wrc": return "WRC";
            default: return null;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Off-season
    // ═══════════════════════════════════════════════════════════════════════

    void WireOffSeason()
    {
        if (_offSeasonWired || offSeasonSection == null) return;
        _offSeasonWired = true;

        _nextEventCard    = Child(offSeasonSection, "NextEvent");
        var calendarCard  = Child(offSeasonSection, "Calendar");
        var standingsCard = Child(offSeasonSection, "ChampionshipStandings");

        FindButton(_nextEventCard, "ViewEvent")?.onClick.AddListener(onViewEvent.Invoke);
        FindButton(standingsCard, "FullStandings")?.onClick.AddListener(onFullStandings.Invoke);
        // The builder's Pilotes / Équipes buttons give way to one segmented control per category.
        Child(standingsCard, "Drivers")?.gameObject.SetActive(false);
        Child(standingsCard, "Teams")?.gameObject.SetActive(false);

        LayoutOffSeason(_nextEventCard, calendarCard, standingsCard);
        if (calendarCard != null)
            _calendarRows = ODDesign.Rect(calendarCard, "Rows", 44f, 78f, 612f, CalendarCardH - 78f - 32f);

        _offSeasonStandings = BuildStandingsView(standingsCard, standingsCard != null ? standingsCard.rect.height - ODDesign.Px(44f) : 0f, 44f);
    }

    void RefreshOffSeasonCountdown()
    {
        OffSeasonEvent? next = _offSeason != null ? _offSeason.nextEvent : null;
        TimeSpan span = next.HasValue ? next.Value.startsAt - DateTime.Now : TimeSpan.Zero;
        if (span < TimeSpan.Zero) span = TimeSpan.Zero;
        int days = (int)span.TotalDays;

        SetText(_nextEventCard, "CountdownValue0", next.HasValue ? days.ToString("00") : "--");
        SetText(_nextEventCard, "CountdownValue1", next.HasValue ? span.Hours.ToString("00") : "--");
        SetText(_nextEventCard, "CountdownValue2", next.HasValue ? span.Minutes.ToString("00") : "--");

        SetLabel(subtitle, !next.HasValue ? "Inter-saison"
                         : days > 1       ? $"Prochain event dans {days} jours"
                         : days == 1      ? "Prochain event demain"
                                          : "Prochain event aujourd'hui");
    }

    /// <summary>As many design rows (80-px badge, 29-px title) as the card holds, after the next event.</summary>
    void BindCalendar(List<OffSeasonEvent> events)
    {
        if (_calendarRows == null) return;
        Clear(_calendarRows);
        if (events == null) return;

        float rowH = 80f, gap = 20f, width = _calendarRows.rect.width / ODDesign.Scale;
        int fits = Mathf.Max(1, Mathf.FloorToInt((_calendarRows.rect.height / ODDesign.Scale + gap) / (rowH + gap)));
        var next = _offSeason != null ? _offSeason.nextEvent : null;
        int shown = 0;
        foreach (var evt in events)
        {
            if (next.HasValue && evt.name == next.Value.name && evt.startsAt == next.Value.startsAt) continue;
            if (shown >= fits) break;
            float y = shown * (rowH + gap);
            bool hasRound = evt.round > 0;
            var badge = ODDesign.Fill(_calendarRows, "RoundBadge", 0f, y, 80f, 80f, ODDesign.Avatar, 26f);
            ODDesign.Text(badge.transform, "Label", hasRound ? "Manche" : FrenchMonths[evt.startsAt.Month - 1], 0f, 12f, 80f, 20f, 13f,
                ODDesign.Weight.SemiBold, ODDesign.Secondary, TextAlignmentOptions.Center, caps: true, trackingEm: .08f);
            ODDesign.Text(badge.transform, "Number", hasRound ? evt.round.ToString() : evt.startsAt.Day.ToString(), 0f, 30f, 80f, 40f, 30f,
                ODDesign.Weight.Bold, ODDesign.Primary, TextAlignmentOptions.Center);
            ODDesign.Text(_calendarRows, "Name", evt.name, 102f, y + 6f, width - 102f, 38f, 29f, ODDesign.Weight.SemiBold);
            ODDesign.Text(_calendarRows, "Location", JoinNonEmpty(" · ", evt.location, $"{FrenchDays[(int)evt.startsAt.DayOfWeek]} {evt.startsAt:dd/MM}"),
                102f, y + 46f, width - 102f, 30f, 21f, ODDesign.Weight.Regular, ODDesign.Muted);
            shown++;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Weekend
    // ═══════════════════════════════════════════════════════════════════════

    void WireWeekend()
    {
        if (_weekendWired || weekendSection == null) return;
        _weekendWired = true;

        _circuitWeatherCard = Child(weekendSection, "CircuitWeather");
        _nextSessionCard    = Child(weekendSection, "NextSession");
        var programCard     = Child(weekendSection, "Program");
        var standingsCard   = Child(weekendSection, "Standings");

        FindButton(programCard, "FollowLive")?.onClick.AddListener(onFollowLive.Invoke);
        LayoutWeekend(_circuitWeatherCard, _nextSessionCard, programCard, standingsCard);

        // The builder's 5 fixed session slots are replaced by one generated row per session.
        if (programCard != null)
        {
            foreach (Transform child in programCard)
                if (child.name.StartsWith("Session") || child.name.StartsWith("Time") || child.name == "Divider")
                    child.gameObject.SetActive(false);
            _programContent = BuildScrollArea(programCard, ODDesign.Px(96f), programCard.rect.height - ODDesign.Px(36f + 72f + 22f), 36f);   // above "Suivre le direct"
        }

        _weekendStandings = BuildStandingsView(standingsCard, standingsCard != null ? standingsCard.rect.height - ODDesign.Px(30f) : 0f, 36f);
    }

    void BindCircuitWeather(WeekendCircuit? circuit, WeekendWeather? weather)
    {
        bool c = circuit.HasValue, w = weather.HasValue;
        // Order matches the builder's labels: Longueur, Tours, Pluie, Air, Piste, Vent.
        CultureInfo fr = CultureInfo.GetCultureInfo("fr-FR");
        string[] values =
        {
            c && circuit.Value.lengthKm > 0 ? $"{circuit.Value.lengthKm.ToString("0.###", fr)} km" : "--",
            c && circuit.Value.totalLaps > 0 ? circuit.Value.totalLaps.ToString() : "--",
            w && weather.Value.rainChancePercent >= 0 ? $"{weather.Value.rainChancePercent} %" : "--",
            w ? Degrees(weather.Value.airTempC) : "--",
            w ? Degrees(weather.Value.trackTempC) : "--",
            w && weather.Value.windKmh >= 0 ? $"{weather.Value.windKmh:0} km/h" : "--",
        };
        for (int i = 0; i < values.Length; i++)
            SetText(_circuitWeatherCard, "MetricValue" + i, values[i]);
    }

    static string Degrees(float value) => float.IsNaN(value) ? "--" : $"{value:0}°";

    /// <summary>Rebuilt periodically so "dans 12 min" and the next-session countdown stay current.</summary>
    void RefreshWeekendSchedule()
    {
        var sessions = _weekend != null ? _weekend.sessions : null;
        int nextIndex = -1, liveIndex = -1;
        if (sessions != null)
            for (int i = 0; i < sessions.Count; i++)
            {
                if (sessions[i].status == WeekendSessionStatus.Live && liveIndex < 0) liveIndex = i;
                if (sessions[i].status == WeekendSessionStatus.Upcoming && nextIndex < 0) nextIndex = i;
            }

        BindNextSession(sessions, liveIndex, nextIndex);
        BindProgram(sessions, liveIndex, nextIndex);
    }

    void BindNextSession(List<WeekendSession> sessions, int liveIndex, int nextIndex)
    {
        string name, number = "", unit = "";
        if (liveIndex >= 0) { name = sessions[liveIndex].name; number = "EN"; unit = "cours"; }
        else if (nextIndex >= 0)
        {
            name = sessions[nextIndex].name;
            TimeSpan span = sessions[nextIndex].startsAt - DateTime.Now;
            if (span < TimeSpan.Zero) span = TimeSpan.Zero;
            if (span.TotalMinutes < 60)    { number = Mathf.CeilToInt((float)span.TotalMinutes).ToString(); unit = "minutes"; }
            else if (span.TotalHours < 48) { number = $"{(int)span.TotalHours}h{span.Minutes:00}"; unit = "avant le départ"; }
            else                           { number = ((int)span.TotalDays).ToString(); unit = "jours"; }
        }
        else name = "Week-end terminé";

        SetText(_nextSessionCard, "SessionName", name);
        SetText(_nextSessionCard, "BigNumber", number);
        SetText(_nextSessionCard, "BigUnit", unit);
    }

    /// <summary>Design programme: one row per session (27-px name, 20-px time), state chip on the right.</summary>
    void BindProgram(List<WeekendSession> sessions, int liveIndex, int nextIndex)
    {
        if (_programContent == null) return;
        Clear(_programContent);
        if (sessions == null || sessions.Count == 0) return;

        float w = _programContent.rect.width / ODDesign.Scale;
        float viewH = _programContent.parent.GetComponent<RectTransform>().rect.height / ODDesign.Scale;
        float rowH = Mathf.Clamp(viewH / sessions.Count, 92f, 124f);
        for (int i = 0; i < sessions.Count; i++)
        {
            WeekendSession session = sessions[i];
            bool done = session.status == WeekendSessionStatus.Completed;
            bool next = i == nextIndex, live = i == liveIndex;
            var row = LayoutRow(_programContent, "Session_" + session.name, ODDesign.Px(rowH));

            string time = $"{FrenchDays[(int)session.startsAt.DayOfWeek]}. {session.startsAt:HH}:{session.startsAt:mm}";
            if (next) time += " · " + RelativeIn(session.startsAt - DateTime.Now);

            (string label, Color fill, Color text) =
                  live ? ("En cours", ODDesign.WithAlpha(ODDesign.Live, .30f), Color.white)
                : next ? ("Prochain", Color.white, ODDesign.Ink)
                : done ? ("Terminé",  new Color(1f, 1f, 1f, .08f), ODDesign.Secondary)
                       : ("À venir",  ODDesign.WithAlpha(ODDesign.Gold, .18f), ODDesign.GoldText);

            float chipW = 150f;
            ODDesign.Text(row, "Name", session.name, 0f, rowH * .5f - 36f, w - chipW - 14f, 38f, 27f, ODDesign.Weight.SemiBold,
                done ? ODDesign.Tertiary : Color.white);
            ODDesign.Text(row, "Time", time, 0f, rowH * .5f + 4f, w - chipW - 14f, 30f, 20f, ODDesign.Weight.Regular,
                done ? ODDesign.Tertiary : ODDesign.WithAlpha(Color.white, .64f));
            var chip = ODDesign.Fill(row, "Badge", w - chipW, (rowH - 46f) * .5f, chipW, 46f, fill, 23f);
            var chipLabel = ODDesign.Text(chip.transform, "Label", label, 0f, 0f, chipW, 46f, 18f, ODDesign.Weight.Bold, text,
                TextAlignmentOptions.Center, caps: next || live, trackingEm: .04f);
            chipLabel.overflowMode = TextOverflowModes.Overflow;

            if (i < sessions.Count - 1) ODDesign.Line(row, 0f, rowH - 1.6f, w);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Live + replay — podium and circuit cards share the same child names
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>P1–P3 rows next to the builder's static "P1/P2/P3" labels.</summary>
    void BindPodium(RectTransform card, List<LiveEntry> entries)
    {
        if (card == null) return;
        var rows = Child(card, "Rows") ?? Rect(card, "Rows", 0, 0, card.rect.width, card.rect.height);
        Clear(rows);

        float w = card.rect.width / ODDesign.Scale;
        for (int i = 0; i < 3; i++)
        {
            if (entries == null || i >= entries.Count) continue;
            LiveEntry entry = entries[i];
            float y = 12f + i * 92f;
            ODDesign.Text(rows, "Name_" + i, entry.name, 102f, y + 12f, w - 102f - 36f - 150f, 40f, 30f, ODDesign.Weight.SemiBold);
            ODDesign.Text(rows, "Team_" + i, entry.teamName, 102f, y + 52f, w - 102f - 36f - 150f, 28f, 20f, ODDesign.Weight.Regular, ODDesign.WithAlpha(Color.white, .64f));
            ODDesign.Text(rows, "Gap_" + i, i == 0 ? "Leader" : entry.gap, w - 36f - 150f, y, 150f, 92f, 30f, ODDesign.Weight.SemiBold,
                i == 0 ? ODDesign.GoldText : Color.white, TextAlignmentOptions.MidlineRight);
            if (i < 2) ODDesign.Line(rows, 36f, y + 92f - 1.6f, w - 72f, ODDesign.DividerStrong);
        }
    }

    void BindLapProgress(RectTransform card, string left, string right, float ratio)
    {
        if (card == null) return;
        SetText(card, "LapProgress", left);

        var lap = Child(card, "LapProgress");
        var percent = Child(card, "LapPercent")?.GetComponent<TextMeshProUGUI>();
        if (percent == null && lap != null)
        {
            percent = ODDesign.Text(card, "LapPercent", "", lap.anchoredPosition.x / ODDesign.Scale, -lap.anchoredPosition.y / ODDesign.Scale,
                lap.rect.width / ODDesign.Scale, lap.rect.height / ODDesign.Scale, 20f, ODDesign.Weight.Regular, ODDesign.Muted);
            percent.alignment = TextAlignmentOptions.MidlineRight;
        }
        if (percent != null) percent.text = right;

        var track = Child(card, "ProgressTrack");
        if (track == null) return;
        var fill = Child(track, "Fill") ?? Fill(track, "Fill", 0, 0, 0, track.rect.height, Color.white, track.rect.height / 2).rectTransform;
        fill.sizeDelta = new Vector2(track.rect.width * Mathf.Clamp01(ratio), fill.sizeDelta.y);
    }

    void BindTrackStatus(RectTransform card, string trackStatus)
    {
        if (card == null) return;
        float w = card.rect.width / ODDesign.Scale;
        var pill = Child(card, "TrackStatus") ?? Rect(card, "TrackStatus", 0, 0, 10, 10);
        Clear(pill);
        pill.gameObject.SetActive(!string.IsNullOrEmpty(trackStatus));
        if (string.IsNullOrEmpty(trackStatus)) return;

        string s = trackStatus.ToLowerInvariant();
        bool red = s.Contains("rouge") || s.Contains("red");
        bool yellow = s.Contains("safety") || s.Contains("jaune") || s.Contains("yellow") || s.Contains("vsc");
        Color colour = red ? ODDesign.Live : yellow ? ODDesign.TyreMedium : ODDesign.Green;
        Color text = red ? ODDesign.Destructive : yellow ? ODDesign.TyreMedium : ODDesign.GreenText;

        var label = ODDesign.Text(pill, "Label", trackStatus, 46f, 0f, 260f, 46f, 18f, ODDesign.Weight.SemiBold, text);
        float pillW = 46f + label.GetPreferredValues(trackStatus).x / ODDesign.Scale + 18f;
        ODDesign.Place(pill, w - 36f - pillW, 24f, pillW, 46f);
        var bg = ODDesign.Fill(pill, "Background", 0f, 0f, pillW, 46f, ODDesign.WithAlpha(colour, .18f), 23f);
        bg.transform.SetAsFirstSibling();
        ODDesign.Fill(pill, "Dot", 18f, 18f, 10f, 10f, colour, 5f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Standings — shared by off-season and weekend: one table per category,
    // one row per entry, inside a scrollable area of the card
    // ═══════════════════════════════════════════════════════════════════════

    StandingsView BuildStandingsView(RectTransform card, float bottom, float padPx)
    {
        if (card == null) return null;

        // The builder's static column headers and dividers are replaced by
        // per-table headers/rows generated inside the scroll area.
        foreach (Transform child in card)
            if (child.name == "Divider" || child.name == "NameColumn" || child.name == "PointsColumn")
                child.gameObject.SetActive(false);

        // Wide card (hors direct): 40-px title line; narrow (week-end): 36-px title.
        bool wide = card.rect.width / ODDesign.Scale > 800f;
        float top = ODDesign.Px(wide ? 136f : 100f);
        return new StandingsView { card = card, content = BuildScrollArea(card, top, bottom, padPx), padPx = padPx, wide = wide };
    }

    /// <summary>
    /// One block per category, as on the mobile app: centred category title,
    /// Pilotes / Équipes segmented control, "Nom / PTS" header, one row per entry.
    /// <paramref name="preferred"/> is the initial tab of categories not chosen yet.
    /// </summary>
    void RenderStandings(StandingsView view, List<SeasonStandingCategory> categories, StandingType preferred)
    {
        if (view == null) return;
        view.categories = new List<SeasonStandingCategory>();
        if (categories != null)
            foreach (var category in categories)
                if ((category.drivers != null && category.drivers.Count > 0) || (category.teams != null && category.teams.Count > 0))
                    view.categories.Add(category);
        view.preferred = preferred;
        if (view.categoryIndex >= view.categories.Count) view.categoryIndex = 0;
        RenderCategory(view);
    }

    /// <summary>
    /// One category at a time, as in the mockups: its name as the card title
    /// (a button cycling through the categories when there are several), the
    /// Pilotes / Équipes switch on the right, then the table.
    /// </summary>
    void RenderCategory(StandingsView view)
    {
        Clear(view.content);
        foreach (var old in view.headerControls) if (old != null) Destroy(old);
        view.headerControls.Clear();

        float pad = view.padPx;
        float cardW = view.card.rect.width / ODDesign.Scale;
        float titleY = view.wide ? 44f : 30f, titleH = view.wide ? 64f : 52f;
        var heading = view.card.Find("Heading")?.GetComponent<TextMeshProUGUI>();
        if (heading != null)
        {
            ODDesign.Place(heading.transform, pad, titleY, cardW - pad * 2f - (view.wide ? 400f : 270f), titleH);
            ODDesign.Style(heading, view.wide ? 40f : 36f, ODDesign.Weight.Bold, ODDesign.Primary);
        }

        if (view.categories.Count == 0)
        {
            SetText(view.card, "Heading", view.wide ? "Classement" : "Championnat");
            var row = LayoutRow(view.content, "Empty", ODDesign.Px(80f));
            ODDesign.Text(row, "Label", "Classement indisponible", 0f, 0f, 600f, 80f, 22f, ODDesign.Weight.Regular, ODDesign.Secondary);
            return;
        }

        var category = view.categories[view.categoryIndex];
        bool hasDrivers = category.drivers != null && category.drivers.Count > 0;
        bool hasTeams   = category.teams   != null && category.teams.Count   > 0;
        string key = category.label ?? "";
        if (!view.types.TryGetValue(key, out StandingType type)) type = view.preferred;
        if (type == StandingType.Drivers && !hasDrivers) type = StandingType.Teams;
        if (type == StandingType.Teams && !hasTeams) type = StandingType.Drivers;

        bool several = view.categories.Count > 1;
        string title = view.wide ? (string.IsNullOrEmpty(category.label) ? "Classement" : "Classement " + category.label)
                                 : (string.IsNullOrEmpty(category.label) ? "Championnat" : category.label);
        SetText(view.card, "Heading", several ? "" : title);
        if (several) view.headerControls.Add(CategoryButton(view, title, view.categoryIndex, view.categories.Count, pad, titleY, titleH));

        float w = view.content.rect.width / ODDesign.Scale;
        ODSegmentedControl control = null;
        string[] tabs = { "Pilotes", "Équipes" };
        int selected = type == StandingType.Drivers ? 0 : 1;
        if (hasDrivers && hasTeams)
        {
            float sw = view.wide ? 380f : 250f;
            control = ODSegmentedControl.Create(view.card, ODDesign.Px(cardW - pad - sw), ODDesign.Px(titleY), ODDesign.Px(sw), ODDesign.Px(titleH),
                tabs, selected, ODDesign.Px(view.wide ? 21f : 19f));
            view.headerControls.Add(control.gameObject);
        }

        if (view.wide)
        {
            var header = LayoutRow(view.content, "ColumnHeader", ODDesign.Px(44f));
            Header(header, w);
        }
        var rows = RowsContainer(view.content);
        rows.gameObject.AddComponent<ODFadeIn>();

        if (control != null)
            control.Changed += index =>
            {
                view.types[key] = index == 0 ? StandingType.Drivers : StandingType.Teams;
                FillStandingRows(view, rows, index == 0 ? category.drivers : category.teams);
                rows.GetComponent<ODFadeIn>().Play();
            };

        FillStandingRows(view, rows, type == StandingType.Drivers ? category.drivers : category.teams);
        view.content.anchoredPosition = Vector2.zero;   // back to top
    }

    /// <summary>The card title itself cycles the categories: "Classement Hypercar  1/3 ›" on a ghost capsule.</summary>
    GameObject CategoryButton(StandingsView view, string title, int index, int count, float x, float y, float h)
    {
        float size = view.wide ? 40f : 36f;
        string text = $"{title}  <color=#FFFFFF80><size=55%>{index + 1}/{count}  ›</size></color>";
        var measure = ODDesign.Text(view.card, "Measure", text, 0f, 0f, 1200f, h, size, ODDesign.Weight.Bold);
        measure.richText = true;
        float width = measure.GetPreferredValues(text).x / ODDesign.Scale + 40f;
        Destroy(measure.gameObject);

        var button = ODDesign.Button(view.card, "CategoryButton", "", x - 20f, y, width, h, ODDesign.ButtonKind.Ghost);
        button.onClick.AddListener(() =>
        {
            view.categoryIndex = (view.categoryIndex + 1) % view.categories.Count;
            RenderCategory(view);
        });
        var caption = ODDesign.Text(button.transform, "Label", text, 20f, 0f, width - 20f, h, size, ODDesign.Weight.Bold);
        caption.richText = true;
        caption.overflowMode = TextOverflowModes.Overflow;
        return button.gameObject;
    }

    // Row layout (design px): wide = hors direct table, narrow = week-end card.
    const float WideRowH = 100f, NarrowRowH = 96f, PtsW = 90f, GapW = 110f;

    RectTransform RowsContainer(RectTransform block)
    {
        var rows = LayoutRow(block, "Rows", 0f);
        rows.GetComponent<LayoutElement>().preferredHeight = -1f;   // height from the rows inside
        var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return rows;
    }

    void Header(RectTransform row, float w)
    {
        Color c = ODDesign.ColumnText;
        ODDesign.Text(row, "Pos", "Pos", 4f, 0f, 40f, 40f, 17f, ODDesign.Weight.SemiBold, c, caps: true, trackingEm: .12f);
        ODDesign.Text(row, "Name", "Nom", 148f, 0f, 300f, 40f, 17f, ODDesign.Weight.SemiBold, c, caps: true, trackingEm: .12f);
        ODDesign.Text(row, "Gap", "Écart", w - 4f - PtsW - 20f - GapW, 0f, GapW, 40f, 17f, ODDesign.Weight.SemiBold, c, TextAlignmentOptions.MidlineRight, caps: true, trackingEm: .12f);
        ODDesign.Text(row, "Points", "Pts", w - 4f - PtsW, 0f, PtsW, 40f, 17f, ODDesign.Weight.SemiBold, c, TextAlignmentOptions.MidlineRight, caps: true, trackingEm: .12f);
        ODDesign.Line(row, 0f, 42f, w, ODDesign.DividerStrong);
    }

    void FillStandingRows(StandingsView view, RectTransform rows, List<SeasonStandingEntry> entries)
    {
        if (rows == null) return;
        Clear(rows);
        if (entries == null) return;
        float leader = entries.Count > 0 ? entries[0].points : 0f;
        for (int i = 0; i < entries.Count; i++) AddStandingRow(view, rows, entries[i], leader, i == entries.Count - 1);
    }

    void AddStandingRow(StandingsView view, RectTransform rows, SeasonStandingEntry entry, float leaderPoints, bool last)
    {
        bool wide = view.wide;
        float h = wide ? WideRowH : NarrowRowH;
        var row = LayoutRow(rows, "Row_" + entry.position, ODDesign.Px(h));
        float w = view.content.rect.width / ODDesign.Scale;
        bool first = entry.position == 1;
        Color rank = first ? ODDesign.GoldText : ODDesign.RankText;

        if (wide)
        {
            ODDesign.Text(row, "Position", entry.position.ToString(), 4f, 0f, 40f, h, 27f, ODDesign.Weight.Bold, rank);
            var badge = ODDesign.Fill(row, "Avatar", 64f, (h - 52f) * .5f, 64f, 52f, ODDesign.Avatar, 16f);
            ODDesign.Text(badge.transform, "Initials", Initials(entry.name), 0f, 0f, 64f, 52f, 19f, ODDesign.Weight.Bold, Color.white, TextAlignmentOptions.Center);
            float nameX = 148f, nameW = w - nameX - PtsW - GapW - 40f;
            Names(row, entry, nameX, nameW, h, 26f, 19f);
            string gap = first ? "—" : "−" + FormatPoints(Mathf.Max(0f, leaderPoints - entry.points));
            ODDesign.Text(row, "Gap", gap, w - 4f - PtsW - 20f - GapW, 0f, GapW, h, 22f, ODDesign.Weight.Regular, ODDesign.Muted, TextAlignmentOptions.MidlineRight);
            ODDesign.Text(row, "Points", FormatPoints(entry.points), w - 4f - PtsW, 0f, PtsW, h, 28f, ODDesign.Weight.Bold,
                first ? ODDesign.GoldText : Color.white, TextAlignmentOptions.MidlineRight);
        }
        else
        {
            ODDesign.Text(row, "Position", entry.position.ToString(), 0f, 0f, 34f, h, 26f, ODDesign.Weight.Bold, rank);
            var avatar = ODDesign.Fill(row, "Avatar", 48f, (h - 54f) * .5f, 54f, 54f, ODDesign.Avatar, 27f);
            ODDesign.Text(avatar.transform, "Initials", Initials(entry.name), 0f, 0f, 54f, 54f, 18f, ODDesign.Weight.SemiBold, Color.white, TextAlignmentOptions.Center);
            Names(row, entry, 120f, w - 120f - PtsW - 12f, h, 26f, 19f);
            ODDesign.Text(row, "Points", FormatPoints(entry.points), w - PtsW, 0f, PtsW, h, 27f, ODDesign.Weight.Bold,
                first ? ODDesign.GoldText : Color.white, TextAlignmentOptions.MidlineRight);
        }
        if (!last) ODDesign.Line(row, 0f, h - 1.6f, w);
    }

    static void Names(RectTransform row, SeasonStandingEntry entry, float x, float w, float h, float nameSize, float teamSize)
    {
        if (!string.IsNullOrEmpty(entry.teamName))
        {
            ODDesign.Text(row, "Name", entry.name, x, h * .5f - 34f, w, 36f, nameSize, ODDesign.Weight.SemiBold);
            ODDesign.Text(row, "Team", entry.teamName, x, h * .5f + 4f, w, 28f, teamSize, ODDesign.Weight.Regular, ODDesign.WithAlpha(Color.white, .64f));
        }
        else
            ODDesign.Text(row, "Name", entry.name, x, 0f, w, h, nameSize, ODDesign.Weight.SemiBold);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Formatting
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>"Hartley" → "HA", "Toyota Gazoo Racing" → "TG".</summary>
    static string Initials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        string[] words = name.Trim().Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2) return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpperInvariant();
        return words[0].Substring(0, Mathf.Min(2, words[0].Length)).ToUpperInvariant();
    }

    static string FormatPoints(float points) =>
        points % 1f == 0f ? ((int)points).ToString() : points.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>"dans 12 min", "dans 4 h 12", "dans 3 j".</summary>
    static string RelativeIn(TimeSpan span)
    {
        if (span <= TimeSpan.Zero)  return "maintenant";
        if (span.TotalMinutes < 60) return $"dans {Mathf.CeilToInt((float)span.TotalMinutes)} min";
        if (span.TotalHours < 48)   return $"dans {(int)span.TotalHours} h {span.Minutes:00}";
        return $"dans {(int)span.TotalDays} j";
    }

    static string JoinNonEmpty(string separator, params string[] parts)
    {
        var kept = new List<string>();
        foreach (var part in parts) if (!string.IsNullOrEmpty(part)) kept.Add(part);
        return string.Join(separator, kept);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Runtime UI helpers — same visual vocabulary as CategoryPagesSetup
    // (which is Editor-only and can't be called at runtime)
    // ═══════════════════════════════════════════════════════════════════════

    RectTransform BuildScrollArea(RectTransform card, float top, float bottom, float padPx)
    {
        float pad = ODDesign.Px(padPx);
        var viewport = Rect(card, "ListViewport", pad, top, card.rect.width - pad * 2, bottom - top);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hitArea = viewport.gameObject.AddComponent<Image>();   // lets the ray pointer drag the list
        hitArea.color = Color.clear;

        var content = Rect(viewport, "Content", 0, 0, viewport.rect.width, 0);
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content           = content;
        scroll.viewport          = viewport;
        scroll.horizontal        = false;
        scroll.movementType      = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;
        return content;
    }

    static RectTransform LayoutRow(RectTransform content, string name, float height)
    {
        var row = Rect(content, name, 0, 0, content.rect.width, height);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return row;
    }

    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go   = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot            = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta        = new Vector2(w, h);
        return rect;
    }

    static RoundedImage Fill(Transform parent, string name, float x, float y, float w, float h, Color color, float radius)
    {
        var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<RoundedImage>();
        image.color          = color;
        image.cornerRadius   = radius;
        image.cornerSegments = 16;
        image.raycastTarget  = false;
        return image;
    }

    static RectTransform Child(Transform parent, string name) =>
        parent != null ? parent.Find(name) as RectTransform : null;

    static void SetLabel(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value;
    }

    static void SetText(Transform card, string child, string value)
    {
        var label = card != null ? card.Find(child)?.GetComponent<TextMeshProUGUI>() : null;
        if (label != null) label.text = value;
    }

    static Button FindButton(Transform card, string child) =>
        card != null ? card.Find(child)?.GetComponent<Button>() : null;

    /// <summary>Deactivated first so layout groups ignore rows until Destroy runs at end of frame.</summary>
    static void Clear(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }
}
