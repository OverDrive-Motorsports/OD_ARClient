/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODPedalsWidget - Throttle and brake of the followed car (design "Pédales").
 ##   • Small: two vertical bars with their percentage.
 ##   • Large: legend with the live percentages and a note summing up the lap
 ##     ("Full throttle for 64 % of the lap · 7 braking zones") on the left;
 ##     on the right the throttle line and the braking zones over the lap.
 ##
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static ODTelemetryStyle;
using D = ODDesign;

public sealed class ODPedalsWidget : ODTelemetryWidget
{
    const float PedalEase = .05f;   // pedals move fast: little easing, just no steps
    const float FullThrottle = .98f, Braking = .08f;
    // Large traces, design px on the card.
    const float StripX = 344f, StripRight = 58f, GasY = 128f, GasH = 100f, BrakeY = 252f, BrakeH = 66f, StripPad = 12f;

    protected override string Title => "Pedals";

    LiveText _throttleText, _brakeText, _note;
    Bar _throttleBar, _brakeBar;
    Eased _throttle, _brake;
    Trace _gas;
    RectTransform _brakeStrip;
    TextMeshProUGUI _lapLength;
    readonly List<RoundedImage> _zones = new();
    float _stripW;

    // ── Small: two vertical bars ────────────────────────────────────────────

    protected override void BuildSmall(RectTransform card, float w, float h)
    {
        const float barW = 84f, gap = 60f;
        _throttleBar = Column(card, "Throttle", w * .5f - gap * .5f - barW, barW, Ink, out _throttleText);
        _brakeBar = Column(card, "Brake", w * .5f + gap * .5f, barW, InkSoft, out _brakeText);
    }

    static Bar Column(Transform card, string name, float x, float barW, Color fill, out LiveText percent)
    {
        float mid = x + barW * .5f;
        percent = Live(D.Text(card, name + "Percent", "", mid - 80f, 326f, 160f, 36f, 28f, D.Weight.SemiBold, Ink,
            TextAlignmentOptions.Center));
        D.Text(card, name + "Name", name, mid - 80f, 372f, 160f, 28f, 19f, D.Weight.Regular, Label, TextAlignmentOptions.Center);
        return new Bar(card, name, x, 94f, barW, 222f, 30f, fill, vertical: true, minPx: 8f);
    }

    // ── Large: legend, lap note, traces ─────────────────────────────────────

    protected override void BuildLarge(RectTransform card, float w, float h)
    {
        D.Fill(card, "ThrottleMark", Pad, 170f - 2.5f, 24f, 5f, Ink, 2.5f);
        D.Fill(card, "BrakeMark", Pad, 224f - 5f, 24f, 10f, InkSoft, 5f);
        _throttleText = Pair(card, "Throttle", 74f, 170f, 206f, 26f);
        _brakeText = Pair(card, "Brake", 74f, 224f, 206f, 26f);

        var note = Panel(card, "Note", Pad, 258f, 244f, 86f, 16f);
        var noteText = D.Text(note.transform, "Text", "", 20f, 0f, 204f, 86f, 18f, D.Weight.Regular, new Color(1f, 1f, 1f, .86f));
        noteText.textWrappingMode = TextWrappingModes.Normal;
        noteText.overflowMode = TextOverflowModes.Truncate;
        noteText.lineSpacing = 6f;
        _note = Live(noteText);

        _stripW = w - StripRight - StripX;
        StripLabel(card, "Gas", GasY + GasH * .5f);
        StripLabel(card, "Brake", BrakeY + BrakeH * .5f);
        _gas = new Trace(Panel(card, "GasTrace", StripX, GasY, _stripW, GasH, 16f).transform, "Line", 0f, 0f, _stripW, GasH, StripPad);
        _brakeStrip = (RectTransform)Panel(card, "BrakeTrace", StripX, BrakeY, _stripW, BrakeH, 16f).transform;

        D.Text(card, "LapStart", "Lap start", StripX, 334f, 200f, 24f, 16f, D.Weight.Regular, Faint);
        _lapLength = D.Text(card, "LapLength", "", w - StripRight - 200f, 334f, 200f, 24f, 16f, D.Weight.Regular, Faint,
            TextAlignmentOptions.MidlineRight);
    }

    static void StripLabel(Transform card, string name, float y) =>
        D.Text(card, name + "Label", name, StripX - 70f, y - 12f, 60f, 24f, 15f, D.Weight.Regular, Faint, TextAlignmentOptions.MidlineRight);

    protected override void OnTrace(LapTrace trace)
    {
        int bins = trace.Bins;
        if (_gas == null || bins == 0) return;
        if (_lapLength.text.Length == 0) _lapLength.text = Km(trace.lapLengthKm);

        _gas.Plot(trace.throttle, 0f, 1f);

        // Braking zones as pills, as wide as the stretch of track spent on the brake.
        int zones = 0, start = -1;
        for (int i = 0; i <= bins; i++)
        {
            bool on = i < bins && trace.brake[i] > Braking;
            if (on && start < 0) start = i;
            if (!on && start >= 0)
            {
                Zone(zones++, start, i, bins);
                start = -1;
            }
        }
        for (int i = zones; i < _zones.Count; i++) _zones[i].gameObject.SetActive(false);
        if (zones > 1 && trace.brake[0] > Braking && trace.brake[bins - 1] > Braking) zones--;   // one zone across the line

        int full = 0;
        foreach (float t in trace.throttle) if (t >= FullThrottle) full++;
        int share = Mathf.RoundToInt(full * 100f / bins);
        if (_note.Changed(share * 1000 + zones))
            _note.Text = $"Full throttle for {share} % of the lap · {zones} braking zone{(zones == 1 ? "" : "s")}";
    }

    void Zone(int index, int from, int to, int bins)
    {
        const float pad = 10f, minW = 14f;
        while (_zones.Count <= index)
            _zones.Add(D.Fill(_brakeStrip, "Zone", 0f, 0f, minW, BrakeH - pad * 2f, InkSoft, 7f));
        var zone = _zones[index];
        zone.gameObject.SetActive(true);
        float inner = _stripW - StripPad * 2f;
        float x = StripPad + (float)from / bins * inner, width = Mathf.Max(minW, (float)(to - from) / bins * inner);
        D.Place(zone.transform, Mathf.Min(x, _stripW - StripPad - width), pad, width, BrakeH - pad * 2f);
    }

    // ── Live values ─────────────────────────────────────────────────────────

    protected override void OnCar(in CarTelemetry car, bool first)
    {
        _throttle.target = car.throttle;
        _brake.target = car.brake;
        if (!first) return;
        _throttle.Snap();
        _brake.Snap();
        Paint();
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
