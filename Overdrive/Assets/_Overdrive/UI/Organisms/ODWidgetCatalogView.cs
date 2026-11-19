/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODWidgetCatalogView - Widget catalogue window, opened beside the race
 ## window by its "Telemetry" button. Three columns, drawn at runtime in design px
 ## (ODDesign, 1920 x 1200 window):
 ##   • side bar: "Widgets" title, search field, categories with their count,
 ##     and how many widgets are in the user's space;
 ##   • centre: the selected category, then its widgets — for now always the
 ##     "No widgets available yet" state, no telemetry widget exists yet;
 ##   • right: the selected widget — preview, name, description, format
 ##     (Small / Large) and "Add to my space" (disabled while nothing is picked).
 ## The close button (top right) returns to the race in one action.
 ## Display only: it emits events, ODWidgetCatalogPage decides.
 ##
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using D = ODDesign;

public class ODWidgetCatalogView : MonoBehaviour
{
    public event Action CloseClicked;
    public event Action<int> CategorySelected;
    public event Action<string> SearchChanged;
    public event Action<WidgetFormat> FormatChanged;
    public event Action AddClicked;

    // ── Layout (design px) ──────────────────────────────────────────────────

    const float SideX = 24f, SideY = 24f, SideW = 446f, SideH = 1200f - 48f, SideRadius = D.WindowRadius - 20f;
    const float RowY = 220f, RowH = 80f, RowGap = 8f, RowX = 16f, RowW = SideW - 32f;
    // Right column: 548 px in the first layout, 1/6 narrower now; the centre column gets the difference.
    const float DetailW = 548f * 5f / 6f, DetailX = 1920f - 56f - DetailW, DetailTop = 128f;
    const float SplitX = DetailX - 26f, MainX = SideX + SideW + 48f, MainW = SplitX - 48f - MainX;
    const float ContentTop = 200f, ContentBottom = 1200f - D.WindowPadBottom;
    const float PreviewH = 300f, AddH = 80f, AddY = 1200f - 56f - 44f - AddH;

    /// <summary>Plain grey glass: no blue / gold corner glow behind the catalogue.</summary>
    static readonly ODChampionshipTheme.Palette PlainGlass = new ODChampionshipTheme.Palette
    {
        accent = D.Blue,
        topLeft = D.Hex("3A3A3C"),
        middle = D.Night,
        bottomRight = D.Night,
    };

    bool _built;
    RectTransform _side, _main, _detail;
    TMP_InputField _search;
    readonly List<CategoryRow> _rows = new();
    TextMeshProUGUI _heading, _subtitle, _spaceCount;
    RectTransform _empty, _grid;
    TextMeshProUGUI _emptyTitle, _emptyBody;
    TextMeshProUGUI _detailName, _detailDescription;
    CanvasGroup _format;
    Button _add;
    TextMeshProUGUI _addLabel;
    ODNavigationIcon _addIcon;

    struct CategoryRow
    {
        public Button button;
        public TextMeshProUGUI name, count;
    }

    public void Build()
    {
        if (_built) return;
        _built = true;
        var root = (RectTransform)transform;

        // Glass window like the other pages; it swallows every ray.
        var window = ODRuntimeUI.Stretch(root, "Window").gameObject.AddComponent<ODHomepageSurface>();
        window.color = Color.white;
        window.raycastTarget = true;
        ODChampionshipTheme.ApplyBackground(window, PlainGlass);

        BuildSideBar(root);
        BuildMain(root);
        D.Fill(root, "Split", SplitX, SideY + 40f, 1.6f, SideH - 80f, D.Divider, 0f);
        BuildDetail(root);

        var close = D.IconButton(root, "Close", 1920f - 40f - 68f, 36f, 68f, ODNavigationIcon.Symbol.Close, iconPx: 26f);
        close.onClick.AddListener(() => CloseClicked?.Invoke());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Side bar
    // ═══════════════════════════════════════════════════════════════════════

    void BuildSideBar(RectTransform root)
    {
        _side = D.Fill(root, "SideBar", SideX, SideY, SideW, SideH, D.Recessed, SideRadius).rectTransform;
        D.Text(_side, "Title", "Widgets", 40f, 36f, SideW - 80f, 72f, D.TitleXL, D.Weight.Bold);

        _search = D.SearchField(_side, 32f, 124f, SideW - 64f, 64f, "Search");
        _search.onValueChanged.AddListener(text => SearchChanged?.Invoke(text));

        var footer = D.Fill(_side, "SpaceCount", RowX, SideH - 16f - 72f, RowW, 72f, D.Plateau, 36f);
        var icon = D.Rect(footer.transform, "Icon", 28f, 23f, 26f, 26f).gameObject.AddComponent<ODNavigationIcon>();
        icon.symbol = ODNavigationIcon.Symbol.Categories;
        icon.color = D.Secondary;
        icon.raycastTarget = false;
        _spaceCount = D.Text(footer.transform, "Label", "", 70f, 0f, RowW - 70f - 24f, 72f, 22f, D.Weight.Regular, D.Secondary);
    }

    void BuildRows(List<WidgetCategory> categories)
    {
        foreach (var row in _rows) if (row.button != null) Destroy(row.button.gameObject);
        _rows.Clear();
        for (int i = 0; i < categories.Count; i++)
        {
            int index = i;
            float y = RowY + i * (RowH + RowGap);
            var button = D.Button(_side, "Category" + i, "", RowX, y, RowW, RowH, D.ButtonKind.Ghost);
            ((RoundedImage)button.targetGraphic).cornerRadius = D.Px(28f);
            button.onClick.AddListener(() => CategorySelected?.Invoke(index));

            Style(categories[i].id, out var symbol, out Color colour, out Color iconColour);
            var tile = D.Fill(button.transform, "Tile", 16f, (RowH - 52f) * .5f, 52f, 52f, D.WithAlpha(colour, .30f), 16f);
            var glyph = D.Rect(tile.transform, "Icon", 13f, 13f, 26f, 26f).gameObject.AddComponent<ODNavigationIcon>();
            glyph.symbol = symbol;
            glyph.color = iconColour;
            glyph.raycastTarget = false;

            _rows.Add(new CategoryRow
            {
                button = button,
                name = D.Text(button.transform, "Name", categories[i].name, 88f, 0f, RowW - 88f - 80f, RowH, D.RowTitle, D.Weight.SemiBold),
                count = D.Text(button.transform, "Count", "0", RowW - 24f - 60f, 0f, 60f, RowH, 22f, D.Weight.Regular, D.Tertiary,
                    TextAlignmentOptions.MidlineRight),
            });
        }
    }

    /// <summary>Icon and colour of each category tile (unknown ids get the neutral grid).</summary>
    static void Style(string categoryId, out ODNavigationIcon.Symbol symbol, out Color colour, out Color icon)
    {
        switch (categoryId)
        {
            case "suggestions": symbol = ODNavigationIcon.Symbol.Star; colour = D.Gold; icon = D.GoldLight; return;
            case "car": symbol = ODNavigationIcon.Symbol.Gauge; colour = D.Blue; icon = D.BlueSoft; return;
            case "tyres": symbol = ODNavigationIcon.Symbol.Tyre; colour = D.Orange; icon = D.OrangeText; return;
            case "timing": symbol = ODNavigationIcon.Symbol.Timer; colour = D.Purple; icon = D.PurpleText; return;
            case "race": symbol = ODNavigationIcon.Symbol.Races; colour = D.Green; icon = D.GreenText; return;
            default: symbol = ODNavigationIcon.Symbol.Categories; colour = Color.white; icon = Color.white; return;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Centre — selected category
    // ═══════════════════════════════════════════════════════════════════════

    void BuildMain(RectTransform root)
    {
        _main = D.Rect(root, "Main", MainX, 0f, MainW, 1200f);
        _heading = D.Text(_main, "Heading", "", 0f, 52f, MainW, 72f, D.TitleXL, D.Weight.Bold);
        _subtitle = D.Text(_main, "Subtitle", "", 0f, 128f, MainW, 32f, D.Body, D.Weight.Regular, D.Secondary);

        // Widget cards go here once telemetry widgets exist.
        _grid = D.Rect(_main, "Grid", 0f, ContentTop, MainW, ContentBottom - ContentTop);

        const float h = ContentBottom - ContentTop, disc = 112f;
        _empty = D.Fill(_main, "Empty", 0f, ContentTop, MainW, h, new Color(0f, 0f, 0f, .12f), D.CardRadius).rectTransform;
        float y = h * .5f - 150f;
        var circle = D.Fill(_empty, "Disc", (MainW - disc) * .5f, y, disc, disc, D.Avatar, disc * .5f);
        var icon = D.Rect(circle.transform, "Icon", (disc - 48f) * .5f, (disc - 48f) * .5f, 48f, 48f).gameObject.AddComponent<ODNavigationIcon>();
        icon.symbol = ODNavigationIcon.Symbol.Categories;
        icon.color = D.Secondary;
        icon.raycastTarget = false;
        _emptyTitle = D.Text(_empty, "Title", "", 40f, y + disc + 32f, MainW - 80f, 44f, D.Title2, D.Weight.SemiBold, D.Primary,
            TextAlignmentOptions.Center);
        _emptyBody = Paragraph(_empty, "Body", 110f, y + disc + 32f + 44f + 16f, MainW - 220f, 100f, 22f, D.Muted, TextAlignmentOptions.Top);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Right — selected widget
    // ═══════════════════════════════════════════════════════════════════════

    void BuildDetail(RectTransform root)
    {
        _detail = D.Rect(root, "Detail", DetailX, 0f, DetailW, 1200f);

        var preview = D.Fill(_detail, "Preview", 0f, DetailTop, DetailW, PreviewH, D.Recessed, D.PlateauRadius);
        var icon = D.Rect(preview.transform, "Icon", (DetailW - 44f) * .5f, PreviewH * .5f - 44f, 44f, 44f).gameObject.AddComponent<ODNavigationIcon>();
        icon.symbol = ODNavigationIcon.Symbol.Categories;
        icon.color = D.Tertiary;
        icon.raycastTarget = false;
        D.Text(preview.transform, "Label", "Widget preview", 0f, PreviewH * .5f + 14f, DetailW, 28f, 20f, D.Weight.Regular, D.Tertiary,
            TextAlignmentOptions.Center);

        float y = DetailTop + PreviewH + 36f;
        _detailName = D.Text(_detail, "Name", "", 0f, y, DetailW, 56f, D.Title1, D.Weight.SemiBold);
        _detailDescription = Paragraph(_detail, "Description", 0f, y + 66f, DetailW, 96f, 23f, D.Secondary, TextAlignmentOptions.TopLeft);

        y += 66f + 96f + 36f;
        D.Caps(_detail, "FormatCaps", "Format", 0f, y, DetailW);
        var format = ODSegmentedControl.Create(_detail, 0f, D.Px(y + 40f), D.Px(DetailW), D.Px(72f),
            new[] { "Small", "Large" }, 1, D.Px(23f));
        format.Changed += index => FormatChanged?.Invoke(index == 0 ? WidgetFormat.Small : WidgetFormat.Large);
        _format = format.gameObject.AddComponent<CanvasGroup>();

        _add = D.Button(_detail, "Add", "", 0f, AddY, DetailW, AddH, D.ButtonKind.Primary);
        _add.onClick.AddListener(() => AddClicked?.Invoke());
        const float labelW = 260f, iconSize = 24f, gap = 14f, start = (DetailW - iconSize - gap - labelW) * .5f;
        _addIcon = D.Rect(_add.transform, "Icon", start, (AddH - iconSize) * .5f, iconSize, iconSize).gameObject.AddComponent<ODNavigationIcon>();
        _addIcon.symbol = ODNavigationIcon.Symbol.Plus;
        _addIcon.raycastTarget = false;
        _addLabel = D.Text(_add.transform, "Label", "Add to my space", start + iconSize + gap, 0f, labelW, AddH, 25f, D.Weight.SemiBold, D.Ink);

        D.Text(_detail, "Footnote", "You can move, resize or remove it at any time.", 0f, AddY + AddH + 20f, DetailW, 28f, 19f,
            D.Weight.Regular, D.Tertiary, TextAlignmentOptions.Center);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Data
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Paints the catalogue for <paramref name="selected"/> (index in the categories) and the
    /// widgets it lists after the search. No widget is picked yet, so the right column shows its
    /// empty state and "Add to my space" stays disabled.
    /// </summary>
    public void Show(WidgetCatalog catalog, int selected, List<WidgetDefinition> widgets, string search)
    {
        Build();
        if (_rows.Count != catalog.categories.Count) BuildRows(catalog.categories);

        for (int i = 0; i < _rows.Count; i++)
        {
            D.ApplyButton(_rows[i].button, i == selected ? D.ButtonKind.Secondary : D.ButtonKind.Ghost);
            ((RoundedImage)_rows[i].button.targetGraphic).cornerRadius = D.Px(28f);
            _rows[i].count.text = catalog.CountIn(catalog.categories[i].id).ToString();
        }

        var category = selected >= 0 && selected < catalog.categories.Count ? catalog.categories[selected] : null;
        int count = widgets.Count;
        _heading.text = category != null ? category.name : "Widgets";
        _subtitle.text = $"{count} widget{(count == 1 ? "" : "s")}" + (category != null && !string.IsNullOrEmpty(category.description) ? " · " + category.description : "");
        _spaceCount.text = $"In your space: {catalog.AddedCount} widget{(catalog.AddedCount == 1 ? "" : "s")}";

        bool searching = !string.IsNullOrWhiteSpace(search);
        _empty.gameObject.SetActive(count == 0);
        _grid.gameObject.SetActive(count > 0);
        _emptyTitle.text = searching ? "No widget found" : "No widgets available yet";
        _emptyBody.text = searching
            ? $"No widget matches “{search.Trim()}”. Try another name."
            : "Telemetry widgets are on their way. They will show up here as soon as they are ready.";

        ShowNoSelection();
    }

    void ShowNoSelection()
    {
        _detailName.text = "No widget selected";
        _detailDescription.text = "Pick a widget in the catalogue to see its preview and formats, then add it to your view.";
        _format.alpha = .38f;
        _format.interactable = false;
        _format.blocksRaycasts = false;
        _add.interactable = false;
        _addLabel.color = D.Tertiary;
        _addIcon.color = D.Tertiary;
    }

    /// <summary>Clears the search field (the catalogue reopens on a fresh list).</summary>
    public void ResetSearch()
    {
        if (_search != null) _search.SetTextWithoutNotify("");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    static TextMeshProUGUI Paragraph(Transform parent, string name, float x, float y, float w, float h, float sizePx, Color colour,
        TextAlignmentOptions align)
    {
        var label = D.Text(parent, name, "", x, y, w, h, sizePx, D.Weight.Regular, colour, align);
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        label.lineSpacing = 8f;
        return label;
    }

}
