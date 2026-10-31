/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODHomeView - Display side of the Home (Canva 2.1): date and greeting
 ## with search + notifications, then one of three layouts —
 ##  2.1a live:     "En direct" plateau with the top 3, "Cette semaine" + last replay
 ##  2.1b weekend:  "Live · Week-end" plateau with the next session, "Aussi ce week-end" + last replay
 ##  2.1c no race:  next event plateau with its countdown, "À revoir" + "Tes championnats"
 ## Built at runtime from ODDesign; display only — every button raises an
 ## event the page controller handles.
 ##
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using D = ODDesign;

public class ODHomeView : MonoBehaviour
{
    public event Action SearchClicked;
    public event Action NotificationsClicked;
    public event Action RemindClicked;
    public event Action CalendarClicked;
    public event Action AllReplaysClicked;
    public event Action CatalogueClicked;
    public event Action WatchLiveClicked;
    public event Action TelemetryClicked;
    public event Action WeekendClicked;
    /// <summary>An event tile of the week row (replay tiles raise <see cref="ReplayClicked"/>).</summary>
    public event Action AgendaClicked;
    public event Action<int> ReplayClicked;

    // Layout of the 1920 x 1200 design window (Canva 2.1).
    const float Side = D.WindowPadSide, Right = 1920f - D.WindowPadSide;
    const float HeroY = 182f, HeroH = 502f;
    const float SectionY = 716f, CardsY = 786f, CardsH = 349f;
    const float TileW = 361f, TileGap = 27f, TilesW = TileW * 3f + TileGap * 2f;
    const float LeadersX = 1248f, LeadersW = Right - LeadersX;
    const float BoxX = 1310f, BoxY = 38f, BoxW = 430f, BoxH = 426f, BoxRowH = 59f, BoxRowsY = 220f;
    const float PillX = 56f, PillY = 132f, ButtonsY = 377f;

    /// <summary>No account API on the client yet: same name as the profile page.</summary>
    const string UserName = "ClemAlex";

    static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    static readonly Color OnPoster = new Color(1f, 1f, 1f, .80f);

    RectTransform _root, _hero, _sections, _pill, _side;
    TextMeshProUGUI _date, _countdown;
    DateTime? _countdownTarget;
    HomeNextEvent _event;
    float _nextTick;
    bool _built;

    public void Build()
    {
        if (_built) return;
        _built = true;
        _root = (RectTransform)transform;

        var surface = GetComponent<ODHomepageSurface>();
        if (surface == null) surface = gameObject.AddComponent<ODHomepageSurface>();
        surface.color = Color.white;
        surface.raycastTarget = true;   // the window swallows rays like the other pages
        ODChampionshipTheme.ApplyBackground(surface, ODChampionshipTheme.NeutralPalette);

        // Header: date caps above the greeting; search and notifications on the right (no profile chip).
        _date = D.Caps(_root, "Date", "", Side, D.WindowPadTop, 900f, D.PageCaps);
        D.Text(_root, "Greeting", "Bonjour, " + UserName, Side, D.WindowPadTop + 37f, 1200f, 72f, D.TitleXL, D.Weight.Bold);
        var bell = D.IconButton(_root, "Notifications", Right - 68f, 94f, 68f, ODNavigationIcon.Symbol.Bell);
        bell.onClick.AddListener(() => NotificationsClicked?.Invoke());
        var search = D.IconButton(_root, "Search", Right - 68f - 16f - 68f, 94f, 68f, ODNavigationIcon.Symbol.Search);
        search.onClick.AddListener(() => SearchClicked?.Invoke());

        var hero = D.Rect(_root, "Featured", Side, HeroY, Right - Side, HeroH);
        ODChampionshipTheme.ApplyGlass(hero.gameObject.AddComponent<ODHomepageSurface>());
        _hero = hero;

        // Section titles, tiles and the right-hand card change with the state: rebuilt on each Show.
        _sections = D.Rect(_root, "Sections", 0f, 0f, 1920f, 1200f);

        RefreshDate();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // States
    // ═══════════════════════════════════════════════════════════════════════

    public void ShowLoading()
    {
        Build();
        ClearContent();
        HeroText(null, "Chargement…", "");
    }

    /// <summary>Repaints from the current state of <paramref name="data"/> (kept by the page and refilled in place).</summary>
    public void Show(HomeData data, bool reminderOn)
    {
        Build();
        ClearContent();
        RefreshDate();
        _event = data.nextEvent;
        switch (data.state)
        {
            case HomeState.Live:
                BuildLiveHero(data.nextEvent, data.live);
                BuildWeekRow("Cette semaine", data);
                break;
            case HomeState.Weekend:
                BuildWeekendHero(data.nextEvent, reminderOn);
                BuildWeekRow("Aussi ce week-end", data);
                break;
            default:
                BuildNextEventHero(data.nextEvent, reminderOn);
                BuildReplays(data.replays);
                BuildLeaders(data.leaders);
                break;
        }
    }

    /// <summary>Live refresh: only the status pill and the top 3 change.</summary>
    public void ShowLive(LiveSessionData live)
    {
        if (_pill == null || _side == null) return;
        ODRuntimeUI.Clear(_pill);
        ODRuntimeUI.Clear(_side);
        LivePill(live);
        BuildTop3(live);
    }

    void ClearContent()
    {
        ODRuntimeUI.Clear(_hero);
        ODRuntimeUI.Clear(_sections);
        _countdown = null;
        _countdownTarget = null;
        _pill = _side = null;
        _event = null;
    }

    void Update()
    {
        if (Time.unscaledTime < _nextTick) return;
        _nextTick = Time.unscaledTime + 1f;
        RefreshDate();
        if (_countdown != null && _countdownTarget.HasValue) _countdown.text = Countdown(_countdownTarget.Value - DateTime.Now);
    }

    void RefreshDate()
    {
        if (_date != null) _date.text = DateTime.Now.ToString("dddd d MMMM", French);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 2.1a — a session is live
    // ═══════════════════════════════════════════════════════════════════════

    void BuildLiveHero(HomeNextEvent next, LiveSessionData live)
    {
        _pill = D.Rect(_hero, "StatusArea", PillX, PillY, 1000f, 52f);
        _side = D.Rect(_hero, "Top3Area", BoxX, BoxY, BoxW, BoxH);
        LivePill(live);

        string session = live?.sessionName;
        HeroText(JoinNonEmpty(" · ", next?.championshipName, session), next?.eventName ?? "En direct",
            JoinNonEmpty(" · ", next?.location, live?.conditions));

        var watch = IconLabelButton("WatchLive", "Rejoindre le direct", 56f, 319f, D.ButtonKind.Primary,
            ODNavigationIcon.Symbol.Play, D.Ink, D.Ink, iconFirst: true, iconPx: 20f);
        watch.onClick.AddListener(() => WatchLiveClicked?.Invoke());
        var telemetry = D.Button(_hero, "Telemetry", "Télémétrie", 56f + 319f + 16f, ButtonsY, 226f, 68f, D.ButtonKind.Secondary);
        telemetry.onClick.AddListener(() => TelemetryClicked?.Invoke());

        BuildTop3(live);
    }

    void LivePill(LiveSessionData live)
    {
        string text = "En direct";
        if (live != null)
        {
            bool laps = (live.sessionName == "Course" || live.sessionName == "Sprint") && live.currentLap > 0;
            text += laps ? " · Tour " + live.currentLap + (live.totalLaps > 0 ? " / " + live.totalLaps : "")
                         : string.IsNullOrEmpty(live.sessionName) ? "" : " · " + live.sessionName;
        }
        StatusPill(_pill, 0f, 0f, text, ODStatusStyle.Live);
    }

    /// <summary>Recessed card: "Top 3", then position, driver, team and gap to the leader.</summary>
    void BuildTop3(LiveSessionData live)
    {
        var box = RecessedBox(_side, 0f, 0f);
        D.Caps(box, "Caps", "Top 3", 32f, 26f, BoxW - 64f, D.CapsSize);

        var entries = live?.entries ?? new List<LiveEntry>();
        if (entries.Count == 0)
        {
            D.Text(box, "Empty", "Classement dès le premier tour", 32f, 56f, BoxW - 64f, BoxH - 88f, 21f, D.Weight.Regular, D.Tertiary,
                TextAlignmentOptions.Center);
            return;
        }
        const float rowsY = 56f, rowH = 116f;
        for (int i = 0; i < entries.Count && i < 3; i++)
        {
            var entry = entries[i];
            float top = rowsY + i * rowH, centre = top + rowH * .5f;
            if (i > 0) D.Line(box, 32f, top, BoxW - 64f, D.DividerStrong);
            D.Text(box, "Position" + i, entry.position.ToString(), 32f, centre - 20f, 36f, 40f, 26f, D.Weight.SemiBold, D.Secondary);
            D.Text(box, "Driver" + i, entry.name, 78f, centre - 34f, 210f, 34f, 27f, D.Weight.SemiBold);
            D.Text(box, "Team" + i, entry.teamName, 78f, centre + 2f, 210f, 26f, 18f, D.Weight.Regular, D.Secondary);
            bool leader = entry.position == 1;
            D.Text(box, "Gap" + i, leader ? "Leader" : FrenchGap(entry.gap), BoxW - 32f - 120f, top, 120f, rowH, 24f, D.Weight.SemiBold,
                leader ? D.GoldText : D.Primary, TextAlignmentOptions.MidlineRight);
        }
    }

    /// <summary>"+3.4s" → "+3,4 s" (laps and other labels unchanged).</summary>
    static string FrenchGap(string gap)
    {
        if (string.IsNullOrEmpty(gap) || gap.Length < 2 || !gap.EndsWith("s") || !char.IsDigit(gap[gap.Length - 2])) return gap ?? "";
        return gap.Substring(0, gap.Length - 1).Replace('.', ',') + " s";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 2.1b — event weekend, between two sessions
    // ═══════════════════════════════════════════════════════════════════════

    void BuildWeekendHero(HomeNextEvent next, bool reminderOn)
    {
        StatusPill(_hero, PillX, PillY, "Live · Week-end", ODStatusStyle.Weekend);
        if (next == null) return;

        string round = next.round > 0 ? "Manche " + next.round + (next.roundCount >= next.round ? " / " + next.roundCount : "") : null;
        HeroText(JoinNonEmpty(" · ", next.championshipName, round, next.place), next.eventName,
            !string.IsNullOrEmpty(next.headline) ? next.headline : next.location);

        const float weekendW = 320f;
        var weekend = IconLabelButton("Weekend", "Voir le week-end", 56f, weekendW, D.ButtonKind.Primary,
            ODNavigationIcon.Symbol.Back, D.Ink, D.Ink, iconFirst: false, iconPx: 22f);
        weekend.onClick.AddListener(() => WeekendClicked?.Invoke());
        bool hasNext = next.sessions.Exists(s => !s.finished);
        if (hasNext) RemindButton(56f + weekendW + 16f, reminderOn, D.ButtonKind.Secondary);

        BuildNextSession(next);
    }

    /// <summary>Recessed card: next session and its countdown, then the last finished and the coming key sessions.</summary>
    void BuildNextSession(HomeNextEvent next)
    {
        var box = RecessedBox(_hero, BoxX, BoxY);
        D.Caps(box, "Caps", "Prochaine session", 32f, 26f, BoxW - 64f, D.CapsSize);

        int nextIndex = next.sessions.FindIndex(s => !s.finished);
        if (nextIndex >= 0)
        {
            var session = next.sessions[nextIndex];
            D.Text(box, "Session", session.name, 32f, 58f, BoxW - 64f, 42f, 30f, D.Weight.SemiBold);
            _countdownTarget = session.startsAt;
            _countdown = D.Text(box, "Value", Countdown(session.startsAt - DateTime.Now), 30f, 98f, BoxW - 60f, 104f, 84f);
            _countdown.richText = true;
        }
        else D.Text(box, "Session", "Week-end terminé", 32f, 58f, BoxW - 64f, 42f, 30f, D.Weight.SemiBold);

        for (int i = 0; i < next.sessions.Count && i < ODHomeMapper.MaxSessions; i++)
        {
            var session = next.sessions[i];
            float y = BoxRowsY + i * BoxRowH;
            D.Line(box, 32f, y, BoxW - 64f, D.DividerStrong);
            D.Text(box, "Session" + i, session.name, 32f, y, BoxW - 64f - 150f, BoxRowH, 21f,
                session.finished ? D.Weight.Regular : D.Weight.SemiBold, session.finished ? D.Secondary : D.Primary);
            string when = session.finished ? "Terminé" : Capitalise(session.startsAt.ToString("ddd HH:mm", French));
            D.Text(box, "Time" + i, when, BoxW - 32f - 150f, y, 150f, BoxRowH, 21f, D.Weight.Regular,
                i == nextIndex ? D.GoldText : D.Secondary, TextAlignmentOptions.MidlineRight);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 2.1c — no race today: next event
    // ═══════════════════════════════════════════════════════════════════════

    void BuildNextEventHero(HomeNextEvent next, bool reminderOn)
    {
        DateTime today = DateTime.Today;
        bool raceToday = next != null && next.sessions.Exists(s => s.startsAt.Date == today);
        StatusPill(_hero, PillX, PillY, raceToday ? "Course aujourd’hui" : "Aucune course aujourd’hui",
            raceToday ? ODStatusStyle.Weekend : ODStatusStyle.OffSeason);

        if (next == null)
        {
            HeroText("Prochain rendez-vous", "Aucune course programmée", "Le calendrier n’a plus de date à venir");
            CalendarButton(56f);
            return;
        }

        HeroText("Prochain rendez-vous · " + next.championshipName, next.eventName,
            JoinNonEmpty(" · ", next.location, StartsLabel(next.startsAt)));
        RemindButton(56f, reminderOn, D.ButtonKind.Primary);
        CalendarButton(56f + 248f + 16f);
        BuildCountdown(next);
    }

    /// <summary>Recessed card: "&lt;session&gt; dans" + big countdown, then the key sessions, one per row.</summary>
    void BuildCountdown(HomeNextEvent next)
    {
        var box = RecessedBox(_hero, BoxX, BoxY);

        string first = next.sessions.Count > 0 ? next.sessions[0].name : null;
        _countdownTarget = next.sessions.Count > 0 ? next.sessions[0].startsAt : next.startsAt;
        D.Caps(box, "Caps", CountdownCaps(first), 32f, 26f, BoxW - 64f, D.CapsSize);
        _countdown = D.Text(box, "Value", Countdown(_countdownTarget.Value - DateTime.Now), 30f, 62f, BoxW - 60f, 104f, 84f);
        _countdown.richText = true;

        // No session programme yet: one row for the start of the event.
        var rows = next.sessions.Count > 0 ? next.sessions
            : new List<HomeSession> { new HomeSession { name = "Début", startsAt = next.startsAt } };
        int month = rows[0].startsAt.Month;
        for (int i = 0; i < rows.Count && i < ODHomeMapper.MaxSessions; i++)
        {
            var session = rows[i];
            float y = BoxRowsY + i * BoxRowH;
            D.Line(box, 32f, y, BoxW - 64f, D.DividerStrong);
            D.Text(box, "Session" + i, DayLabel(session.startsAt, month) + " · " + session.name, 32f, y, BoxW - 64f - 80f, BoxRowH, 21f, D.Weight.SemiBold);
            D.Text(box, "Time" + i, session.startsAt.ToString("HH:mm", French), BoxW - 32f - 100f, y, 100f, BoxRowH, 21f,
                D.Weight.Regular, D.Secondary, TextAlignmentOptions.MidlineRight);
        }
    }

    static string CountdownCaps(string session)
    {
        if (string.IsNullOrEmpty(session)) return "Début dans";
        return session == "Essais libres 1" ? "Premiers essais dans" : session + " dans";
    }

    static string StartsLabel(DateTime start)
    {
        DateTime today = DateTime.Today;
        if (start.Date == today) return "aujourd’hui";
        if (start.Date == today.AddDays(1)) return "à partir de demain";
        return "à partir du " + start.ToString("dddd d MMMM", French);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Plateau pieces
    // ═══════════════════════════════════════════════════════════════════════

    void HeroText(string caps, string title, string subtitle)
    {
        if (!string.IsNullOrEmpty(caps)) D.Caps(_hero, "Caps", caps, 56f, 206f, 1200f, 22f);
        D.Text(_hero, "Title", title, 56f, 238f, 1220f, 76f, 60f, D.Weight.Bold);
        D.Text(_hero, "Subtitle", subtitle, 56f, 316f, 1220f, 32f, D.Body, D.Weight.Regular, D.Secondary);
    }

    void RemindButton(float x, bool reminderOn, D.ButtonKind kind)
    {
        Color ink = reminderOn ? D.GoldLight : kind == D.ButtonKind.Primary ? D.Ink : D.Primary;
        var remind = IconLabelButton("Remind", reminderOn ? "Rappel activé" : "Me prévenir", x, 248f,
            reminderOn ? D.ButtonKind.Gold : kind, ODNavigationIcon.Symbol.Bell, ink, ink, iconFirst: true, iconPx: 26f);
        remind.onClick.AddListener(() => RemindClicked?.Invoke());
    }

    void CalendarButton(float x)
    {
        var calendar = D.Button(_hero, "Calendar", "Voir le calendrier", x, ButtonsY, 258f, 68f, D.ButtonKind.Secondary);
        calendar.onClick.AddListener(() => CalendarClicked?.Invoke());
    }

    /// <summary>Pill button with an icon before the label (bell, play) or after it (arrow — the back chevron mirrored).</summary>
    Button IconLabelButton(string name, string label, float x, float w, D.ButtonKind kind, ODNavigationIcon.Symbol symbol,
        Color iconColour, Color textColour, bool iconFirst, float iconPx)
    {
        const float h = 68f, pad = 36f, gap = 12f;
        var button = D.Button(_hero, name, "", x, ButtonsY, w, h, kind);
        float iconX = iconFirst ? pad : w - pad - iconPx;
        var icon = D.Rect(button.transform, "Icon", iconX, (h - iconPx) * .5f, iconPx, iconPx).gameObject.AddComponent<ODNavigationIcon>();
        icon.symbol = symbol;
        icon.color = iconColour;
        icon.raycastTarget = false;
        if (!iconFirst) icon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        float textX = iconFirst ? pad + iconPx + gap : pad;
        D.Text(button.transform, "Label", label, textX, 0f, w - textX - (iconFirst ? pad - 8f : pad + iconPx + gap - 8f), h, D.Body,
            D.Weight.SemiBold, textColour);
        return button;
    }

    /// <summary>The plateau's inset card (countdown, top 3, next session).</summary>
    static RectTransform RecessedBox(Transform parent, float x, float y)
    {
        var box = D.Rect(parent, "Card", x, y, BoxW, BoxH);
        var surface = box.gameObject.AddComponent<ODHomepageSurface>();
        surface.glass = true;
        surface.topLeft = surface.bottomRight = D.Recessed;
        surface.sheen = 0f;
        surface.shadowSize = 0f;
        surface.cornerRadius = D.Px(D.PlateauRadius);
        surface.rimWidth = 1.5f;
        surface.rimTop = new Color(1f, 1f, 1f, .16f);
        surface.rimBottom = new Color(1f, 1f, 1f, .10f);
        return box;
    }

    static string Countdown(TimeSpan left)
    {
        const string unit = "<size=24%><color=#FFFFFFAA> {0}</color></size>";
        if (left <= TimeSpan.Zero) return "En cours";
        if (left.TotalDays >= 1d)
            return $"{(int)left.TotalDays}{string.Format(unit, (int)left.TotalDays > 1 ? "jours" : "jour")}  {left.Hours:00}{string.Format(unit, "h")}";
        if (left.TotalHours >= 1d)
            return $"{left.Hours:00}{string.Format(unit, "h")}  {left.Minutes:00}{string.Format(unit, "min")}";
        return $"{Mathf.Max(1, left.Minutes)}{string.Format(unit, "min")}";
    }

    /// <summary>"Ven. 30", "Dim. 1er nov." — the month only when it changes from the first row.</summary>
    static string DayLabel(DateTime date, int firstMonth)
    {
        string day = date.ToString("ddd", French) + " " + (date.Day == 1 ? "1er" : date.Day.ToString());
        if (date.Month != firstMonth) day += " " + date.ToString("MMM", French);
        return Capitalise(day);
    }

    void StatusPill(Transform parent, float x, float y, string text, ODStatusStyle style)
    {
        D.StatusColours(style, out Color fill, out Color dot, out bool glow);
        const float h = 52f, pad = 22f, dotSize = 12f, gap = 12f;
        var label = D.Text(parent, "StatusLabel", text, 0f, 0f, 600f, h, D.CapsSize, D.Weight.Bold, Color.white, caps: true, trackingEm: .08f);
        float textW = label.GetPreferredValues(label.text).x / D.Scale;
        var pill = D.Fill(parent, "Status", x, y, pad + dotSize + gap + textW + pad, h, fill, h * .5f);
        D.Dot(pill.transform, pad, (h - dotSize) * .5f, dotSize, dot, glow);
        label.transform.SetParent(pill.transform, false);
        D.Place(label.transform, pad + dotSize + gap, 0f, textW + 4f, h);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Week row (2.1a / 2.1b): other events + last replay
    // ═══════════════════════════════════════════════════════════════════════

    void BuildWeekRow(string title, HomeData data)
    {
        SectionHeader("Week", title, Side, Side + TilesW, "Calendrier", () => CalendarClicked?.Invoke());
        SectionHeader("LastReplay", "Dernier replay", LeadersX, Right, "Replays", () => AllReplaysClicked?.Invoke());
        var tiles = D.Rect(_sections, "WeekTiles", Side, CardsY, TilesW, CardsH);

        var usedReplays = new HashSet<int>();
        if (data.agenda.Count == 0) Empty(tiles, "Rien d’autre au programme cette semaine", TilesW);
        for (int i = 0; i < data.agenda.Count && i < ODHomeMapper.MaxAgenda; i++)
        {
            var item = data.agenda[i];
            var tile = AgendaTile(tiles, item, data, i * (TileW + TileGap));
            if (item.replayIndex >= 0)
            {
                usedReplays.Add(item.replayIndex);
                int index = item.replayIndex;
                tile.onClick.AddListener(() => ReplayClicked?.Invoke(index));
            }
            else tile.onClick.AddListener(() => AgendaClicked?.Invoke());
        }

        // The most recent replay not already in the row (the row's own when there is no other).
        int last = data.replays.Count > 0 ? 0 : -1;
        for (int i = 0; i < data.replays.Count; i++)
            if (!usedReplays.Contains(i)) { last = i; break; }
        BuildLastReplay(data.replays, last);
    }

    /// <summary>Dark glass tile: date (or "Replay") and championship on top, code watermark, title and detail at the bottom.</summary>
    Button AgendaTile(Transform parent, HomeAgendaItem item, HomeData data, float x)
    {
        bool replay = item.replayIndex >= 0 && item.replayIndex < data.replays.Count;
        var rect = D.Rect(parent, replay ? "ReplayTile" : "EventTile", x, 0f, TileW, CardsH);
        var surface = rect.gameObject.AddComponent<ODHomepageSurface>();
        ODChampionshipTheme.ApplyGlass(surface);
        surface.topLeft = new Color(1f, 1f, 1f, .09f);
        surface.bottomRight = new Color(1f, 1f, 1f, .03f);
        surface.cornerRadius = D.Px(D.TileRadius);
        var button = Clickable(rect, surface);

        var face = Face(rect, item.championshipId, .05f);

        string pillText = replay ? "Replay" : item.startsAt.ToString("ddd d", French);
        var label = D.Text(face, "DateLabel", pillText, 0f, 0f, 300f, 46f, 19f, D.Weight.Bold, Color.white, caps: true, trackingEm: .06f);
        float pillW = label.GetPreferredValues(label.text).x / D.Scale + 36f;
        var pill = D.Fill(face, "Date", 28f, 26f, pillW, 46f, new Color(0f, 0f, 0f, .28f), 23f);
        label.transform.SetParent(pill.transform, false);
        D.Place(label.transform, 18f, 0f, pillW - 18f, 46f);

        D.Text(face, "Championship", item.championshipName, TileW - 28f - 200f, 26f, 200f, 46f, 18f, D.Weight.SemiBold,
            OnPoster, TextAlignmentOptions.MidlineRight);
        D.Text(face, "Title", ShortGrandPrix(item.title), 28f, 254f, TileW - 56f, 40f, 29f, D.Weight.SemiBold);

        string detail;
        if (replay)
        {
            var summary = data.replays[item.replayIndex].summary;
            detail = item.detail ?? JoinNonEmpty(" · ", ShortGrandPrix(summary.grandPrix), summary.date.ToString("ddd d MMM", French));
        }
        else detail = JoinNonEmpty(" · ", FirstPart(item.location), When(item));
        D.Text(face, "Detail", detail, 28f, 294f, TileW - 56f, 28f, 20f, D.Weight.Regular, D.Secondary);
        return button;
    }

    /// <summary>"Jour 2" while running, "10:00" or "Ven. → Dim." this week, "dans 6 jours" later.</summary>
    static string When(HomeAgendaItem item)
    {
        DateTime now = DateTime.Now, today = DateTime.Today;
        if (item.startsAt <= now) return "Jour " + ((today - item.startsAt.Date).Days + 1);
        int days = (item.startsAt.Date - today).Days;
        if (days >= 7) return "dans " + days + " jours";
        if (item.startsAt.TimeOfDay != TimeSpan.Zero) return item.startsAt.ToString("HH:mm", French);
        if (item.endsAt.Date > item.startsAt.Date)
            return Capitalise(item.startsAt.ToString("ddd", French)) + " → " + Capitalise(item.endsAt.ToString("ddd", French));
        return days == 1 ? "demain" : "";
    }

    /// <summary>Wide card: championship thumbnail, "F1 · Replay", title, length and a play button.</summary>
    void BuildLastReplay(List<HomeReplay> replays, int index)
    {
        var card = D.Rect(_sections, "LastReplay", LeadersX, CardsY, LeadersW, CardsH);
        var surface = card.gameObject.AddComponent<ODHomepageSurface>();
        ODChampionshipTheme.ApplyGlass(surface);
        if (index < 0)
        {
            Empty(card, "Aucun replay disponible pour l’instant", LeadersW);
            return;
        }
        var button = Clickable(card, surface);
        button.onClick.AddListener(() => ReplayClicked?.Invoke(index));

        var replay = replays[index];
        var summary = replay.summary;
        D.Championship(summary.championshipId, out Color colour, out Color dark);
        const float pad = 26f, thumbW = 220f, thumbH = 124f, textX = pad + thumbW + 22f;
        var thumb = D.Rect(card, "Thumbnail", pad, pad, thumbW, thumbH).gameObject.AddComponent<ODHomepageSurface>();
        thumb.topLeft = colour;
        thumb.bottomRight = dark;
        thumb.cornerRadius = D.Px(22f);
        thumb.cornerSegments = 12;
        thumb.raycastTarget = false;
        D.Text(thumb.transform, "Code", ShortCode(summary.championshipId), 0f, 0f, thumbW, thumbH, 44f, D.Weight.Bold,
            new Color(1f, 1f, 1f, .85f), TextAlignmentOptions.Center);

        D.Caps(card, "Caps", ShortCode(summary.championshipId) + " · Replay", textX, 34f, LeadersW - textX - pad, D.CapsSize);
        var title = D.Text(card, "Title", JoinNonEmpty(" — ", ShortGrandPrix(summary.grandPrix), summary.sessionName),
            textX, 62f, LeadersW - textX - pad, 76f, 29f, D.Weight.SemiBold, null, TextAlignmentOptions.TopLeft);
        title.textWrappingMode = TextWrappingModes.Normal;
        title.overflowMode = TextOverflowModes.Ellipsis;
        string length = replay.durationMinutes > 0 ? Duration(replay.durationMinutes) : null;
        D.Text(card, "Detail", JoinNonEmpty(" · ", length, summary.date.ToString("ddd d MMM", French)), textX, 140f,
            LeadersW - textX - pad, 28f, 20f, D.Weight.Regular, D.Secondary);

        const float play = 68f;
        var disc = D.Fill(card, "Play", LeadersW - pad - play, CardsH - pad - play, play, play, Color.white, play * .5f);
        disc.raycastTarget = false;
        var icon = D.Rect(disc.transform, "Icon", 25f, 23f, 22f, 22f).gameObject.AddComponent<ODNavigationIcon>();
        icon.symbol = ODNavigationIcon.Symbol.Play;
        icon.color = D.Ink;
        icon.raycastTarget = false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // À revoir (2.1c)
    // ═══════════════════════════════════════════════════════════════════════

    void BuildReplays(List<HomeReplay> replays)
    {
        SectionHeader("Replays", "À revoir", Side, Side + TilesW, "Tous les replays", () => AllReplaysClicked?.Invoke());
        var tiles = D.Rect(_sections, "ReplayTiles", Side, CardsY, TilesW, CardsH);
        if (replays.Count == 0)
        {
            Empty(tiles, "Aucun replay disponible pour l’instant", TilesW);
            return;
        }
        for (int i = 0; i < replays.Count && i < 3; i++)
        {
            int index = i;
            var tile = ReplayTile(tiles, replays[i], i * (TileW + TileGap));
            tile.onClick.AddListener(() => ReplayClicked?.Invoke(index));
        }
    }

    /// <summary>Poster in the championship colours: duration pill and championship on top, giant code watermark, title and detail at the bottom.</summary>
    Button ReplayTile(Transform parent, HomeReplay replay, float x)
    {
        var summary = replay.summary;
        D.Championship(summary.championshipId, out Color colour, out Color dark);

        var rect = D.Rect(parent, "Replay", x, 0f, TileW, CardsH);
        var surface = rect.gameObject.AddComponent<ODHomepageSurface>();
        surface.glass = true;   // glass path = rim geometry; opaque gradient, default material
        surface.topLeft = colour;
        surface.bottomRight = dark;
        surface.sheen = 0f;
        surface.shadowSize = 0f;
        surface.cornerRadius = D.Px(D.TileRadius);
        surface.rimWidth = 1.5f;
        surface.rimTop = new Color(1f, 1f, 1f, .22f);
        surface.rimBottom = new Color(1f, 1f, 1f, .04f);
        var button = Clickable(rect, surface);

        var face = Face(rect, summary.championshipId, .06f);

        float pillW = 46f;
        var pill = D.Fill(face, "Duration", 28f, 26f, pillW, 46f, new Color(0f, 0f, 0f, .28f), 23f);
        var play = D.Rect(pill.transform, "Play", 16f, 16f, 14f, 14f).gameObject.AddComponent<ODNavigationIcon>();
        play.symbol = ODNavigationIcon.Symbol.Play;
        play.color = Color.white;
        play.raycastTarget = false;
        if (replay.durationMinutes > 0)
        {
            var length = D.Text(pill.transform, "Label", Duration(replay.durationMinutes), 38f, 0f, 200f, 46f, 19f, D.Weight.SemiBold);
            pillW = 38f + length.GetPreferredValues(length.text).x / D.Scale + 18f;
            D.Place(pill.transform, 28f, 26f, pillW, 46f);
        }
        else D.Place(play.transform, (pillW - 14f) * .5f + 1f, 16f, 14f, 14f);

        D.Text(face, "Championship", summary.championshipName, TileW - 28f - 200f, 26f, 200f, 46f, 18f, D.Weight.SemiBold,
            OnPoster, TextAlignmentOptions.MidlineRight);
        D.Text(face, "Title", ShortGrandPrix(summary.grandPrix), 28f, 254f, TileW - 56f, 40f, 29f, D.Weight.SemiBold);
        D.Text(face, "Detail", summary.sessionName + " · " + summary.date.ToString("ddd d MMM", French), 28f, 294f, TileW - 56f, 28f, 20f,
            D.Weight.Regular, OnPoster);
        return button;
    }

    static string Duration(int minutes) => minutes >= 60 ? $"{minutes / 60} h {minutes % 60:00}" : $"{minutes} min";

    static string ShortGrandPrix(string name) => (name ?? "").Replace("Grand Prix", "GP").Replace("Grand prix", "GP");

    // ═══════════════════════════════════════════════════════════════════════
    // Tes championnats (2.1c)
    // ═══════════════════════════════════════════════════════════════════════

    void BuildLeaders(List<HomeLeader> leaders)
    {
        SectionHeader("Championships", "Tes championnats", LeadersX, Right, "Catalogue", () => CatalogueClicked?.Invoke());
        var card = D.Rect(_sections, "Leaders", LeadersX, CardsY, LeadersW, CardsH);
        ODChampionshipTheme.ApplyGlass(card.gameObject.AddComponent<ODHomepageSurface>());
        if (leaders.Count == 0)
        {
            Empty(card, "Aucun classement disponible", LeadersW);
            return;
        }
        const float rowH = 113.6f, pad = 28f, badge = 55f;
        for (int i = 0; i < leaders.Count && i < 3; i++)
        {
            var leader = leaders[i];
            float top = 5f + i * rowH, centre = top + rowH * .5f;
            if (i > 0) D.Line(card, pad, top, LeadersW - pad * 2f, D.DividerStrong);

            D.Championship(leader.championshipId, out Color colour, out Color dark);
            var chip = D.Rect(card, "Badge" + i, pad, centre - badge * .5f, badge, badge).gameObject.AddComponent<ODHomepageSurface>();
            chip.topLeft = colour;
            chip.bottomRight = Color.Lerp(colour, dark, .55f);
            chip.cornerRadius = D.Px(14f);
            chip.cornerSegments = 12;
            chip.raycastTarget = false;
            D.Text(chip.transform, "Code", ShortCode(leader.championshipId), 0f, 0f, badge, badge, 16f, D.Weight.Bold, Color.white,
                TextAlignmentOptions.Center);

            D.Text(card, "Name" + i, leader.name, 102f, centre - 34f, 330f, 34f, 24f, D.Weight.SemiBold);
            D.Text(card, "Detail" + i, leader.lead >= 0f ? $"Leader · +{Points(leader.lead)} pts" : "Leader", 102f, centre + 2f, 330f, 26f,
                17f, D.Weight.Regular, D.Secondary);
            D.Text(card, "Points" + i, Points(leader.points), LeadersW - pad - 140f, top, 140f, rowH, 26f, D.Weight.SemiBold,
                D.GoldText, TextAlignmentOptions.MidlineRight);
        }
    }

    static string Points(float points) => points.ToString("0.#", French);

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Section title on the left, gold text link on the right edge of the column.</summary>
    void SectionHeader(string name, string title, float left, float right, string link, Action onClick)
    {
        D.Text(_sections, name + "Title", title, left, SectionY, 700f, 52f, D.Title2, D.Weight.SemiBold);
        var measure = D.Text(_sections, "Measure", link, 0f, 0f, 600f, 48f, 22f, D.Weight.SemiBold);
        float w = measure.GetPreferredValues(link).x / D.Scale + 32f;
        Destroy(measure.gameObject);
        var button = D.Button(_sections, name + "Link", "", right + 16f - w, SectionY + 4f, w, 48f, D.ButtonKind.Ghost);
        D.Text(button.transform, "Label", link, 0f, 0f, w, 48f, 22f, D.Weight.SemiBold, D.GoldText, TextAlignmentOptions.Center);
        button.onClick.AddListener(() => onClick());
    }

    /// <summary>Whole card as a button, with the gaze ring and slight press of the other controls.</summary>
    static Button Clickable(RectTransform rect, ODHomepageSurface surface)
    {
        surface.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = surface;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        rect.gameObject.AddComponent<ODHoverState>();
        return button;
    }

    /// <summary>
    /// Clipped content layer with the giant championship code at the bottom. The watermark runs past
    /// the tile's bottom edge and is kept clear of the rounded bottom-right corner (the rect mask is square).
    /// </summary>
    static RectTransform Face(RectTransform tile, string championshipId, float watermarkAlpha)
    {
        var face = ODRuntimeUI.Stretch(tile, "Face");
        face.gameObject.AddComponent<RectMask2D>();
        var watermark = D.Text(face, "Watermark", ShortCode(championshipId), TileW - D.TileRadius - 640f, 192f, 640f, 240f, 175f, D.Weight.Bold,
            new Color(1f, 1f, 1f, watermarkAlpha), TextAlignmentOptions.TopRight, trackingEm: -.04f);
        watermark.overflowMode = TextOverflowModes.Overflow;
        return face;
    }

    static void Empty(RectTransform area, string text, float w)
    {
        if (string.IsNullOrEmpty(text)) return;
        D.Text(area, "Empty", text, 0f, 0f, w, CardsH, 24f, D.Weight.Regular, D.Tertiary, TextAlignmentOptions.Center);
    }

    /// <summary>Championship chip code of the design: F1, MGP, WEC, FE, IC, WRC.</summary>
    static string ShortCode(string championshipId)
    {
        switch (D.Normalise(championshipId))
        {
            case "f1": return "F1";
            case "motogp": return "MGP";
            case "wec": return "WEC";
            case "fe": return "FE";
            case "indycar": return "IC";
            case "wrc": return "WRC";
            default: return (championshipId ?? "").ToUpperInvariant();
        }
    }

    /// <summary>"Circuit de la Sarthe · France" → "Circuit de la Sarthe": the tiles only have room for the place.</summary>
    static string FirstPart(string location)
    {
        if (string.IsNullOrEmpty(location)) return location;
        int cut = location.IndexOf(" · ", StringComparison.Ordinal);
        return cut > 0 ? location.Substring(0, cut) : location;
    }

    static string Capitalise(string text) => string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0], French) + text.Substring(1);

    static string JoinNonEmpty(string separator, params string[] parts) =>
        string.Join(separator, Array.FindAll(parts, p => !string.IsNullOrWhiteSpace(p)));
}
