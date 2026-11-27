/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODDriverPickerView - The "Assigned driver" panel of a telemetry widget
 ## (design "Pilote associé"): which car the widget follows. A window of its
 ## own on the widget's right, tops aligned, in the widget's plane, joined to
 ## its driver pill by a gold line:
 ##   • title, "<Widget> · this widget" and a round close button;
 ##   • widgets comparing two cars: two chips, "Followed" (white) and
 ##     "Compared" (gold), choosing which of the two a row sets;
 ##   • "Search for a driver" — filters the list by code, name, number or
 ##     position ("P3");
 ##   • AUTOMATIC: "Follow the leader" (or "the leaders", P1 and P2), which
 ##     changes on its own with the running order;
 ##   • FIXED DRIVER · RUNNING ORDER: one row per car, "P1 ▌VER Max
 ##     Verstappen", P1 in gold, scrolling;
 ##   • the current choice on a lighter row with a check: white for the
 ##     followed car, gold for the compared one.
 ## Built for one widget at each opening (Create), destroyed when closed.
 ## View only: Logic (ODWidgetSpace) fills it and listens to Picked,
 ## LeadersPicked and Closed.
 ##
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using D = ODDesign;

public sealed class ODDriverPickerView : MonoBehaviour
{
    /// <summary>Design px: the gold line from the widget's edge, then the panel.</summary>
    public const float LinkW = 24f, PanelW = 520f;
    const float Pad = 32f, RowInset = 16f, AutoH = 76f, OrderRowH = 60f, VisibleRows = 5f, ChipH = 60f;

    /// <summary>A row of the order was picked for slot (0 = followed car, 1 = compared car).</summary>
    public event Action<int, WidgetTarget> Picked;
    /// <summary>"Follow the leader(s)": every slot back on its position (P1, P2).</summary>
    public event Action LeadersPicked;
    public event Action Closed;

    sealed class Row
    {
        public RectTransform rect;
        public RoundedImage selected;
        public GameObject[] checks;
        public TextMeshProUGUI position, code, name;
        public RoundedImage colour;
        public DriverChoice choice;
    }

    sealed class Chip
    {
        public RoundedImage fill;
        public ODHomepageSurface ring;
        public RoundedImage colour;
        public TextMeshProUGUI code;
    }

    int _slots;
    int _active;
    Row _leaders;
    readonly Dictionary<int, Row> _rows = new();
    Chip[] _chips;
    RectTransform _list;
    TMP_InputField _search;
    WidgetTarget[] _targets = Array.Empty<WidgetTarget>();

    /// <summary>Slot the next picked row sets (0 = followed, 1 = compared).</summary>
    public int ActiveSlot => _active;

    /// <summary>The whole window (line and panel) in canvas units, for a widget following <paramref name="slots"/> cars.</summary>
    public static Vector2 SizeFor(int slots) =>
        new Vector2(D.Px(LinkW + PanelW), D.Px(Layout(Mathf.Clamp(slots, 1, 2), out _, out _, out _, out _)));

    /// <summary>Vertical layout (design px): search, leader row, order caps and list tops; returns the panel height.</summary>
    static float Layout(int slots, out float search, out float automatic, out float fixedCaps, out float list)
    {
        float y = 112f;
        if (slots > 1) y += ChipH + 16f;
        search = y;
        y += 56f + 22f;
        automatic = y;
        y += 30f + AutoH + 18f;
        fixedCaps = y;
        list = y + 30f;
        return list + ListH + 20f;
    }

    static float ListH => OrderRowH * VisibleRows + 2f * (VisibleRows - 1f);

    /// <summary>
    /// Picker of the widget titled <paramref name="widget"/> following <paramref name="slots"/> cars, under
    /// <paramref name="parent"/>; the gold line at <paramref name="linkY"/> design px from the top (the pill's middle).
    /// </summary>
    public static ODDriverPickerView Create(Transform parent, string widget, int slots, float linkY)
    {
        slots = Mathf.Clamp(slots, 1, 2);
        var canvas = ODPointableCanvas.Create("DriverPicker", SizeFor(slots));
        canvas.sortingOrder = 1;   // drawn over a neighbouring widget it overlaps
        canvas.transform.SetParent(parent, false);
        var view = canvas.gameObject.AddComponent<ODDriverPickerView>();
        view._slots = slots;
        view.Build((RectTransform)canvas.transform, widget, linkY);
        return view;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Filling
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Running order, leader first: rows are kept per car and moved, not rebuilt.</summary>
    public void SetOrder(IReadOnlyList<DriverChoice> order)
    {
        var seen = new HashSet<int>();
        for (int i = 0; i < order.Count; i++)
        {
            var choice = order[i];
            seen.Add(choice.number);
            if (!_rows.TryGetValue(choice.number, out var row)) _rows[choice.number] = row = OrderRow(choice);
            row.choice = choice;
            row.position.text = choice.position > 0 ? "P" + choice.position : "—";
            row.position.color = choice.position == 1 ? D.GoldText : D.Secondary;
            row.code.text = choice.code;
            row.name.text = choice.fullName;
            row.colour.color = choice.teamColor;
            row.rect.SetSiblingIndex(i);
        }
        foreach (var pair in new List<KeyValuePair<int, Row>>(_rows))
            if (!seen.Contains(pair.Key))
            {
                Destroy(pair.Value.rect.gameObject);
                _rows.Remove(pair.Key);
            }
        Filter(_search.text);
        PaintSelection();
    }

    /// <summary>The widget's choice per slot, and the car each gives now (<paramref name="followed"/>, null when unknown).</summary>
    public void SetSelection(WidgetTarget[] targets, DriverChoice[] followed)
    {
        _targets = targets;
        if (_chips != null)
            for (int i = 0; i < _chips.Length && i < followed.Length; i++)
            {
                _chips[i].code.text = followed[i]?.code ?? "—";
                _chips[i].colour.color = followed[i]?.teamColor ?? D.Tertiary;
            }
        PaintSelection();
    }

    /// <summary>Which slot the next picked row sets.</summary>
    public void SetActiveSlot(int slot)
    {
        _active = Mathf.Clamp(slot, 0, _slots - 1);
        PaintChips();
    }

    void PaintSelection()
    {
        bool leaders = _targets.Length > 0;
        for (int i = 0; i < _targets.Length; i++) leaders &= _targets[i] == WidgetTarget.Position(i + 1);
        Mark(_leaders, leaders ? 0 : -1);
        foreach (var row in _rows.Values)
        {
            int slot = -1;
            for (int i = 0; i < _targets.Length; i++)
                if (_targets[i].kind == WidgetTargetKind.Driver && _targets[i].value == row.choice.number) slot = i;
            Mark(row, slot);
        }
    }

    /// <summary>Lighter row and the slot's check (white followed, gold compared); -1: not chosen.</summary>
    static void Mark(Row row, int slot)
    {
        row.selected.enabled = slot >= 0;
        for (int i = 0; i < row.checks.Length; i++) row.checks[i].SetActive(i == slot);
    }

    void PaintChips()
    {
        if (_chips == null) return;
        for (int i = 0; i < _chips.Length; i++)
        {
            bool on = i == _active;
            _chips[i].fill.color = D.WithAlpha(i == 0 ? Color.white : D.Gold, on ? .16f : .05f);
            _chips[i].ring.gameObject.SetActive(on);
        }
    }

    void Filter(string text)
    {
        text = (text ?? "").Trim();
        foreach (var row in _rows.Values)
        {
            var c = row.choice;
            bool show = text.Length == 0
                || c.code.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                || c.fullName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                || ("P" + c.position).Equals(text, StringComparison.OrdinalIgnoreCase)
                || c.number.ToString() == text;
            if (row.rect.gameObject.activeSelf != show) row.rect.gameObject.SetActive(show);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Building
    // ═══════════════════════════════════════════════════════════════════════

    void Build(RectTransform root, string widget, float linkY)
    {
        float height = Layout(_slots, out float searchY, out float automaticY, out float fixedY, out float listY);

        // Gold line from the widget's edge to the panel (the widget draws it from its pill to its edge).
        D.Fill(root, "Link", 0f, linkY - 1.5f, LinkW + 2f, 3f, D.Gold, 0f);

        var panel = D.Rect(root, "Panel", LinkW, 0f, PanelW, height);
        var glass = ODRuntimeUI.Stretch(panel, "Glass").gameObject.AddComponent<ODHomepageSurface>();
        glass.color = Color.white;
        glass.raycastTarget = true;   // the panel catches the ray: nothing behind it is hit through it
        ODChampionshipTheme.ApplyBackground(glass, ODTelemetryStyle.Glass);
        glass.cornerRadius = D.Px(ODTelemetryStyle.Radius);
        glass.shadowSize = 20f;

        D.Text(panel, "Title", "Assigned driver", Pad, 28f, PanelW - 140f, 36f, 28f, D.Weight.SemiBold);
        D.Text(panel, "Subtitle", widget + " · this widget", Pad, 66f, PanelW - 140f, 26f, 18f, D.Weight.Regular, D.Secondary);
        var close = D.IconButton(panel, "Close", PanelW - Pad - 48f, 28f, 48f, ODNavigationIcon.Symbol.Close, iconPx: 18f);
        close.onClick.AddListener(() => Closed?.Invoke());

        if (_slots > 1)
        {
            float chipW = (PanelW - Pad * 2f - 12f) * .5f;
            _chips = new[]
            {
                BuildChip(panel, 0, "Followed", Pad, 112f, chipW, Color.white),
                BuildChip(panel, 1, "Compared", Pad + chipW + 12f, 112f, chipW, D.GoldText),
            };
            PaintChips();
        }

        _search = D.SearchField(panel, Pad, searchY, PanelW - Pad * 2f, 56f, "Search for a driver", 20f);
        _search.onValueChanged.AddListener(Filter);

        D.Caps(panel, "AutomaticCaps", "Automatic", Pad, automaticY, PanelW - Pad * 2f, 17f);
        _leaders = LeaderRow(panel, automaticY + 30f);

        D.Caps(panel, "FixedCaps", "Fixed driver · running order", Pad, fixedY, PanelW - Pad * 2f, 17f);
        _list = ScrollList(panel, RowInset, listY, PanelW - RowInset * 2f, ListH);
    }

    Chip BuildChip(Transform panel, int slot, string label, float x, float y, float w, Color labelColour)
    {
        var button = D.Button(panel, label + "Chip", "", x, y, w, ChipH, D.ButtonKind.Ghost);
        var rect = (RectTransform)button.transform;
        ((RoundedImage)button.targetGraphic).cornerRadius = D.Px(18f);
        var chip = new Chip
        {
            fill = D.Fill(rect, "Fill", 0f, 0f, w, ChipH, Color.clear, 18f),
            ring = Ring(rect, w, ChipH, 18f, slot == 0 ? Color.white : D.Gold),
        };
        D.Caps(rect, "Label", label, 18f, 8f, w - 36f, 13f, labelColour);
        chip.colour = D.Fill(rect, "TeamColour", 18f, 32f, 5f, 20f, Color.white, 2f);
        chip.code = D.Text(rect, "Code", "", 32f, 28f, w - 50f, 28f, 20f, D.Weight.Bold);
        button.onClick.AddListener(() => SetActiveSlot(slot));
        return chip;
    }

    /// <summary>"Follow the leader" (P1), or "the leaders" (P1 and P2) for two cars.</summary>
    Row LeaderRow(Transform panel, float y)
    {
        float w = PanelW - RowInset * 2f;
        var row = RowButton(panel, "Leaders", RowInset, y, w, AutoH, 22f);
        var square = D.Fill(row.rect, "IconSquare", 16f, (AutoH - 50f) * .5f, 50f, 50f, D.WithAlpha(D.Gold, .22f), 14f);
        var icon = D.Rect(square.transform, "Icon", 12f, 12f, 26f, 26f).gameObject.AddComponent<ODNavigationIcon>();
        icon.symbol = ODNavigationIcon.Symbol.Trophy;
        icon.color = D.GoldText;
        icon.raycastTarget = false;
        bool two = _slots > 1;
        D.Text(row.rect, "Title", two ? "Follow the leaders" : "Follow the leader", 82f, 14f, w - 140f, 28f, 20f, D.Weight.SemiBold);
        D.Text(row.rect, "Line", two ? "P1 and P2 · change on their own" : "Changes on its own when the leader changes",
            82f, 44f, w - 140f, 24f, 16f, D.Weight.Regular, D.Secondary);
        row.checks = new[] { Check(row.rect, w - 16f - 30f, (AutoH - 30f) * .5f, Color.white) };
        row.rect.GetComponent<Button>().onClick.AddListener(() => LeadersPicked?.Invoke());
        return row;
    }

    /// <summary>One car of the order: "P1 ▌VER Max Verstappen".</summary>
    Row OrderRow(DriverChoice choice)
    {
        float w = PanelW - RowInset * 2f;
        var row = RowButton(_list, "Car" + choice.number, 0f, 0f, w, OrderRowH, 18f);
        row.rect.gameObject.AddComponent<LayoutElement>().preferredHeight = D.Px(OrderRowH);
        row.position = D.Text(row.rect, "Position", "", 16f, 0f, 48f, OrderRowH, 18f, D.Weight.SemiBold, D.Secondary);
        row.colour = D.Fill(row.rect, "TeamColour", 68f, (OrderRowH - 24f) * .5f, 5f, 24f, Color.white, 2f);
        row.code = D.Text(row.rect, "Code", "", 84f, 0f, 62f, OrderRowH, 20f, D.Weight.Bold);
        row.name = D.Text(row.rect, "Name", "", 150f, 0f, w - 150f - 58f, OrderRowH, 19f, D.Weight.Regular, D.Secondary);
        float cx = w - 16f - 30f, cy = (OrderRowH - 30f) * .5f;
        row.checks = _slots > 1
            ? new[] { Check(row.rect, cx, cy, Color.white), Check(row.rect, cx, cy, D.Gold) }
            : new[] { Check(row.rect, cx, cy, Color.white) };
        int number = choice.number;
        row.rect.GetComponent<Button>().onClick.AddListener(() => Picked?.Invoke(_active, WidgetTarget.Driver(number)));
        return row;
    }

    /// <summary>Row with a ghost button (hover / press) and its lighter "selected" fill.</summary>
    static Row RowButton(Transform parent, string name, float x, float y, float w, float h, float radius)
    {
        var button = D.Button(parent, name, "", x, y, w, h, D.ButtonKind.Ghost);
        var rect = (RectTransform)button.transform;
        var selected = D.Fill(rect, "Selected", 0f, 0f, w, h, new Color(1f, 1f, 1f, .12f), radius);
        selected.enabled = false;
        ((RoundedImage)button.targetGraphic).cornerRadius = D.Px(radius);
        return new Row { rect = rect, selected = selected };
    }

    /// <summary>Disc of <paramref name="colour"/> with a dark check: the current choice.</summary>
    static GameObject Check(Transform row, float x, float y, Color colour)
    {
        const float size = 30f;
        var disc = D.Fill(row, "Check", x, y, size, size, colour, size * .5f);
        // Two strokes: the short one down to the bottom of the check, the long one up from it.
        Stroke(disc.transform, 10.6f, 17.2f, 8f, -45f);
        Stroke(disc.transform, 17.2f, 14.1f, 13.5f, 50f);
        disc.gameObject.SetActive(false);
        return disc.gameObject;
    }

    static void Stroke(Transform disc, float cx, float cy, float length, float angle)
    {
        var bar = D.Fill(disc, "Stroke", 0f, 0f, length, 3.2f, D.Ink, 1.6f).rectTransform;
        bar.pivot = new Vector2(.5f, .5f);
        bar.anchoredPosition = new Vector2(D.Px(cx), -D.Px(cy));
        bar.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    /// <summary>Rim-only rounded outline over a w x h box.</summary>
    static ODHomepageSurface Ring(Transform parent, float w, float h, float radius, Color colour)
    {
        var ring = D.Rect(parent, "Ring", 0f, 0f, w, h).gameObject.AddComponent<ODHomepageSurface>();
        ring.color = Color.white;
        ring.raycastTarget = false;
        ring.glass = true;
        ring.useMiddle = false;
        ring.sheen = 0f;
        ring.shadowSize = 0f;
        ring.rimWidth = D.Px(2f);
        ring.cornerRadius = D.Px(radius);
        ring.cornerSegments = 12;
        ring.topLeft = ring.bottomRight = Color.clear;
        ring.rimTop = ring.rimBottom = colour;
        return ring;
    }

    /// <summary>Clipped vertical list the ray can drag; returns its content (a vertical layout).</summary>
    static RectTransform ScrollList(Transform panel, float x, float y, float w, float h)
    {
        var viewport = D.Rect(panel, "Order", x, y, w, h);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>();   // lets the ray drag the list between rows
        hit.color = Color.clear;

        var content = D.Rect(viewport, "Content", 0f, 0f, w, 0f);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = D.Px(2f);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;
        return content;
    }
}
