/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTelemetryWidget.Parts - The live pieces every telemetry widget is made
 ## of. Each one only touches the UI when what it shows changes, so values
 ## move smoothly without flicker or layout shift:
 ##   • LiveText — a label rewritten only when its key (the shown value)
 ##     changes, so strings are built only then;
 ##   • Eased — a value easing toward its last sample, frame-rate independent;
 ##   • Bar — gauge: recessed track and a fill whose length follows 0 – 1;
 ##   • Trace — a line over one lap (speed, throttle) in a box, with the
 ##     mapping from lap share / value to its local position for cursors.
 ##
 */

using TMPro;
using UnityEngine;
using static ODTelemetryStyle;
using D = ODDesign;

public abstract partial class ODTelemetryWidget
{
    /// <summary>Label rewritten only when the shown value changes.</summary>
    protected sealed class LiveText
    {
        public readonly TextMeshProUGUI label;
        int _key = int.MinValue;

        public LiveText(TextMeshProUGUI label) => this.label = label;

        /// <summary>True (once) when <paramref name="key"/> differs from the last one: then write the text.</summary>
        public bool Changed(int key)
        {
            if (key == _key) return false;
            _key = key;
            return true;
        }

        /// <summary>Monospaced integer, rewritten only when it changes.</summary>
        public void Number(int value, string suffix = "")
        {
            if (Changed(value)) label.text = Digits(value) + suffix;
        }

        public string Text { set => label.text = value; }
        public Color Color { set => label.color = value; }
    }

    /// <summary>Shown value easing toward the last sample.</summary>
    protected struct Eased
    {
        public float target, value;

        /// <summary>Exponential easing with a time constant of <paramref name="seconds"/>.</summary>
        public float Step(float seconds, float dt) =>
            value = Mathf.Lerp(value, target, 1f - Mathf.Exp(-dt / Mathf.Max(.0001f, seconds)));

        /// <summary>For a lap share (0 – 1): snaps instead of running back when the car crosses the line.</summary>
        public float StepLap(float seconds, float dt) => target < value - .5f ? value = target : Step(seconds, dt);

        public float Snap() => value = target;
    }

    /// <summary>Gauge bar: a recessed rounded track and a fill whose length follows the value (0 – 1).</summary>
    protected sealed class Bar
    {
        readonly RectTransform _fill;
        readonly float _length, _thickness, _min;
        readonly bool _vertical;
        float _shown = -1f;

        public Bar(Transform parent, string name, float x, float y, float w, float h, float radius, Color fill, bool vertical, float minPx)
        {
            _vertical = vertical;
            var track = D.Fill(parent, name, x, y, w, h, Well, radius);
            _fill = D.Fill(track.transform, "Fill", 0f, 0f, w, h, fill, radius).rectTransform;
            // Grows from the left, or from the bottom when vertical.
            _fill.anchorMin = _fill.anchorMax = _fill.pivot = vertical ? Vector2.zero : new Vector2(0f, 1f);
            _fill.anchoredPosition = Vector2.zero;
            _length = D.Px(vertical ? h : w);
            _thickness = D.Px(vertical ? w : h);
            _min = D.Px(minPx);   // an empty bar still shows a sliver where it starts
            Set(0f);
        }

        public void Set(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Abs(value - _shown) < .0005f) return;
            _shown = value;
            float length = Mathf.Max(_min, _length * value);
            _fill.sizeDelta = _vertical ? new Vector2(_thickness, length) : new Vector2(length, _thickness);
        }
    }

    /// <summary>Line of one value over the lap, in a w x h box (design px) with an inner margin.</summary>
    protected sealed class Trace
    {
        /// <summary>The box: cursors and markers go in it, placed with X / Y.</summary>
        public readonly RectTransform rect;
        readonly ODPolyline _line;
        readonly float _pad, _w, _h;
        float _min, _max = 1f;
        Vector2[] _points;

        public Trace(Transform parent, string name, float x, float y, float w, float h, float pad = 0f)
        {
            rect = D.Rect(parent, name, x, y, w, h);
            _line = rect.gameObject.AddComponent<ODPolyline>();
            _line.color = Ink;
            _line.thickness = D.Px(3.4f);
            _line.raycastTarget = false;
            _pad = D.Px(pad);
            _w = D.Px(w - pad * 2f);
            _h = D.Px(h - pad * 2f);
        }

        /// <summary>Local x of a lap share (0 – 1).</summary>
        public float X(float share) => _pad + share * _w;

        /// <summary>Local y of a value, between the min and max of the last Plot.</summary>
        public float Y(float value) => -(_pad + (1f - Mathf.Clamp01((value - _min) / (_max - _min))) * _h);

        /// <summary>One point per bin, centred in its slice of the lap; <paramref name="min"/> at the bottom.</summary>
        public void Plot(float[] values, float min, float max)
        {
            _min = min;
            _max = Mathf.Max(min + .0001f, max);
            int bins = values.Length;
            if (bins == 0) return;
            if (_points == null || _points.Length != bins) _points = new Vector2[bins];
            for (int i = 0; i < bins; i++) _points[i] = new Vector2(X((i + .5f) / bins), Y(values[i]));
            _line.SetPoints(_points, bins);
        }
    }
}
