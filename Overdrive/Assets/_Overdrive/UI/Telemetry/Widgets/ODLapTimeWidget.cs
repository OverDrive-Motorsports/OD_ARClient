/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODLapTimeWidget - Lap timer of the followed car (design "Chrono").
 ##   • Small: running lap time in big light digits, "Lap 32 in progress",
 ##     then the best lap and the live delta to it.
 ##   • Large: the same on the left (plus the share of the lap driven, best
 ##     and delta side by side), and the last 5 laps on the right with their
 ##     gap to the best lap — the best one in purple.
 ## The clock runs every frame from the last sample, so it never stutters at
 ## the feed's 10 Hz.
 ##
 */

using TMPro;
using UnityEngine;
using static ODTelemetryStyle;
using D = ODDesign;

public sealed class ODLapTimeWidget : ODTelemetryWidget
{
    const int ShownLaps = 5;
    // Last-laps table of the large format, design px on the card.
    const float TableX = 394f, TableY = 120f, TableRight = 36f, TableH = 268f, TablePad = 22f;

    protected override string Title => "Lap time";

    LiveText _clock, _status, _best, _delta;
    TextMeshProUGUI[] _lapNames, _lapTimes, _lapGaps;   // large only
    int _lap = int.MinValue, _tableKey = int.MinValue;
    float _base, _since, _shown;

    protected override void BuildSmall(RectTransform card, float w, float h)
    {
        _clock = Readout(card, 40f, 116f, w - 60f, 120f, 88f);
        _status = Live(D.Text(card, "Status", "", 40f, 214f, w - 76f, 32f, 20f, D.Weight.Regular, Label));
        D.Line(card, Pad, 280f, w - Pad * 2f, D.Divider);
        var name = new Color(1f, 1f, 1f, .75f);
        _best = Pair(card, "Best", Pad, 316f, w - Pad * 2f, 24f, name);
        _delta = Pair(card, "Delta", Pad, 358f, w - Pad * 2f, 24f, name);
    }

    protected override void BuildLarge(RectTransform card, float w, float h)
    {
        _clock = Readout(card, 40f, 134f, TableX - 48f, 120f, 84f);
        _status = Live(D.Text(card, "Status", "", 40f, 234f, TableX - 60f, 32f, 20f, D.Weight.Regular, Label));
        _best = Stat(card, "Best", 40f, 285f, 150f);
        _delta = Stat(card, "Delta", 196f, 285f, 150f);

        float tableW = w - TableRight - TableX, rowH = TableH / ShownLaps;
        var table = Panel(card, "LastLaps", TableX, TableY, tableW, TableH, 16f).transform;
        _lapNames = new TextMeshProUGUI[ShownLaps];
        _lapTimes = new TextMeshProUGUI[ShownLaps];
        _lapGaps = new TextMeshProUGUI[ShownLaps];
        for (int i = 0; i < ShownLaps; i++)
        {
            float y = i * rowH;
            if (i > 0) D.Line(table, TablePad, y, tableW - TablePad * 2f, D.Divider);
            _lapNames[i] = D.Text(table, "Lap" + i, "", TablePad, y, 80f, rowH, 18f, D.Weight.Regular, Label);
            _lapTimes[i] = D.Text(table, "Time" + i, "", tableW * .5f - 100f, y, 200f, rowH, 20f, D.Weight.SemiBold, Ink,
                TextAlignmentOptions.Center);
            _lapGaps[i] = D.Text(table, "Gap" + i, "", tableW - TablePad - 140f, y, 140f, rowH, 20f, D.Weight.Regular, Label,
                TextAlignmentOptions.MidlineRight);
        }
    }

    // ── Data ────────────────────────────────────────────────────────────────

    protected override void OnTiming(LapTiming timing, LapTiming rival)
    {
        if (timing.lap != _lap || timing.lapTime < _base - .5f)
        {
            _lap = timing.lap;
            _shown = 0f;   // new lap, or playback moved back: the clock starts again from the sample
        }
        _base = timing.lapTime;
        _since = 0f;

        bool large = _lapTimes != null;
        int share = Mathf.RoundToInt(timing.lapShare * 100f);
        if (_status.Changed(timing.lap * 1000 + (large ? share : 0)))
            _status.Text = large ? $"Lap {timing.lap} in progress · {share} % of the lap" : $"Lap {timing.lap} in progress";

        bool hasBest = timing.bestLap > 0f;
        if (_best.Changed(Mathf.RoundToInt(timing.bestLap * 1000f))) _best.Text = hasBest ? Digits(LapClock(timing.bestLap)) : "—";
        if (_delta.Changed(Mathf.RoundToInt(timing.delta * 1000f))) _delta.Text = hasBest ? Digits(Diff(timing.delta)) : "—";

        if (large) PaintTable(timing);
    }

    /// <summary>Last laps, oldest at the top as in the design; the best lap in purple.</summary>
    void PaintTable(LapTiming timing)
    {
        var laps = timing.lastLaps;
        int key = laps.Count == 0 ? 0 : laps[laps.Count - 1].lap * 10 + laps.Count;
        if (key == _tableKey) return;
        _tableKey = key;

        int start = laps.Count - ShownLaps;
        for (int i = 0; i < ShownLaps; i++)
        {
            int index = start + i;
            if (index < 0 || index >= laps.Count)
            {
                _lapNames[i].text = _lapTimes[i].text = _lapGaps[i].text = "";
                continue;
            }
            var lap = laps[index];
            bool best = Mathf.Abs(lap.time - timing.bestLap) < .0005f;
            _lapNames[i].text = "L" + lap.lap;
            _lapTimes[i].text = Digits(LapClock(lap.time));
            _lapTimes[i].color = best ? D.PurpleText : Ink;
            _lapGaps[i].text = best ? "best" : Digits(Diff(lap.time - timing.bestLap));
        }
    }

    protected override void OnRestart()
    {
        // Another car: its clock, laps and table, not the previous car's.
        _lap = _tableKey = int.MinValue;
        _base = _since = _shown = 0f;
    }

    protected override void Tick(float dt)
    {
        // Runs on from the last sample; never backwards within a lap unless playback moved back (OnTiming).
        _since += dt;
        _shown = Mathf.Max(_shown, _base + _since);
        if (_clock.Changed(Mathf.FloorToInt(_shown * 1000f))) _clock.Text = Digits(LapClock(_shown, alwaysMinutes: true));
    }
}
