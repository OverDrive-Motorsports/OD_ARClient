/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTelemetryStyle - Tokens shared by every telemetry widget (design
 ## "Widgets télémétrie"): the two formats and their sizes, spacing, the
 ## monochrome palette on the frosted glass, the F1 timing colour code, and
 ## how numbers and times are written. Widgets import it with
 ## `using static ODTelemetryStyle;`.
 ##
 */

using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using D = ODDesign;

// The two formats (WidgetFormat: Small / Large) are declared once, in Data (WidgetCatalog.cs).

public static class ODTelemetryStyle
{
    // ── Sizes (design px) ───────────────────────────────────────────────────

    /// <summary>Small is a square, Large two squares and their gap, so the formats line up in a grid.</summary>
    public const float Square = 440f, Gap = 40f, Radius = 52f, Pad = 36f;

    public static Vector2 SizeOf(WidgetFormat format) =>
        format == WidgetFormat.Small ? new Vector2(Square, Square) : new Vector2(Square * 2f + Gap, Square);

    // ── Colours ─────────────────────────────────────────────────────────────

    /// <summary>Frosted window glass, neutral grey (no championship glow).</summary>
    public static readonly ODChampionshipTheme.Palette Glass = new ODChampionshipTheme.Palette
    {
        accent = D.Blue,
        topLeft = D.Hex("3A3A3C"),
        middle = D.Night,
        bottomRight = D.Night,
    };

    public static readonly Color Ink = new Color(1f, 1f, 1f, .96f);        // values, throttle, traces
    public static readonly Color InkSoft = new Color(1f, 1f, 1f, .80f);    // brake
    public static readonly Color Label = new Color(1f, 1f, 1f, .62f);
    public static readonly Color StatCaps = new Color(1f, 1f, 1f, .72f);
    public static readonly Color Faint = new Color(1f, 1f, 1f, .45f);
    public static readonly Color GridLine = new Color(1f, 1f, 1f, .08f);
    public static readonly Color Well = new Color(0f, 0f, 0f, .26f);       // recessed: bar tracks, strips, boxes
    public static readonly Color Ahead = new Color(1f, 1f, 1f, .14f);      // not driven yet
    public static readonly Color Yellow = D.Hex("F5C84C");

    /// <summary>F1 colour code: purple session best, green personal best, yellow slower, grey ahead.</summary>
    public static Color Of(SplitStatus status) => status switch
    {
        SplitStatus.SessionBest => D.Purple,
        SplitStatus.PersonalBest => D.Green,
        SplitStatus.Slower => Yellow,
        _ => Ahead,
    };

    // ── Numbers ─────────────────────────────────────────────────────────────

    static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    static readonly StringBuilder Builder = new StringBuilder(64);

    /// <summary>
    /// Monospaced digits: the text keeps its width while the number changes. Only the runs of
    /// digits are spaced, so ":" "." "−" keep their natural width ("1:34.012" stays compact).
    /// </summary>
    public static string Digits(string text)
    {
        Builder.Clear();
        bool inDigits = false;
        foreach (char c in text)
        {
            bool digit = c >= '0' && c <= '9';
            if (digit != inDigits) Builder.Append(digit ? "<mspace=0.58em>" : "</mspace>");
            inDigits = digit;
            Builder.Append(c);
        }
        if (inDigits) Builder.Append("</mspace>");
        return Builder.ToString();
    }

    public static string Digits(int value) => Digits(value.ToString(Invariant));

    /// <summary>"1:34.012"; under a minute "28.199", or "0:06.819" with <paramref name="alwaysMinutes"/>.</summary>
    public static string LapClock(float seconds, bool alwaysMinutes = false)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = (int)(seconds / 60f);
        float rest = seconds - minutes * 60f;
        return minutes > 0 || alwaysMinutes
            ? minutes + ":" + rest.ToString("00.000", Invariant)
            : rest.ToString("0.000", Invariant);
    }

    /// <summary>"+0.108" / "−0.131" (true minus sign).</summary>
    public static string Diff(float seconds) =>
        (seconds < 0f ? "−" : "+") + Mathf.Abs(seconds).ToString("0.000", Invariant);

    public static string Km(float km) => km.ToString("0.0", Invariant) + " km";

    // ── Type ────────────────────────────────────────────────────────────────

    static Material _light;

    /// <summary>
    /// The big readouts of the design are DM Sans Light. Only Regular / SemiBold / Bold are
    /// imported, so Regular is thinned through its distance field instead.
    /// </summary>
    public static TextMeshProUGUI Light(TextMeshProUGUI label)
    {
        if (_light == null && label.font != null)
        {
            _light = new Material(label.font.material) { name = "DM Sans Light (thinned Regular)" };
            _light.SetFloat(ShaderUtilities.ID_FaceDilate, -.22f);
        }
        if (_light != null) label.fontSharedMaterial = _light;
        return label;
    }
}
