/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODSpeedWidget - Speed of the followed car (design "Vitesse").
 ##   • Small: the speed in big light digits, "km/h", and a bar placing it
 ##     between the lowest and the top speed of the lap ("Min. 96 · Top 312").
 ##   • Large: the speed, "km/h · Sector 2", top and average of the lap on the
 ##     left; on the right the speed trace of the lap (100 / 200 / 300 grid,
 ##     dashed sector ends, S1 S2 S3) with a cursor and a dot on the car.
 ##
 */

using TMPro;
using UnityEngine;
using static ODTelemetryStyle;
using D = ODDesign;

public sealed class ODSpeedWidget : ODTelemetryWidget
{
    const float SpeedEase = .12f, LapEase = .1f;
    // Large chart, design px on the card, and the speeds at its bottom and top.
    const float PlotX = 364f, PlotY = 150f, PlotRight = 58f, PlotH = 170f, VMin = 70f, VMax = 324f;

    protected override string Title => "Speed";

    LiveText _speed, _min, _top, _average;
    TextMeshProUGUI _detail;
    Bar _range;
    Trace _trace;
    RectTransform _cursor, _dot;
    Eased _kmh, _lap;
    float _lapMin, _lapTop = 1f, _plotW;
    bool _sectorsBuilt;
    string _location;

    protected override void BuildSmall(RectTransform card, float w, float h)
    {
        _speed = Readout(card, 0f, 113f, w, 170f, 150f, TextAlignmentOptions.Center);
        D.Text(card, "Unit", "km/h", 0f, 262f, w, 36f, 22f, D.Weight.Regular, Label, TextAlignmentOptions.Center);
        _range = new Bar(card, "Range", Pad, 326f, w - Pad * 2f, 12f, 6f, Ink, vertical: false, minPx: 12f);
        _min = Live(D.Text(card, "Min", "", Pad, 348f, w * .5f - Pad, 28f, 18f, D.Weight.Regular, Label));
        _top = Live(D.Text(card, "Top", "", w * .5f, 348f, w * .5f - Pad, 28f, 18f, D.Weight.Regular, Label,
            TextAlignmentOptions.MidlineRight));
    }

    protected override void BuildLarge(RectTransform card, float w, float h)
    {
        _speed = Readout(card, Pad, 116f, 300f, 160f, 128f);
        _detail = D.Text(card, "Detail", "km/h", Pad, 250f, 300f, 32f, 20f, D.Weight.Regular, Label);
        _top = Stat(card, "Top", Pad, 296f, 90f);
        _average = Stat(card, "Average", 126f, 296f, 140f);

        float plotW = _plotW = w - PlotRight - PlotX;
        for (int v = 100; v <= 300; v += 100)
        {
            float y = PlotY + (1f - (v - VMin) / (VMax - VMin)) * PlotH;
            D.Fill(card, "Grid" + v, PlotX, y - .75f, plotW, 1.5f, GridLine, 0f);
            D.Text(card, "Axis" + v, v.ToString(), PlotX - 66f, y - 12f, 50f, 24f, 16f, D.Weight.Regular, Faint,
                TextAlignmentOptions.MidlineRight);
        }
        _trace = new Trace(card, "Plot", PlotX, PlotY, plotW, PlotH);
        _cursor = D.Fill(_trace.rect, "Cursor", 0f, 0f, 2f, PlotH, Faint, 0f).rectTransform;
        _dot = D.Fill(_trace.rect, "Dot", 0f, 0f, 16f, 16f, Ink, 8f).rectTransform;
        _dot.pivot = new Vector2(.5f, .5f);
    }

    /// <summary>Dashed lines at the sector ends and S1 S2 S3 under the chart (once: the track doesn't change).</summary>
    void BuildSectors(float[] ends)
    {
        _sectorsBuilt = true;
        var dash = new Color(1f, 1f, 1f, .28f);
        float plotW = _plotW, start = 0f;
        for (int i = 0; i <= ends.Length; i++)
        {
            float end = i < ends.Length ? ends[i] : 1f;
            if (i < ends.Length)
                for (float y = 0f; y < PlotH; y += 14f)
                    D.Fill(_trace.rect, "Dash", end * plotW - 1f, y, 2f, Mathf.Min(8f, PlotH - y), dash, 0f);
            D.Text(_trace.rect.parent, "S" + (i + 1), "S" + (i + 1), PlotX + (start + end) * .5f * plotW - 40f, 336f, 80f, 28f, 16f,
                D.Weight.SemiBold, Faint, TextAlignmentOptions.Center);
            start = end;
        }
    }

    protected override void OnTrace(LapTrace trace)
    {
        int bins = trace.Bins;
        if (bins == 0) return;

        float min = float.MaxValue, top = 0f, sum = 0f;
        foreach (float v in trace.speed)
        {
            min = Mathf.Min(min, v);
            top = Mathf.Max(top, v);
            sum += v;
        }
        _lapMin = min;
        _lapTop = Mathf.Max(min + 1f, top);
        int topKmh = Mathf.RoundToInt(top);

        if (_trace == null)
        {
            int minKmh = Mathf.RoundToInt(min);
            if (_min.Changed(minKmh)) _min.Text = "Min. " + minKmh;
            if (_top.Changed(topKmh)) _top.Text = "Top " + topKmh;
            return;
        }

        _top.Number(topKmh);
        _average.Number(Mathf.RoundToInt(sum / bins));
        if (!_sectorsBuilt) BuildSectors(trace.sectorEnds);
        _trace.Plot(trace.speed, VMin, VMax);
    }

    protected override void OnCar(in CarTelemetry car, bool first)
    {
        _kmh.target = car.speedKmh;
        _lap.target = car.lapDistance;
        if (_detail != null && car.location != _location)
        {
            _location = car.location;
            _detail.text = string.IsNullOrEmpty(_location) ? "km/h" : "km/h · " + _location;
        }
        if (!first) return;
        _kmh.Snap();
        _lap.Snap();
        Paint();
    }

    protected override void Tick(float dt)
    {
        _kmh.Step(SpeedEase, dt);
        _lap.StepLap(LapEase, dt);
        Paint();
    }

    void Paint()
    {
        _speed.Number(Mathf.Max(0, Mathf.RoundToInt(_kmh.value)));
        if (_range != null)
        {
            _range.Set((_kmh.value - _lapMin) / (_lapTop - _lapMin));
            return;
        }
        float x = _trace.X(_lap.value);
        _cursor.anchoredPosition = new Vector2(x - D.Px(1f), 0f);
        _dot.anchoredPosition = new Vector2(x, _trace.Y(_kmh.value));
    }
}
