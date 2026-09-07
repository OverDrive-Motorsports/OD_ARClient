using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Drives the live standings widget: spawns 20 driver rows from a template,
/// advances the lap counter, and performs random overtakes with smooth
/// row reordering — simulating live race data.
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

    // ── OnEnable so the simulation restarts every time the widget is shown ───
    private void OnEnable()
    {
        if (!_spawned) Spawn();
        if (!_spawned) return;

        RefreshAll(instant: true);
        StartCoroutine(LapRoutine());
        StartCoroutine(OvertakeRoutine());
    }

    private void OnDisable() { StopAllCoroutines(); }

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
            _lap++;
            UpdateLapLabel();
        }
    }

    private IEnumerator OvertakeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(overtakeMin, overtakeMax));

            int from = Random.Range(1, _order.Count);
            int jump = Random.Range(1, Mathf.Min(3, from) + 1);
            int to = from - jump;

            var d = _order[from];
            _order.RemoveAt(from);
            _order.Insert(to, d);

            // small gap churn so numbers feel alive
            foreach (var drv in _order)
                drv.gap = Mathf.Max(0.05f,
                    drv.gap + Random.Range(-0.35f, 0.35f));

            RefreshAll(instant: false);
        }
    }

    // ── refresh ───────────────────────────────────────────────────────────────
    private void RefreshAll(bool instant)
    {
        for (int i = 0; i < _order.Count; i++)
        {
            var d = _order[i];
            string gap = i == 0 ? "Leader"
                       : (Random.value > 0.25f ? "+" : "-") + d.gap.ToString("0.000");

            d.card.Set(i + 1, d.code, d.team, gap);

            if (instant) d.card.SetYInstant(SlotY(i));
            else d.card.MoveToY(SlotY(i), moveDuration);
        }
    }

    private float SlotY(int index) => -index * (rowHeight + rowSpacing);

    private void UpdateLapLabel()
    {
        if (lapText != null) lapText.text = $"LAP {_lap}/{totalLaps}";
    }
}
