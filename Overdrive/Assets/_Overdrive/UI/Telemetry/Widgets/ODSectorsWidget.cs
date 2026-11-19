/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODSectorsWidget - Sector times of the followed car on the running lap
 ## (design "Secteurs").
 ##   • Small: S1 S2 S3 rows — the time once the sector is done, "in
 ##     progress" for the one being driven, last lap's time (dimmed) for the
 ##     ones ahead; a done sector glows purple (session best) or green
 ##     (personal best). Footnote explaining purple.
 ##   • Large: the same rows, narrower, and the 24 mini-sectors of the lap for
 ##     the followed car and the compared one ("VER / HAM" in the header),
 ##     with their legend.
 ##
 */

using TMPro;
using UnityEngine;
using static ODTelemetryStyle;
using D = ODDesign;

public sealed class ODSectorsWidget : ODTelemetryWidget
{
    // Mini-sector grid of the large format, design px on the card.
    const float GridX = 372f, GridRight = 36f, CellW = 15f, CellH = 34f, CodeX = 310f;

    protected override string Title => "Sectors";

    protected override bool ComparesRival => Format == WidgetFormat.Large;

    sealed class Row
    {
        public ODHomepageSurface panel;
        public LiveText value;
        public float timePx, runningPx;
    }

    readonly Row[] _rows = new Row[LapTiming.Sectors];
    LiveText _gridTitle;
    TextMeshProUGUI _youCode, _rivalCode;
    RoundedImage[] _cells, _rivalCells;

    protected override void BuildSmall(RectTransform card, float w, float h)
    {
        BuildRows(card, w - Pad * 2f, 76f, 100f, small: true);
        D.Text(card, "Footnote", "Purple: best time of the race", Pad, 376f, w - Pad * 2f, 26f, 18f, D.Weight.Regular, Label);
    }

    protected override void BuildLarge(RectTransform card, float w, float h)
    {
        BuildRows(card, 244f, 62f, 140f, small: false);

        _gridTitle = Live(D.Caps(card, "MiniTitle", "Mini-sectors", CodeX, 160f, w - CodeX - GridRight, 16f, Label));
        _cells = Cells(card, "You", 222f, w, out _youCode);
        _rivalCells = Cells(card, "Rival", 274f, w, out _rivalCode);

        float x = CodeX;
        x = LegendItem(card, x, SplitStatus.SessionBest, "Session best", 104f);
        x = LegendItem(card, x, SplitStatus.PersonalBest, "Personal best", 112f);
        x = LegendItem(card, x, SplitStatus.Slower, "Slower", 56f);
        LegendItem(card, x, SplitStatus.Upcoming, "Upcoming", 80f);
    }

    void BuildRows(Transform card, float w, float h, float top, bool small)
    {
        float pad = small ? 24f : 20f;
        for (int s = 0; s < LapTiming.Sectors; s++)
        {
            var row = _rows[s] = new Row
            {
                panel = Panel(card, "S" + (s + 1), Pad, top + s * (h + 16f), w, h, small ? 20f : 18f),
                timePx = small ? 30f : 26f,
                runningPx = small ? 24f : 21f,
            };
            var t = row.panel.transform;
            D.Text(t, "Name", "S" + (s + 1), pad, 0f, 60f, h, small ? 24f : 22f, D.Weight.SemiBold, Ink);
            row.value = Live(D.Text(t, "Value", "", 70f, 0f, w - 70f - pad, h, row.timePx, D.Weight.SemiBold, Ink,
                TextAlignmentOptions.MidlineRight));
        }
    }

    /// <summary>A driver code and its 24 cells, centred on <paramref name="y"/>; the last cell ends at the right margin.</summary>
    static RoundedImage[] Cells(Transform card, string name, float y, float w, out TextMeshProUGUI code)
    {
        code = D.Text(card, name + "Code", "", CodeX, y - 14f, 60f, 28f, 18f, D.Weight.Bold, Ink);
        float pitch = (w - GridRight - GridX - CellW) / (LapTiming.MiniSectors - 1);
        var cells = new RoundedImage[LapTiming.MiniSectors];
        for (int i = 0; i < cells.Length; i++)
            cells[i] = D.Fill(card, name + "Mini" + i, GridX + i * pitch, y - CellH * .5f, CellW, CellH, Ahead, 5f);
        return cells;
    }

    static float LegendItem(Transform card, float x, SplitStatus status, string text, float textW)
    {
        D.Fill(card, "Legend" + text, x, 318f - 7f, 14f, 14f, Of(status), 4f);
        D.Text(card, "LegendText" + text, text, x + 22f, 318f - 12f, textW, 24f, 16f, D.Weight.Regular, Label);
        return x + 22f + textW + 24f;
    }

    // ── Data ────────────────────────────────────────────────────────────────

    protected override void OnTiming(LapTiming timing, LapTiming rival)
    {
        for (int s = 0; s < LapTiming.Sectors; s++) Paint(_rows[s], timing, s);
        if (_cells == null) return;

        if (_gridTitle.Changed(timing.lap)) _gridTitle.Text = "Mini-sectors · Lap " + timing.lap;
        ShowCode(_youCode, timing);
        ShowCode(_rivalCode, rival);
        Paint(_cells, timing.miniSectors);
        Paint(_rivalCells, rival.miniSectors);
    }

    static void Paint(Row row, LapTiming timing, int s)
    {
        var status = timing.sectorStatus[s];
        float time = timing.sectorTimes[s], last = timing.lastSectorTimes[s];
        bool done = time >= 0f && status != SplitStatus.Running && status != SplitStatus.Upcoming;

        // One key per look: the status and the time shown, in milliseconds.
        int ms = status == SplitStatus.Running ? 0 : Mathf.Clamp(Mathf.RoundToInt((done ? time : last) * 1000f), 0, 999999);
        if (!row.value.Changed((int)status * 1000000 + ms)) return;

        Color glow = !done ? Color.clear : status == SplitStatus.SessionBest ? D.Purple
                   : status == SplitStatus.PersonalBest ? D.Green : Color.clear;
        Tint(row.panel, glow.a > 0f ? new Color(glow.r, glow.g, glow.b, .26f) : Well, glow);

        var label = row.value.label;
        if (status == SplitStatus.Running)
        {
            D.Style(label, row.runningPx, D.Weight.Regular, Label, TextAlignmentOptions.MidlineRight);
            label.text = "in progress";
            return;
        }
        D.Style(label, row.timePx, D.Weight.SemiBold, done ? Ink : Faint, TextAlignmentOptions.MidlineRight);
        label.text = done || last >= 0f ? Digits(LapClock(done ? time : last)) : "—";
    }

    static void Paint(RoundedImage[] cells, SplitStatus[] statuses)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            var colour = Of(statuses[i]);
            if (cells[i].color != colour) cells[i].color = colour;
        }
    }
}
