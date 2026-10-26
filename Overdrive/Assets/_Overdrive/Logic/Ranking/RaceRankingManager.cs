using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Drives the live standings widget: spawns 20 driver rows from a template,
/// advances the lap counter, and performs random overtakes with smooth
/// row reordering — simulating live race data.
/// Once ShowEntries() is called (backend or demo data), the simulation stops
/// for good and the rows mirror those entries instead.
/// </summary>
public class RaceRankingManager : MonoBehaviour
{
    [Header("References (wired by builder)")]
    public GameObject cardTemplate;
    public RectTransform container;
    public TextMeshProUGUI lapText;

    [Header("Layout")]
    public float rowHeight = 56f;
    public float rowSpacing = 8f;

    [Header("Simulation")]
    public int totalLaps = 54;
    public float lapDuration = 9f;
    public float overtakeMin = 2.0f;
    public float overtakeMax = 4.5f;
    public float moveDuration = 0.5f;

    // ── mock data ─────────────────────────────────────────────────────────────
    static readonly string[] Codes = {
        "ANT","JUD","VER","LAW","HAM","LEC","NOR","PIA","RUS","ALO",
        "STR","GAS","DOO","ALB","SAI","HUL","FAY","ZEN","ARC","BOT"
    };

    static readonly Color Mercedes = new Color(0.00f, 0.82f, 0.74f, 1f);
    static readonly Color Ferrari = new Color(0.90f, 0.05f, 0.05f, 1f);
    static readonly Color RedBull = new Color(0.14f, 0.25f, 0.62f, 1f);
    static readonly Color McLaren = new Color(1.00f, 0.53f, 0.00f, 1f);
    static readonly Color Williams = new Color(0.00f, 0.45f, 0.85f, 1f);
    static readonly Color Alpine = new Color(0.95f, 0.45f, 0.75f, 1f);
    static readonly Color Aston = new Color(0.00f, 0.48f, 0.42f, 1f);

    static readonly Color[] Teams = {
        Mercedes, Ferrari,  Mercedes, Williams, Mercedes,
        McLaren,  Ferrari,  McLaren,  Williams, Alpine,
        Aston,    Alpine,   RedBull,  Williams, Ferrari,
        RedBull,  Aston,    RedBull,  Mercedes, Mercedes
    };

    private class Driver
    {
        public string code;
        public Color team;
        public float gap;
        public RaceStandingCard card;
    }

    private readonly List<Driver> _order = new List<Driver>();
    private int _lap = 1;
    private bool _spawned;
    public int DriverCount => _order.Count;
    public int CurrentLap => _lap;

    // ── OnEnable so the simulation restarts every time the widget is shown ───
    private void OnEnable()
    {
        Initialize();
        if (!_spawned || _external) return;
        if (!Application.isPlaying) return;
        StartCoroutine(LapRoutine());
        StartCoroutine(OvertakeRoutine());
    }

    private void OnDisable() { StopAllCoroutines(); }

    public void Initialize()
    {
        if (!_spawned) Spawn();
        if (!_spawned || _external) return;
        RefreshAll(instant: true);
        UpdateLapLabel();
    }

    private void Spawn()
    {
        if (cardTemplate == null || container == null)
        {
            Debug.LogError("[Ranking] Missing template/container");
            return;
        }

        for (int i = 0; i < Codes.Length; i++)
        {
            var go = Instantiate(cardTemplate, container);
            go.name = "Row_" + Codes[i];
            go.SetActive(true);

            var d = new Driver
            {
                code = Codes[i],
                team = Teams[i % Teams.Length],
                gap = Random.Range(0.3f, 4.5f),
                card = go.GetComponent<RaceStandingCard>()
            };
            _order.Add(d);
            d.card.SetYInstant(SlotY(i));
        }

        cardTemplate.SetActive(false);
        _spawned = true;
    }

    // ── simulation loops ──────────────────────────────────────────────────────
    private IEnumerator LapRoutine()
    {
        UpdateLapLabel();
        while (_lap < totalLaps)
        {
            yield return new WaitForSeconds(lapDuration);
            AdvanceLap();
        }
    }

    private IEnumerator OvertakeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(overtakeMin, overtakeMax));

            SimulateOvertake();
        }
    }

    public void AdvanceLap()
    {
        _lap = Mathf.Min(_lap + 1, totalLaps);
        UpdateLapLabel();
    }

    public void SimulateOvertake()
    {
        if (_order.Count < 2) return;
        int from = Random.Range(1, _order.Count);
        int jump = Random.Range(1, Mathf.Min(3, from) + 1);
        int to = from - jump;

        var d = _order[from];
        _order.RemoveAt(from);
        _order.Insert(to, d);

        // Small gap churn so numbers feel alive.
        foreach (var drv in _order)
            drv.gap = Mathf.Max(0.05f, drv.gap + Random.Range(-0.35f, 0.35f));

        RefreshAll(instant: !Application.isPlaying);
    }

    // ── refresh ───────────────────────────────────────────────────────────────
    private void RefreshAll(bool instant)
    {
        for (int i = 0; i < _order.Count; i++)
        {
            var d = _order[i];
            string gap = i == 0 ? "Leader" : "+" + d.gap.ToString("0.000");

            d.card.Set(i + 1, d.code, d.team, gap);

            if (instant) d.card.SetYInstant(SlotY(i));
            else d.card.MoveToY(SlotY(i), moveDuration);
        }
    }

    private float SlotY(int index) => -index * (rowHeight + rowSpacing);

    private void UpdateLapLabel()
    {
        if (lapText != null) lapText.text = $"Tour {_lap}/{totalLaps}";
    }

    // ── real data (backend or demo data) ──────────────────────────────────────
    private static readonly Color FastestLap = new Color(.72f, .55f, 1f);
    private static readonly Color LastLapColor = new Color(1f, 1f, 1f, .65f);   // the row template's secondary text
    private static readonly Color GapGold = new Color(.93f, .78f, .45f);
    private bool _external;

    /// <summary>
    /// Stops the simulation and shows one row per entry (P1 first). Rows are
    /// matched by driver between calls, so a position change slides like an
    /// overtake; rows are added from the template or hidden as needed.
    /// </summary>
    /// <param name="animate">False places every row at once (replay results): no sliding.</param>
    public void ShowEntries(IList<LiveEntry> entries, string lapLabel, string sourceNotice, bool animate = true)
    {
        if (!_spawned) Spawn();
        if (!_spawned || entries == null) return;
        _external = true;
        StopAllCoroutines();

        var rows = new Driver[entries.Count];
        var used = new HashSet<Driver>();
        for (int i = 0; i < entries.Count; i++)
        {
            rows[i] = _order.Find(d => d.code == entries[i].name && !used.Contains(d));
            if (rows[i] != null) used.Add(rows[i]);
        }
        var free = new Queue<Driver>(_order.FindAll(d => !used.Contains(d)));
        var fresh = new HashSet<Driver>();
        for (int i = 0; i < entries.Count; i++)
        {
            if (rows[i] != null) continue;
            rows[i] = free.Count > 0 ? free.Dequeue() : SpawnRow();
            fresh.Add(rows[i]);
        }

        _order.Clear();
        _order.AddRange(rows);
        foreach (var spare in free)
        {
            spare.card.gameObject.SetActive(false);
            _order.Add(spare);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            LiveEntry entry = entries[i];
            Driver d = rows[i];
            d.code = entry.name;
            d.team = entry.teamColor;
            d.card.gameObject.SetActive(true);
            d.card.Set(entry.position, DisplayCode(entry), entry.teamColor, entry.gap);
            if (d.card.gapText != null) d.card.gapText.color = GapGold;

            SetCell(d.card, "Tyre", string.IsNullOrEmpty(entry.tyreCompound) ? "--"
                : entry.tyreAgeLaps >= 0 ? $"{entry.tyreCompound} {entry.tyreAgeLaps}t" : entry.tyreCompound);
            SetCell(d.card, "Stops", entry.pitStops >= 0 ? entry.pitStops.ToString() : "--");
            var lastLap = SetCell(d.card, "LastLap", string.IsNullOrEmpty(entry.lastLap) ? "--:--.---" : entry.lastLap);
            if (lastLap != null) lastLap.color = entry.isFastestLap ? FastestLap : LastLapColor;

            if (fresh.Contains(d) || !animate) d.card.SetYInstant(SlotY(i));
            else d.card.MoveToY(SlotY(i), moveDuration);
        }

        if (container != null)
            container.sizeDelta = new Vector2(container.sizeDelta.x, entries.Count * (rowHeight + rowSpacing));
        if (lapText != null) lapText.text = lapLabel ?? "";

        var hint = transform.Find("ScrollHint")?.GetComponent<TextMeshProUGUI>();
        if (hint != null) hint.text = entries.Count > 9 ? $"{entries.Count} pilotes · Faites défiler pour voir la suite" : $"{entries.Count} pilotes";
        var notice = transform.Find("DemoNotice")?.GetComponent<TextMeshProUGUI>();
        if (notice != null) notice.text = sourceNotice ?? "";
    }

    private Driver SpawnRow()
    {
        var go = Instantiate(cardTemplate, container);
        go.name = "Row_" + _order.Count;
        go.SetActive(true);
        return new Driver { card = go.GetComponent<RaceStandingCard>() };
    }

    private static string DisplayCode(LiveEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.code)) return entry.code;
        string name = entry.name ?? "";
        return name.Substring(0, Mathf.Min(3, name.Length)).ToUpperInvariant();
    }

    private static TextMeshProUGUI SetCell(RaceStandingCard card, string child, string value)
    {
        var label = card.transform.Find(child)?.GetComponent<TextMeshProUGUI>();
        if (label != null) label.text = value;
        return label;
    }
}
