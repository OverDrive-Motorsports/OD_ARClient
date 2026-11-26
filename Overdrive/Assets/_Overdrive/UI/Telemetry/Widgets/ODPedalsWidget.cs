/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODPedalsWidget - Throttle and brake of the followed car (design "Pédales").
 ## Throttle is blue, brake red, everywhere in the widget.
 ##   • Small: two vertical bars with their percentage.
 ##   • Large: legend with the live percentages and a note summing up the lap
 ##     ("Full throttle for 64 % of the lap · 7 braking zones") on the left;
 ##     on the right one chart with both lines over the lap, throttle and
 ##     brake (0 – 100 %). It draws the running lap up to the car and starts
 ##     again empty at each line crossing.
 ##
 */

using TMPro;
using UnityEngine;
using static ODTelemetryStyle;
using D = ODDesign;

public sealed class ODPedalsWidget : ODTelemetryWidget
{
    const float PedalEase = .05f;   // pedals move fast: little easing, just no steps
    const float FullThrottle = .98f, Braking = .08f;
    // Large chart, design px on the card.
    const float ChartX = 344f, ChartRight = 58f, ChartY = 128f, ChartH = 190f, ChartPad = 14f;

    protected override string Title => "Pedals";

    LiveText _throttleText, _brakeText, _note;
    Bar _throttleBar, _brakeBar;
    Eased _throttle, _brake;
    Trace _gas, _brakeLine;
    TextMeshProUGUI _lapLength;
    LapTrace _trace;

    // ── Small: two vertical bars ────────────────────────────────────────────

    protected override void BuildSmall(RectTransform card, float w, float h)
    {
        const float barW = 84f, gap = 60f;
        _throttleBar = Column(card, "Throttle", w * .5f - gap * .5f - barW, barW, ThrottleColour, out _throttleText);
        _brakeBar = Column(card, "Brake", w * .5f + gap * .5f, barW, BrakeColour, out _brakeText);
    }

    static Bar Column(Transform card, string name, float x, float barW, Color fill, out LiveText percent)
    {
        float mid = x + barW * .5f;
        percent = Live(D.Text(card, name + "Percent", "", mid - 80f, 326f, 160f, 36f, 28f, D.Weight.SemiBold, Ink,
            TextAlignmentOptions.Center));
        D.Text(card, name + "Name", name, mid - 80f, 372f, 160f, 28f, 19f, D.Weight.Regular, fill, TextAlignmentOptions.Center);
        return new Bar(card, name, x, 94f, barW, 222f, 30f, fill, vertical: true, minPx: 8f);
    }

    // ── Large: legend, lap note, chart ──────────────────────────────────────

    protected override void BuildLarge(RectTransform card, float w, float h)
    {
        D.Fill(card, "ThrottleMark", Pad, 170f - 2.5f, 24f, 5f, ThrottleColour, 2.5f);
        D.Fill(card, "BrakeMark", Pad, 224f - 2.5f, 24f, 5f, BrakeColour, 2.5f);
        _throttleText = Pair(card, "Throttle", 74f, 170f, 206f, 26f);
        _brakeText = Pair(card, "Brake", 74f, 224f, 206f, 26f);

        var note = Panel(card, "Note", Pad, 258f, 244f, 86f, 16f);
        var noteText = D.Text(note.transform, "Text", "", 20f, 0f, 204f, 86f, 18f, D.Weight.Regular, new Color(1f, 1f, 1f, .86f));
        noteText.textWrappingMode = TextWrappingModes.Normal;
        noteText.overflowMode = TextOverflowModes.Truncate;
        noteText.lineSpacing = 6f;
        _note = Live(noteText);

        // One chart, both pedals on the same 0 – 100 % scale; the brake drawn last, over the throttle.
        float chartW = w - ChartRight - ChartX;
        var chart = Panel(card, "Chart", ChartX, ChartY, chartW, ChartH, 16f).transform;
        float inner = ChartH - ChartPad * 2f;
        for (int p = 0; p <= 100; p += 50)
        {
            float y = ChartY + ChartPad + (1f - p / 100f) * inner;
            if (p == 50) D.Fill(card, "Grid50", ChartX + ChartPad, y - .75f, chartW - ChartPad * 2f, 1.5f, GridLine, 0f);
            D.Text(card, "Axis" + p, p + " %", ChartX - 76f, y - 12f, 64f, 24f, 15f, D.Weight.Regular, Faint,
                TextAlignmentOptions.MidlineRight);
        }
        _gas = new Trace(chart, "Throttle", 0f, 0f, chartW, ChartH, ChartPad, ThrottleColour);
        _brakeLine = new Trace(chart, "Brake", 0f, 0f, chartW, ChartH, ChartPad, BrakeColour);

        D.Text(card, "LapStart", "Lap start", ChartX, 334f, 200f, 24f, 16f, D.Weight.Regular, Faint);
        _lapLength = D.Text(card, "LapLength", "", w - ChartRight - 200f, 334f, 200f, 24f, 16f, D.Weight.Regular, Faint,
            TextAlignmentOptions.MidlineRight);
    }

    protected override void OnTrace(LapTrace trace)
    {
        int bins = trace.Bins;
        if (_gas == null || bins == 0) return;
        _trace = trace;   // plotted in OnCar, once the car's place on the lap is known
        if (_lapLength.text.Length == 0) _lapLength.text = Km(trace.lapLengthKm);

        // Braking zones: stretches of track on the brake (one across the line counts once).
        int zones = 0;
        for (int i = 0; i < bins; i++)
            if (trace.brake[i] > Braking && trace.brake[(i + bins - 1) % bins] <= Braking) zones++;
        if (zones == 0 && trace.brake[0] > Braking) zones = 1;

        int full = 0;
        foreach (float t in trace.throttle) if (t >= FullThrottle) full++;
        int share = Mathf.RoundToInt(full * 100f / bins);
        if (_note.Changed(share * 1000 + zones))
            _note.Text = $"Full throttle for {share} % of the lap · {zones} braking zone{(zones == 1 ? "" : "s")}";
    }

    // ── Live values ─────────────────────────────────────────────────────────

    protected override void OnCar(in CarTelemetry car, bool first)
    {
        _throttle.target = car.throttle;
        _brake.target = car.brake;
        PlotLap(car.lapDistance);
        if (!first) return;
        _throttle.Snap();
        _brake.Snap();
        Paint();
    }

    /// <summary>Both lines from the line to the car: empty again once it crosses the line.</summary>
    void PlotLap(float lapShare)
    {
        if (_gas == null || _trace == null || _trace.Bins == 0) return;
        int bins = _trace.Bins;
        int driven = Mathf.Clamp(Mathf.FloorToInt(lapShare * bins + .5f), 0, bins);
        _gas.Plot(_trace.throttle, 0f, 1f, driven);
        _brakeLine.Plot(_trace.brake, 0f, 1f, driven);
    }

    protected override void Tick(float dt)
    {
        _throttle.Step(PedalEase, dt);
        _brake.Step(PedalEase, dt);
        Paint();
    }

    void Paint()
    {
        _throttleBar?.Set(_throttle.value);
        _brakeBar?.Set(_brake.value);
        _throttleText.Number(Percent(_throttle.value), " %");
        _brakeText.Number(Percent(_brake.value), " %");
    }

    static int Percent(float value) => Mathf.Clamp(Mathf.RoundToInt(value * 100f), 0, 100);
}
