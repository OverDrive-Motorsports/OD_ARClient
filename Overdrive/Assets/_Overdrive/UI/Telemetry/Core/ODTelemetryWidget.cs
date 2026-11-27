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
 ##   • header: caps title, followed-driver pill with its team colour, code
 ##     and number ("▌VER 1 ⌄", or "▌VER 1 / ▌HAM" when the widget compares
 ##     two cars — ComparesRival —, the compared one in gold). In the space the pill is a button
 ##     (EnablePicker) that opens the driver picker; while it is open the
 ##     pill and the card get a gold rim and a gold line runs from the pill
 ##     to the picker (SetPickerOpen). ShowTarget writes the target in it;
 ##     Restart forgets the shown values when the target changes;
 ##   • data: Logic pushes each TelemetryFrame with Show(); the base hands its
 ##     parts to the widget (OnCar, OnTrace, OnTiming) and ticks it every
 ##     frame (Tick) so values ease between samples;
 ##   • status: ShowStatus() — loading, no data or connection lost hide the
 ##     whole content under a short message (the header stays), so no stale
 ##     value is ever shown; the first frame after it is shown without easing;
 ##   • builders for the recurring pieces (Readout, Stat, Pair, Panel) and
 ##     the parts in ODTelemetryWidget.Parts.cs (LiveText, Eased, Bar, Trace).
 ##
 ## A new widget: derive from it, give a Title, build BuildSmall / BuildLarge
 ## in design px on the card (header included, Pad = 36 margins), override the
 ## data hooks it needs, then list it in the catalogue (ODWidgetCatalogPage.Widgets).
 ## ShowStill paints one frame and freezes the widget (catalogue previews).
 ##
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

    /// <summary>The widget follows another car: drop what was kept of the previous one.</summary>
    protected virtual void OnRestart() { }

    /// <summary>Caps title, also the picker's subtitle ("Speed · this widget").</summary>
    public string Name => Title;

    /// <summary>Shows a second car (its frames carry the rival).</summary>
    public bool Compares => ComparesRival;

    /// <summary>Design px from the card's top to the middle of the driver pill (the picker's gold line starts there).</summary>
    public const float PillMiddle = PillY + PillH * .5f;

    /// <summary>The driver pill was pressed (EnablePicker).</summary>
    public event Action DriverPillClicked;

    TextMeshProUGUI _driver, _rival, _number, _statusText;
    RoundedImage _driverColour, _rivalColour;
    RectTransform _content;
    ODHomepageSurface _glass, _pillRing;
    RoundedImage _pill, _link;
    Button _pillButton;
    Color _rimTop, _rimBottom;
    float _rimWidth;
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

    /// <summary>
    /// What the feed can give: Ready shows the content, anything else hides it under
    /// <paramref name="message"/> ("Loading telemetry…"). The header keeps the drivers of <paramref name="header"/>.
    /// </summary>
    public void ShowStatus(TelemetryStatus status, string message, TelemetryFrame header)
    {
        if (header != null)
        {
            ShowDriver(_driver, _driverColour, header.timing);
            if (_rival != null) ShowDriver(_rival, _rivalColour, header.rival);
        }
        bool ready = status == TelemetryStatus.Ready;
        if (!ready) _hasData = false;   // the next frame is placed at once, not eased from old values
        _content.gameObject.SetActive(ready);
        _statusText.gameObject.SetActive(!ready);
        _statusText.text = message ?? "";
        _statusText.color = status == TelemetryStatus.ConnectionLost ? D.Destructive : Label;
    }

    /// <summary>Header pill: team colour, code and number of the followed car, and the compared one ("—" while unknown).</summary>
    public void ShowTarget(DriverChoice target, DriverChoice rival = null)
    {
        _driver.text = target != null ? target.code : "—";
        _driverColour.color = target != null ? target.teamColor : Faint;
        _number.text = target != null && target.number > 0 ? target.number.ToString() : "";
        if (_rival == null) return;
        _rival.text = rival != null ? rival.code : "—";
        _rivalColour.color = rival != null ? rival.teamColor : Faint;
    }

    /// <summary>The followed car changed: the next frame is shown at once, nothing kept from the previous car.</summary>
    public void Restart()
    {
        _hasData = false;
        OnRestart();
    }

    /// <summary>Makes the driver pill a button raising DriverPillClicked (live widgets, not catalogue previews).</summary>
    public void EnablePicker()
    {
        if (_pillButton != null) return;
        _pill.raycastTarget = true;
        _pillButton = _pill.gameObject.AddComponent<Button>();
        _pillButton.targetGraphic = _pill;
        D.ApplyButton(_pillButton, D.ButtonKind.Ghost);
        PaintPill(false);
        _pillButton.onClick.AddListener(() => DriverPillClicked?.Invoke());
    }

    /// <summary>Gold pill rim, card rim and line toward the picker while it is open for this widget.</summary>
    public void SetPickerOpen(bool open)
    {
        PaintPill(open);
        _pillRing.gameObject.SetActive(open);
        _link.gameObject.SetActive(open);
        _glass.rimTop = open ? D.Gold : _rimTop;
        _glass.rimBottom = open ? D.Gold : _rimBottom;
        _glass.rimWidth = open ? D.Px(3f) : _rimWidth;
        _glass.SetVerticesDirty();
    }

    void PaintPill(bool open)
    {
        if (_pillButton == null) return;
        var colours = _pillButton.colors;
        Color tint = open ? D.Gold : Color.white;
        colours.normalColor = colours.selectedColor = D.WithAlpha(tint, open ? .26f : .12f);
        colours.highlightedColor = D.WithAlpha(tint, open ? .36f : .20f);
        colours.pressedColor = D.WithAlpha(tint, open ? .44f : .28f);
        _pillButton.colors = colours;
        _pill.color = Color.white;
        _pill.CrossFadeColor(colours.normalColor, 0f, true, true);
    }

    /// <summary>
    /// Still picture of <paramref name="frame"/> (catalogue previews): the values are painted
    /// once, as they are, and the widget stops ticking — nothing eases or runs afterwards.
    /// </summary>
    public void ShowStill(TelemetryFrame frame)
    {
        if (frame == null) return;
        Show(frame);
        Tick(0f);          // widgets that paint on the tick (lap clock) draw the sample once
        enabled = false;   // no Update: the picture stays still
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
        _glass = glass;
        _rimTop = glass.rimTop;
        _rimBottom = glass.rimBottom;
        _rimWidth = glass.rimWidth;

        D.Caps(card, "Title", Title, Pad, 37f, size.x * .5f, 18f, Label);
        float pillW = ComparesRival ? 214f : 128f, pillX = size.x - Pad - pillW;
        // Gold line from the pill to the card's edge, carried on by the open picker.
        _link = D.Fill(card, "PickerLink", pillX + pillW, PillMiddle - 1.5f, Pad, 3f, D.Gold, 0f);
        _link.gameObject.SetActive(false);
        _pillRing = Panel(card, "PillRing", pillX - 3f, PillY - 3f, pillW + 6f, PillH + 6f, PillH * .5f + 3f);
        Tint(_pillRing, Color.clear, D.Gold);
        _pillRing.rimWidth = D.Px(2.5f);
        _pillRing.gameObject.SetActive(false);
        BuildPill(card, pillX, PillY, pillW);

        // The widget's own content, over the whole card (same coordinates): hidden as one piece by ShowStatus.
        _content = ODRuntimeUI.Stretch(card, "Content");
        if (format == WidgetFormat.Small) BuildSmall(_content, size.x, size.y);
        else BuildLarge(_content, size.x, size.y);

        _statusText = D.Text(card, "Status", "", Pad, HeaderH, size.x - Pad * 2f, size.y - HeaderH - Pad, 22f,
            D.Weight.Regular, Label, TextAlignmentOptions.Center);
        _statusText.textWrappingMode = TextWrappingModes.Normal;
        _statusText.gameObject.SetActive(false);
    }

    /// <summary>Height of the header (title and driver pill), design px.</summary>
    const float HeaderH = 92f;
    const float PillY = 32f, PillH = 36f;

    /// <summary>Capsule with team colour, code and number of the followed driver (and the compared one), and a chevron.</summary>
    void BuildPill(Transform card, float x, float y, float w)
    {
        const float h = PillH;
        _pill = D.Fill(card, "Driver", x, y, w, h, new Color(1f, 1f, 1f, .12f), h * .5f);
        var pill = _pill.transform;
        _driver = PillDriver(pill, 14f, out _driverColour);
        _number = D.Text(pill, "Number", "", 72f, 0f, 28f, h, 15f, D.Weight.SemiBold, Faint);
        if (ComparesRival)
        {
            D.Text(pill, "Slash", "/", 100f, 0f, 16f, h, 18f, D.Weight.Regular, Faint, TextAlignmentOptions.Center);
            _rival = PillDriver(pill, 124f, out _rivalColour);
            _rival.color = D.GoldText;   // design: the followed car in white, the compared one in gold
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
