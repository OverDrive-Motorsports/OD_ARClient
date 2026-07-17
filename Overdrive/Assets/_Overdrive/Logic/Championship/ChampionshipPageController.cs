/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ChampionshipPageController - Controls the floating championship page.
 ## Fully data-driven: every section is shown/built purely from which fields
 ## are present on the ChampionshipData passed to Open() — no championship-
 ## specific branching anywhere in this file.
 ##
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton controller for the championship page. Opened by clicking a
/// championship selector button in the home nav's "Championnats" tab. The
/// window is draggable via WindowHandle, attached to the same root.
///
/// Sections are built and cleared entirely at runtime (like
/// RaceRankingManager/ContentGridView elsewhere in this project) because
/// their count and content vary per championship — e.g. WEC has 6 standings
/// tables across 3 categories, MotoGP has 1 — so nothing can be pre-baked by
/// the Editor builder beyond the empty section containers.
/// </summary>
public class ChampionshipPageController : MonoBehaviour
{
    public static ChampionshipPageController Instance { get; private set; }

    [Header("References — wired by ChampionshipPageBuilder")]
    public ODCard card;

    [Header("Section containers — wired by ChampionshipPageBuilder")]
    public RectTransform liveSection;
    public RectTransform circuitWeatherSection;
    public RectTransform nextEventSection;
    public RectTransform scheduleSection;
    public RectTransform standingsSection;

    [Header("Prefab references — wired by ChampionshipPageBuilder")]
    public GameObject ghostButtonPrefab;
    public GameObject dataTablePrefab;

    private static readonly string[] FrenchDays = { "Dim", "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam" };

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        Instance = this;

        // A World Space canvas needs its worldCamera set for the
        // GraphicRaycaster to hit-test pointer/mouse clicks correctly.
        // Never bake a scene camera reference into the prefab itself.
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.worldCamera = Camera.main;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        gameObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Open / close
    // ═══════════════════════════════════════════════════════════════════════════

    public void Open(ChampionshipData data)
    {
        gameObject.SetActive(true);
        card?.SetTitle(data.name);

        ClearChildren(liveSection);
        ClearChildren(circuitWeatherSection);
        ClearChildren(nextEventSection);
        ClearChildren(scheduleSection);
        ClearChildren(standingsSection);

        bool hasLive            = data.liveGroups != null && data.liveGroups.Count > 0;
        bool hasCircuitWeather  = data.circuit.HasValue && data.weather.HasValue;
        bool hasNextEvent       = data.nextEvent.HasValue;
        bool hasSchedule        = data.schedule != null && data.schedule.Count > 0;

        SetSectionActive(liveSection, hasLive);
        SetSectionActive(circuitWeatherSection, hasCircuitWeather);
        SetSectionActive(nextEventSection, hasNextEvent);
        SetSectionActive(scheduleSection, hasSchedule);

        if (hasLive)           PopulateLive(data.liveGroups);
        if (hasCircuitWeather) PopulateCircuitWeather(data.circuit.Value, data.weather.Value);
        if (hasNextEvent)      PopulateNextEvent(data.nextEvent.Value);
        if (hasSchedule)       PopulateSchedule(data.schedule);
        if (data.standings != null) PopulateStandings(data.standings);

        PlaceInFrontOfUser();

        StopAllCoroutines();
        StartCoroutine(UITransitions.FadeScaleIn(_canvasGroup, card != null ? card.transform : null));
    }

    public void Close()
    {
        StopAllCoroutines();
        StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        yield return UITransitions.FadeScaleOut(_canvasGroup, card != null ? card.transform : null);
        gameObject.SetActive(false);
    }

    private static void SetSectionActive(RectTransform section, bool active)
    {
        if (section != null) section.gameObject.SetActive(active);
    }

    private static void ClearChildren(RectTransform rt)
    {
        if (rt == null) return;
        for (int i = rt.childCount - 1; i >= 0; i--)
            Destroy(rt.GetChild(i).gameObject);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Section builders — each is only called when its data is present
    // ═══════════════════════════════════════════════════════════════════════════

    private void PopulateLive(List<ChampionshipLiveGroup> groups)
    {
        GameObject buttonRow = NewHorizontalRow(liveSection, "ActionButtons", 16f);
        CreateButton(buttonRow.transform, "TV Live", 180f);
        CreateButton(buttonRow.transform, "Télémétrie", 180f);

        foreach (var group in groups)
        {
            if (!string.IsNullOrEmpty(group.label))
                CreateLabel(liveSection, group.label, ODLabel.TextStyle.H2);

            var columns = new List<ODTableColumn>
            {
                new ODTableColumn { columnId = "pos",  header = "POS",    flexWidth = 0.5f, bold = true, isAccent = true, alignment = TextAlignmentOptions.Center },
                new ODTableColumn { columnId = "name", header = "PILOTE", flexWidth = 1.8f, bold = true, alignment = TextAlignmentOptions.Left },
                new ODTableColumn { columnId = "team", header = "EQUIPE", flexWidth = 1.6f, alignment = TextAlignmentOptions.Left },
                new ODTableColumn { columnId = "gap",  header = "GAP",    flexWidth = 1.0f, bold = true, alignment = TextAlignmentOptions.Right },
                new ODTableColumn { columnId = "lap",  header = "TOUR",   flexWidth = 0.6f, alignment = TextAlignmentOptions.Center },
                new ODTableColumn { columnId = "tyre", header = "PNEU",   flexWidth = 0.6f, alignment = TextAlignmentOptions.Center },
            };

            var rows = new List<List<string>>();
            foreach (var entry in group.entries)
                rows.Add(new List<string> { entry.position.ToString(), entry.name, entry.teamName, entry.gap, entry.lap.ToString(), entry.tyreCompound });

            CreateDataTable(liveSection, columns, rows);
        }
    }

    private void PopulateCircuitWeather(ChampionshipCircuit circuit, ChampionshipWeather weather)
    {
        GameObject row = NewHorizontalRow(circuitWeatherSection, "WeatherMapRow", 16f);
        row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;

        ODWeatherWidget weatherWidget = BuildWeatherWidget(row.transform);
        weatherWidget.SetWeather(WeatherIcon(weather.condition), $"{weather.trackTempC}°C", $"{weather.condition} · {weather.rainChancePercent}% pluie");

        BuildMapWidget(row.transform, $"{circuit.name} · {circuit.location}");

        GameObject statsRow = NewHorizontalRow(circuitWeatherSection, "CircuitStats", 32f);
        CreateStatColumn(statsRow.transform, "LONGUEUR", $"{circuit.lengthKm:0.000} km");
        CreateStatColumn(statsRow.transform, "TOURS", circuit.totalLaps.ToString());
    }

    private void PopulateNextEvent(ChampionshipNextEvent evt)
    {
        TimeSpan span = evt.startsAt - DateTime.Now;
        if (span < TimeSpan.Zero) span = TimeSpan.Zero;

        CreateLabel(nextEventSection, $"📅 Prochain event dans {(int)span.TotalDays} jours", ODLabel.TextStyle.Caption);

        GameObject eventCard = NewCardBlock(nextEventSection, "NextEventCard");
        CreateLabel(eventCard.transform, "PROCHAIN EVENT", ODLabel.TextStyle.Caption);
        CreateLabel(eventCard.transform, evt.name, ODLabel.TextStyle.H1);

        BuildMapWidget(eventCard.transform, evt.location);

        GameObject countdownRow = NewHorizontalRow(eventCard.transform, "Countdown", 32f);
        CreateStatColumn(countdownRow.transform, "JOURS",  ((int)span.TotalDays).ToString());
        CreateStatColumn(countdownRow.transform, "HEURES", span.Hours.ToString());
        CreateStatColumn(countdownRow.transform, "MIN",    span.Minutes.ToString());
    }

    private void PopulateSchedule(List<ChampionshipSession> sessions)
    {
        bool nextFound = false;
        foreach (var session in sessions)
        {
            string statusLabel;
            if (session.status == SessionStatus.Completed)
            {
                statusLabel = "Terminé";
            }
            else if (!nextFound)
            {
                statusLabel = "PROCHAIN";
                nextFound = true;
            }
            else
            {
                statusLabel = "À venir";
            }

            CreateSessionRow(scheduleSection, session.name, FormatSessionTime(session.scheduledAt), statusLabel);
        }
    }

    /// <summary>
    /// Groups the flat standings list by category (derived from each table's
    /// label — see ChampionshipStandingTable.Category()) and builds one
    /// ODStandingsWidget per category, with drivers+teams paired into the
    /// same widget's Pilotes/Écuries toggle wherever both exist.
    /// </summary>
    private void PopulateStandings(List<ChampionshipStandingTable> tables)
    {
        var order      = new List<string>();
        var drivers    = new Dictionary<string, ChampionshipStandingTable>();
        var teams      = new Dictionary<string, ChampionshipStandingTable>();

        foreach (var table in tables)
        {
            string category = table.Category();
            if (!order.Contains(category)) order.Add(category);

            if (table.type == StandingType.Drivers) drivers[category] = table;
            else                                    teams[category]  = table;
        }

        foreach (string category in order)
        {
            ChampionshipStandingTable? d = drivers.TryGetValue(category, out var dv) ? dv : (ChampionshipStandingTable?)null;
            ChampionshipStandingTable? t = teams.TryGetValue(category, out var tv)   ? tv : (ChampionshipStandingTable?)null;

            ODStandingsWidget widget = ODStandingsWidget.Create(standingsSection, ghostButtonPrefab, dataTablePrefab);
            widget.Setup(category, d, t);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Low-level helpers (runtime-safe: no UnityEditor dependency)
    // ═══════════════════════════════════════════════════════════════════════════

    private static string WeatherIcon(string condition)
    {
        string c = condition.ToLowerInvariant();
        if (c.Contains("ensoleil")) return "☀";
        if (c.Contains("nuage"))    return "☁";
        if (c.Contains("pluie"))    return "🌧";
        return "🌡";
    }

    private static string FormatSessionTime(DateTime dt) => $"{FrenchDays[(int)dt.DayOfWeek]} {dt:HH}h{dt:mm}";

    private static GameObject NewHorizontalRow(Transform parent, string name, float spacing)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = spacing;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = false;
        go.AddComponent<LayoutElement>().preferredHeight = 56f;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
        return go;
    }

    /// <summary>A rounded card-style block (own background), used for the next-event card and the 2 widgets.</summary>
    private static GameObject NewCardBlock(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding                = new RectOffset(20, 20, 16, 16);
        vlg.spacing                = 8f;
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        go.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        GameObject bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(go.transform, false);
        bgGO.transform.SetSiblingIndex(0);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<RoundedImage>().cornerRadius = 20f;
        bgGO.AddComponent<ODBackground>().backgroundStyle = ODBackground.Style.Alt;
        bgGO.AddComponent<LayoutElement>().ignoreLayout = true;

        return go;
    }

    private static GameObject CreateLabel(Transform parent, string text, ODLabel.TextStyle style)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode       = TextOverflowModes.Ellipsis;
        ODLabel lbl = go.AddComponent<ODLabel>();
        lbl.textStyle = style;
        go.AddComponent<LayoutElement>().preferredHeight = style == ODLabel.TextStyle.H1 ? 44f : 28f;
        return go;
    }

    private static void CreateStatColumn(Transform parent, string label, string value)
    {
        GameObject col = new GameObject(label, typeof(RectTransform));
        col.transform.SetParent(parent, false);
        VerticalLayoutGroup vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment    = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.spacing           = 2f;
        col.AddComponent<LayoutElement>().preferredWidth = 80f;

        CreateLabel(col.transform, value, ODLabel.TextStyle.H1);
        CreateLabel(col.transform, label, ODLabel.TextStyle.Caption);
    }

    private void CreateSessionRow(Transform parent, string name, string time, string status)
    {
        GameObject row = NewHorizontalRow(parent, "Session_" + name, 12f);
        row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;

        GameObject nameGO = new GameObject("Name", typeof(RectTransform));
        nameGO.transform.SetParent(row.transform, false);
        TextMeshProUGUI nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
        nameTmp.text      = $"{name}\n<size=70%>{time}</size>";
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
        ODLabel nameLbl = nameGO.AddComponent<ODLabel>();
        nameLbl.textStyle = ODLabel.TextStyle.Body;
        nameGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

        GameObject badgeGO = new GameObject("StatusBadge", typeof(RectTransform));
        badgeGO.transform.SetParent(row.transform, false);
        badgeGO.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 32f);
        ODBadge badge = badgeGO.AddComponent<ODBadge>();

        GameObject badgeBgGO = new GameObject("Background", typeof(RectTransform));
        badgeBgGO.transform.SetParent(badgeGO.transform, false);
        RectTransform bbRT = badgeBgGO.GetComponent<RectTransform>();
        bbRT.anchorMin = Vector2.zero; bbRT.anchorMax = Vector2.one;
        bbRT.offsetMin = bbRT.offsetMax = Vector2.zero;
        badgeBgGO.AddComponent<RoundedImage>().cornerRadius = 24f;
        ODBackground badgeBg = badgeBgGO.AddComponent<ODBackground>();
        badgeBg.backgroundStyle = ODBackground.Style.Subtle;

        GameObject badgeLabelGO = new GameObject("Label", typeof(RectTransform));
        badgeLabelGO.transform.SetParent(badgeGO.transform, false);
        RectTransform blRT = badgeLabelGO.GetComponent<RectTransform>();
        blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one;
        blRT.offsetMin = new Vector2(8f, 0f); blRT.offsetMax = new Vector2(-8f, 0f);
        TextMeshProUGUI blTmp = badgeLabelGO.AddComponent<TextMeshProUGUI>();
        blTmp.alignment = TextAlignmentOptions.Center;
        blTmp.fontSize  = 16f;
        ODLabel blLbl = badgeLabelGO.AddComponent<ODLabel>();
        blLbl.textStyle = ODLabel.TextStyle.Caption;

        badge.background = badgeBg;
        badge.label      = blLbl;
        badge.SetVariant(status == "PROCHAIN" ? ODBadge.BadgeVariant.Gold : ODBadge.BadgeVariant.Default);
        badge.SetText(status);
    }

    private ODWeatherWidget BuildWeatherWidget(Transform parent)
    {
        GameObject root = NewCardBlock(parent, "WeatherWidget");
        LayoutElement le = root.AddComponent<LayoutElement>();
        le.flexibleWidth   = 1f;
        le.preferredHeight = 160f;

        ODWeatherWidget widget = root.AddComponent<ODWeatherWidget>();
        widget.background = root.GetComponentInChildren<ODBackground>();

        GameObject iconGO  = CreateLabel(root.transform, "", ODLabel.TextStyle.H1);
        GameObject valueGO = CreateLabel(root.transform, "", ODLabel.TextStyle.H1);
        GameObject condGO  = CreateLabel(root.transform, "", ODLabel.TextStyle.Caption);

        widget.iconLabel      = iconGO.GetComponent<ODLabel>();
        widget.valueLabel     = valueGO.GetComponent<ODLabel>();
        widget.conditionLabel = condGO.GetComponent<ODLabel>();

        return widget;
    }

    private ODMapWidget BuildMapWidget(Transform parent, string location)
    {
        GameObject root = NewCardBlock(parent, "MapWidget");
        LayoutElement le = root.AddComponent<LayoutElement>();
        le.flexibleWidth   = 1f;
        le.preferredHeight = 160f;

        ODMapWidget widget = root.AddComponent<ODMapWidget>();
        widget.background = root.GetComponentInChildren<ODBackground>();

        GameObject placeholderGO = CreateLabel(root.transform, "Carte", ODLabel.TextStyle.H2);
        GameObject locationGO    = CreateLabel(root.transform, location, ODLabel.TextStyle.Caption);

        widget.placeholderLabel = placeholderGO.GetComponent<ODLabel>();
        widget.locationLabel    = locationGO.GetComponent<ODLabel>();
        widget.SetLocation(location);

        return widget;
    }

    private void CreateDataTable(Transform parent, List<ODTableColumn> columns, List<List<string>> rows)
    {
        GameObject go = Instantiate(dataTablePrefab, parent);
        ODDataTable table = go.GetComponent<ODDataTable>();
        table.SetColumns(columns);
        table.SetData(rows);
    }

    private void CreateButton(Transform parent, string label, float width)
    {
        GameObject go = Instantiate(ghostButtonPrefab, parent);
        go.GetComponent<ODButton>().SetLabel(label);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 56f);
    }

    private void PlaceInFrontOfUser()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 fwd = cam.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        fwd.Normalize();

        transform.position = cam.transform.position + fwd * 1.6f;
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }
}
