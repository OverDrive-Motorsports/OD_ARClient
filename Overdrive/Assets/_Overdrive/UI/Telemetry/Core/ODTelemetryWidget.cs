/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTelemetryWidget - Base of every telemetry widget. It owns what all of
 ## them share, so a widget only draws its own content and reacts to data:
 ##   • sizing: the two formats of ODTelemetryStyle, always the same frame;
 ##   • frame: frosted glass card like the other windows (blurred room
 ##     behind, ODChampionshipTheme.ApplyBackground), radius 52;
 ##   • header: caps title, followed-driver pill ("VER", or "VER / HAM" when
 ##     the widget compares two cars — ComparesRival);
 ##   • data: Logic pushes each TelemetryFrame with Show(); the base hands its
 ##     parts to the widget (OnCar, OnTrace, OnTiming) and ticks it every
 ##     frame (Tick) so values ease between samples;
 ##   • builders for the recurring pieces (Readout, Stat, Pair, Panel) and
 ##     the parts in ODTelemetryWidget.Parts.cs (LiveText, Eased, Bar, Trace).
 ##
 ## A new widget: derive from it, give a Title, build BuildSmall / BuildLarge
 ## in design px on the card (header included, Pad = 36 margins), override the
 ## data hooks it needs, then add its type to ODTelemetryWindow.Widgets.
 ##
 */

using System;
using TMPro;
using UnityEngine;
using static ODTelemetryStyle;
using D = ODDesign;

public abstract partial class ODTelemetryWidget : MonoBehaviour
{
    public WidgetFormat Format { get; private set; }

    /// <summary>Caps title of the header ("Speed").</summary>
    protected abstract string Title { get; }

    /// <summary>The header pill shows the compared car next to the followed one ("VER / HAM").</summary>
    protected virtual bool ComparesRival => false;

    /// <summary>Draws the content on <paramref name="card"/> (w x h design px, header included).</summary>
    protected abstract void BuildSmall(RectTransform card, float w, float h);

    protected abstract void BuildLarge(RectTransform card, float w, float h);

    /// <summary>New live sample. <paramref name="first"/>: the first one since the widget was built — show it without easing.</summary>
    protected virtual void OnCar(in CarTelemetry car, bool first) { }

    protected virtual void OnTrace(LapTrace trace) { }

    protected virtual void OnTiming(LapTiming timing, LapTiming rival) { }

    /// <summary>Every frame once data came: ease the shown values toward the last sample.</summary>
    protected virtual void Tick(float dt) { }

    TextMeshProUGUI _driver, _rival;
    RoundedImage _driverColour, _rivalColour;
    bool _hasData;

    /// <summary>Widget of <paramref name="type"/> (an ODTelemetryWidget) in <paramref name="format"/>, top-left at (x, y) design px.</summary>
    public static ODTelemetryWidget Create(Type type, Transform parent, float x, float y, WidgetFormat format)
    {
        var size = SizeOf(format);
        var rect = D.Rect(parent, type.Name + format, x, y, size.x, size.y);
        var widget = (ODTelemetryWidget)rect.gameObject.AddComponent(type);
        widget.Build(format, size);
        return widget;
    }

    /// <summary>Shows a new frame of the feed.</summary>
    public void Show(TelemetryFrame frame)
    {
        if (frame == null) return;
        ShowDriver(_driver, _driverColour, frame.timing);
        if (_rival != null) ShowDriver(_rival, _rivalColour, frame.rival);
        OnTrace(frame.trace);
        OnTiming(frame.timing, frame.rival);
        OnCar(frame.car, !_hasData);
        _hasData = true;
    }

    void Update()
    {
        if (_hasData) Tick(ODTime.Delta);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Frame and header
    // ═══════════════════════════════════════════════════════════════════════

    void Build(WidgetFormat format, Vector2 size)
    {
        Format = format;
        var card = (RectTransform)transform;

        var glass = ODRuntimeUI.Stretch(card, "Glass").gameObject.AddComponent<ODHomepageSurface>();
        glass.color = Color.white;
        glass.raycastTarget = false;
        ODChampionshipTheme.ApplyBackground(glass, Glass);
        glass.cornerRadius = D.Px(Radius);
        glass.shadowSize = 20f;

        D.Caps(card, "Title", Title, Pad, 37f, size.x * .5f, 18f, Label);
        float pillW = ComparesRival ? 184f : 98f;
        BuildPill(card, size.x - Pad - pillW, 32f, pillW);

        if (format == WidgetFormat.Small) BuildSmall(card, size.x, size.y);
        else BuildLarge(card, size.x, size.y);
    }

    /// <summary>Capsule with team colour and code of each driver, and a chevron.</summary>
    void BuildPill(Transform card, float x, float y, float w)
    {
        const float h = 36f;
        var pill = D.Fill(card, "Driver", x, y, w, h, new Color(1f, 1f, 1f, .12f), h * .5f).transform;
        _driver = PillDriver(pill, 14f, out _driverColour);
        if (ComparesRival)
        {
            D.Text(pill, "Slash", "/", 74f, 0f, 16f, h, 18f, D.Weight.Regular, Faint, TextAlignmentOptions.Center);
            _rival = PillDriver(pill, 98f, out _rivalColour);
        }

        // Downward chevron: two short strokes, left one "\", right one "/".
        const float arm = 6.2f, stroke = 2.4f;
        for (int side = -1; side <= 1; side += 2)
        {
            var bar = D.Fill(pill, "Chevron", 0f, 0f, arm, stroke, Label, stroke * .5f).rectTransform;
            bar.pivot = new Vector2(.5f, .5f);
            bar.anchoredPosition = new Vector2(D.Px(w - 22f + side * arm * .33f), -D.Px(h * .5f + 1f));
            bar.localEulerAngles = new Vector3(0f, 0f, side * 42f);
        }
    }

    static TextMeshProUGUI PillDriver(Transform pill, float x, out RoundedImage colour)
    {
        colour = D.Fill(pill, "TeamColour", x, 9f, 5f, 18f, Color.white, 1.5f);
        return D.Text(pill, "Code", "", x + 12f, 0f, 46f, 36f, 18f, D.Weight.Bold, Ink);
    }

    static void ShowDriver(TextMeshProUGUI code, RoundedImage colour, LapTiming timing)
    {
        if (ShowCode(code, timing)) colour.color = timing.teamColor;
    }

    /// <summary>Writes the driver code of <paramref name="timing"/> when it changed; true if it did.</summary>
    protected static bool ShowCode(TextMeshProUGUI code, LapTiming timing)
    {
        string text = timing.driverCode ?? "";
        if (code.text == text) return false;
        code.text = text;
        return true;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Builders for the widgets (design px on the card)
    // ═══════════════════════════════════════════════════════════════════════

    protected static LiveText Live(TextMeshProUGUI label) => new LiveText(label);

    /// <summary>Big readout in light digits ("287", "1:12.408").</summary>
    protected static LiveText Readout(Transform parent, float x, float y, float w, float h, float px,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft) =>
        Live(Light(D.Text(parent, "Readout", "", x, y, w, h, px, D.Weight.Regular, Ink, align)));

    /// <summary>Caps name over a value ("TOP / 312"), top-left at (x, y).</summary>
    protected static LiveText Stat(Transform parent, string name, float x, float y, float w)
    {
        D.Caps(parent, name + "Caps", name, x, y, w, 16f, StatCaps);
        return Live(D.Text(parent, name, "", x, y + 24f, w, 36f, 28f, D.Weight.SemiBold, Ink));
    }

    /// <summary>"Best ········ 1:34.012" line centred on <paramref name="y"/>, from x over w.</summary>
    protected static LiveText Pair(Transform parent, string name, float x, float y, float w, float valuePx = 24f, Color? nameColour = null)
    {
        D.Text(parent, name + "Name", name, x, y - 16f, w * .5f, 32f, 20f, D.Weight.Regular, nameColour ?? Label);
        return Live(D.Text(parent, name, "", x + w * .4f, y - 18f, w * .6f, 36f, valuePx, D.Weight.SemiBold, Ink,
            TextAlignmentOptions.MidlineRight));
    }

    /// <summary>Recessed box with an optional coloured rim (rows, strips, notes); recolour it with Tint.</summary>
    protected static ODHomepageSurface Panel(Transform parent, string name, float x, float y, float w, float h, float radius)
    {
        var panel = D.Rect(parent, name, x, y, w, h).gameObject.AddComponent<ODHomepageSurface>();
        panel.color = Color.white;
        panel.raycastTarget = false;
        panel.glass = true;   // flat fill + rim, no material: the frosted card shows through
        panel.useMiddle = false;
        panel.sheen = 0f;
        panel.shadowSize = 0f;
        panel.rimWidth = D.Px(2f);
        panel.cornerRadius = D.Px(radius);
        panel.cornerSegments = 12;
        Tint(panel, Well, Color.clear);
        return panel;
    }

    protected static void Tint(ODHomepageSurface panel, Color fill, Color rim)
    {
        panel.topLeft = panel.bottomRight = fill;
        panel.rimTop = panel.rimBottom = rim;
        panel.SetVerticesDirty();
    }
}
