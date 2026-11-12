using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// The standings table: one row per entry, built from the row template
/// (ShowEntries). Rows are matched by driver between calls, so a position
/// change slides like an overtake. Empty until the first data arrives.
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
    public float moveDuration = 0.5f;

    private class Driver
    {
        public string code;
        public Color team;
        public RaceStandingCard card;
    }

    private readonly List<Driver> _order = new List<Driver>();

    private void Awake()
    {
        if (cardTemplate != null) cardTemplate.SetActive(false);
        if (lapText != null) lapText.text = "";
        var notice = transform.Find("DemoNotice")?.GetComponent<TextMeshProUGUI>();   // the builder's placeholder text
        if (notice != null) notice.text = "";
    }

    private float SlotY(int index) => -index * (rowHeight + rowSpacing);

    private static readonly Color FastestLap = new Color(.72f, .55f, 1f);
    private static readonly Color LastLapColor = new Color(1f, 1f, 1f, .65f);   // the row template's secondary text
    private static readonly Color GapGold = new Color(.93f, .78f, .45f);

    /// <summary>
    /// Shows one row per entry (P1 first). Rows are matched by driver between
    /// calls, so a position change slides like an overtake; rows are added from
    /// the template or hidden as needed.
    /// </summary>
    /// <param name="animate">False places every row at once (replay results): no sliding.</param>
    public void ShowEntries(IList<LiveEntry> entries, string lapLabel, string sourceNotice, bool animate = true)
    {
        if (cardTemplate == null || container == null || entries == null) return;
        cardTemplate.SetActive(false);

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

            SetTyre(d.card, entry);
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

    /// <summary>Compound badge in the Pirelli colour (S red, M yellow, H white, I green, W blue), then the tyre age.</summary>
    private static void SetTyre(RaceStandingCard card, LiveEntry entry)
    {
        string compound = entry.tyreCompound ?? "";
        var age = SetCell(card, "Tyre", compound.Length == 0 ? "--" : entry.tyreAgeLaps >= 0 ? $"{entry.tyreAgeLaps}t" : "");
        var badge = card.transform.Find("TyreBadge");
        if (badge == null)   // row not laid out as in the design: the letter stays in the text
        {
            if (age != null && compound.Length > 0) age.text = $"<color=#{ColorUtility.ToHtmlStringRGB(CompoundColor(compound))}>{compound}</color> {age.text}";
            return;
        }
        bool shown = compound.Length > 0 && (age == null || age.gameObject.activeSelf);
        badge.gameObject.SetActive(shown);
        if (!shown) return;
        Color colour = CompoundColor(compound);
        var ring = badge.GetComponent<UnityEngine.UI.Graphic>();
        if (ring != null) ring.color = colour;
        var letter = badge.Find("Letter")?.GetComponent<TextMeshProUGUI>();
        if (letter != null) { letter.text = compound; letter.color = colour; }
    }

    private static Color CompoundColor(string letter)
    {
        switch (letter)
        {
            case "S": return new Color(.93f, .16f, .16f);    // soft
            case "M": return new Color(1f, .82f, .18f);      // medium
            case "H": return new Color(.94f, .94f, .92f);    // hard
            case "I": return new Color(.26f, .69f, .16f);    // intermediate
            case "W": return new Color(.10f, .48f, .90f);    // wet
            default: return new Color(1f, 1f, 1f, .65f);
        }
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
