using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Universal command-bar search for Overdrive.
/// Auto-wires to the SearchBar + SearchOverlay built by BuildSearchPanel.
/// Filters a mock dataset live, grouped by category, with an empty-state
/// (recent searches + suggestions). Designed as an MVP to iterate on.
/// </summary>
public class SearchController : MonoBehaviour
{
    // ── data model ─────────────────────────────────────────────────────────
    public enum Kind { Race, Driver, Team, Circuit, Season }

    [System.Serializable]
    public class Entry
    {
        public string title;
        public string subtitle;
        public Kind kind;
        public Color accent;
        public Entry(string t, string s, Kind k, Color c) { title = t; subtitle = s; kind = k; accent = c; }
    }

    // ── palette — pulled from UITheme.Instance in Start() via PullTheme() so
    // runtime-built rows match the rest of the app instead of their own brand. ──
    static Color RowBg;
    static Color RowHover;
    static Color Gold;
    static Color Txt;
    static Color Sub;
    static Color ChipOn;
    static Color ChipOff;

    static Color Ferrari;
    static Color RedBull;
    static Color Merc;
    static Color McLaren;
    // Aston/Neutral aren't in UITheme (only the 4 teams already used elsewhere are) — literals stay.
    static readonly Color Aston = new Color(0.00f, 0.45f, 0.40f, 1f);
    static readonly Color Neutral = new Color(0.55f, 0.55f, 0.60f, 1f);

    static void PullTheme()
    {
        UITheme theme = UITheme.Instance;
        if (theme == null) return;

        RowBg = theme.panelBackgroundAlt;
        RowHover = theme.surfaceColor;
        Gold = theme.accentGold;
        Txt = theme.textPrimary;
        Sub = theme.textSecondary;
        ChipOn = theme.accentGold;
        ChipOff = theme.panelBackground;

        Ferrari = theme.teamFerrari;
        RedBull = theme.teamRedBull;
        Merc = theme.teamMercedes;
        McLaren = theme.teamMcLaren;
    }

    // ── wired at runtime (auto-found) ───────────────────────────────────────
    private TMP_InputField _input;
    private GameObject _overlay;
    private CanvasGroup _overlayGroup;
    private Coroutine _overlayRoutine;
    private GameObject _emptyState;
    private GameObject _resultsScroll;
    private Transform _resultsContent;
    private Transform _recentContent;
    private Transform _suggestContent;
    private TextMeshProUGUI _noResultsLabel;

    private readonly Button[] _chips = new Button[6];
    private readonly TextMeshProUGUI[] _chipLabels = new TextMeshProUGUI[6];
    private readonly string[] _chipNames = { "All", "Races", "Drivers", "Teams", "Circuits", "Seasons" };

    private int _activeChip = 0; // 0 = All
    private readonly List<Entry> _data = new List<Entry>();
    private readonly List<string> _recent = new List<string> { "Verstappen", "Monaco GP", "Ferrari" };

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        PullTheme();
        BuildDataset();
        AutoWire();

        if (_input != null)
        {
            _input.onValueChanged.AddListener(OnValueChanged);
            _input.onSelect.AddListener(_ => Open());
        }

        for (int i = 0; i < _chips.Length; i++)
        {
            int idx = i;
            if (_chips[i] != null) _chips[i].onClick.AddListener(() => SetChip(idx));
        }

        if (_overlay != null) _overlay.SetActive(false);
    }

    // ── open / close ──────────────────────────────────────────────────────────
    public void Open()
    {
        if (_overlay == null || _overlay.activeSelf) { Refresh(); return; }

        _overlay.SetActive(true);
        if (_overlayRoutine != null) StopCoroutine(_overlayRoutine);
        _overlayRoutine = StartCoroutine(UITransitions.FadeScaleIn(_overlayGroup, _overlay.transform));
        Refresh();
    }

    public void Close()
    {
        if (_overlay == null || !_overlay.activeSelf) return;
        if (_overlayRoutine != null) StopCoroutine(_overlayRoutine);
        _overlayRoutine = StartCoroutine(CloseOverlayRoutine());
    }

    private IEnumerator CloseOverlayRoutine()
    {
        yield return UITransitions.FadeScaleOut(_overlayGroup, _overlay.transform);
        _overlay.SetActive(false);
    }

    public void ToggleFromBar()
    {
        if (_overlay == null) return;
        if (_overlay.activeSelf) Close(); else Open();
    }

    // ── input / chips ──────────────────────────────────────────────────────────
    private void OnValueChanged(string _)
    {
        if (!_overlay.activeSelf) Open();
        else Refresh();
    }

    private void SetChip(int idx)
    {
        _activeChip = idx;
        for (int i = 0; i < _chips.Length; i++)
        {
            if (_chips[i] == null) continue;
            var img = _chips[i].GetComponent<Image>();
            if (img) img.color = (i == idx) ? ChipOn : ChipOff;
            if (_chipLabels[i]) _chipLabels[i].color = (i == idx) ? Color.black : Txt;
        }
        Refresh();
    }

    // ── refresh results ─────────────────────────────────────────────────────────
    private void Refresh()
    {
        string q = _input != null ? _input.text.Trim() : "";

        bool empty = string.IsNullOrEmpty(q);
        if (_emptyState != null) _emptyState.SetActive(empty);
        if (_resultsScroll != null) _resultsScroll.SetActive(!empty);

        if (empty) { PopulateRecents(); return; }

        ClearChildren(_resultsContent);

        int shown = 0;
        foreach (var e in _data)
        {
            if (!MatchesChip(e.kind)) continue;
            if (!Contains(e.title, q) && !Contains(e.subtitle, q)) continue;
            BuildResultRow(_resultsContent, e);
            shown++;
        }

        if (_noResultsLabel != null)
            _noResultsLabel.gameObject.SetActive(shown == 0);
    }

    private bool MatchesChip(Kind k)
    {
        switch (_activeChip)
        {
            case 1: return k == Kind.Race;
            case 2: return k == Kind.Driver;
            case 3: return k == Kind.Team;
            case 4: return k == Kind.Circuit;
            case 5: return k == Kind.Season;
            default: return true; // All
        }
    }

    private static bool Contains(string hay, string needle)
        => !string.IsNullOrEmpty(hay) &&
           hay.ToLowerInvariant().Contains(needle.ToLowerInvariant());

    // ── empty state (recent searches) ─────────────────────────────────────────
    private void PopulateRecents()
    {
        if (_recentContent == null) return;
        ClearChildren(_recentContent);
        foreach (var r in _recent)
            BuildRecentChip(_recentContent, r);
    }

    // ── row builders (runtime) ────────────────────────────────────────────────
    private void BuildResultRow(Transform parent, Entry e)
    {
        var row = NewGO("Row_" + e.title, parent);
        var bg = Rounded(row, RowBg, 12f);
        bg.raycastTarget = true;
        row.AddComponent<LayoutElement>().preferredHeight = 56f;

        var btn = row.AddComponent<Button>(); btn.targetGraphic = bg;
        btn.onClick.AddListener(() => OnResultClicked(e));

        var hover = row.AddComponent<RowHoverTint>();
        hover.Init(bg, RowBg, RowHover);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(14, 14, 0, 0); hlg.spacing = 14;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
        hlg.childControlHeight = true; hlg.childForceExpandHeight = true;

        // accent tag
        var tag = NewGO("Accent", row.transform);
        Rounded(tag, e.accent, 6f);
        FixedW(tag, 10f);

        // text block
        var block = NewGO("Text", row.transform);
        block.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var bvlg = block.AddComponent<VerticalLayoutGroup>();
        bvlg.spacing = 1; bvlg.childAlignment = TextAnchor.MiddleLeft;
        bvlg.childControlWidth = true; bvlg.childForceExpandWidth = true;
        bvlg.childControlHeight = false; bvlg.childForceExpandHeight = false;
        var t = Label(block.transform, e.title, 17f, FontStyles.Bold, Txt, TextAlignmentOptions.MidlineLeft);
        t.AddComponent<LayoutElement>().preferredHeight = 24f;
        var s = Label(block.transform, e.subtitle, 13f, FontStyles.Normal, Sub, TextAlignmentOptions.MidlineLeft);
        s.AddComponent<LayoutElement>().preferredHeight = 18f;

        // type label (right)
        var ty = Label(row.transform, e.kind.ToString().ToUpper(), 12f, FontStyles.Bold, Gold, TextAlignmentOptions.MidlineRight);
        FixedW(ty, 80f);
    }

    private void BuildRecentChip(Transform parent, string text)
    {
        var chip = NewGO("Recent_" + text, parent);
        var bg = Rounded(chip, ChipOff, 16f);
        chip.AddComponent<LayoutElement>().preferredHeight = 40f;
        var btn = chip.AddComponent<Button>(); btn.targetGraphic = bg;
        btn.onClick.AddListener(() => { if (_input) _input.text = text; });

        var hlg = chip.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(14, 16, 0, 0); hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
        hlg.childControlHeight = true; hlg.childForceExpandHeight = true;

        var icon = Label(chip.transform, "↻", 16f, FontStyles.Normal, Sub, TextAlignmentOptions.Center);
        FixedW(icon, 18f);
        var t = Label(chip.transform, text, 15f, FontStyles.Normal, Txt, TextAlignmentOptions.MidlineLeft);
        t.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private void OnResultClicked(Entry e)
    {
        Debug.Log($"[Search] Selected {e.kind}: {e.title}");
        if (!_recent.Contains(e.title))
        {
            _recent.Insert(0, e.title);
            if (_recent.Count > 5) _recent.RemoveAt(_recent.Count - 1);
        }
        // TODO: route to detail view / play stream
    }

    // ── dataset ────────────────────────────────────────────────────────────────
    private void BuildDataset()
    {
        // Races
        _data.Add(new Entry("Monaco GP 2024", "Round 8 · Monte Carlo", Kind.Race, Ferrari));
        _data.Add(new Entry("Bahrain GP 2024", "Round 1 · Sakhir", Kind.Race, RedBull));
        _data.Add(new Entry("Japan GP 2024", "Round 4 · Suzuka", Kind.Race, RedBull));
        _data.Add(new Entry("Silverstone 2023", "British GP · Round 10", Kind.Race, McLaren));
        _data.Add(new Entry("Las Vegas GP 2023", "Round 21 · Strip Circuit", Kind.Race, Merc));
        _data.Add(new Entry("Miami GP 2024", "Round 6 · Miami", Kind.Race, McLaren));

        // Drivers
        _data.Add(new Entry("Max Verstappen", "Red Bull Racing · #1", Kind.Driver, RedBull));
        _data.Add(new Entry("Lewis Hamilton", "Mercedes · #44", Kind.Driver, Merc));
        _data.Add(new Entry("Charles Leclerc", "Ferrari · #16", Kind.Driver, Ferrari));
        _data.Add(new Entry("Lando Norris", "McLaren · #4", Kind.Driver, McLaren));
        _data.Add(new Entry("Fernando Alonso", "Aston Martin · #14", Kind.Driver, Aston));
        _data.Add(new Entry("Carlos Sainz", "Ferrari · #55", Kind.Driver, Ferrari));

        // Teams
        _data.Add(new Entry("Red Bull Racing", "Constructor", Kind.Team, RedBull));
        _data.Add(new Entry("Ferrari", "Constructor", Kind.Team, Ferrari));
        _data.Add(new Entry("Mercedes", "Constructor", Kind.Team, Merc));
        _data.Add(new Entry("McLaren", "Constructor", Kind.Team, McLaren));
        _data.Add(new Entry("Aston Martin", "Constructor", Kind.Team, Aston));

        // Circuits
        _data.Add(new Entry("Circuit de Monaco", "Monte Carlo · 3.337 km", Kind.Circuit, Neutral));
        _data.Add(new Entry("Spa-Francorchamps", "Belgium · 7.004 km", Kind.Circuit, Neutral));
        _data.Add(new Entry("Suzuka Circuit", "Japan · 5.807 km", Kind.Circuit, Neutral));
        _data.Add(new Entry("Silverstone Circuit", "UK · 5.891 km", Kind.Circuit, Neutral));

        // Seasons
        _data.Add(new Entry("2024 Season", "24 races", Kind.Season, Gold));
        _data.Add(new Entry("2023 Season", "22 races", Kind.Season, Gold));
        _data.Add(new Entry("2022 Season", "22 races", Kind.Season, Gold));
    }

    // ── auto-wire ────────────────────────────────────────────────────────────
    private void AutoWire()
    {
        _input = Find("SearchBar")?.GetComponent<TMP_InputField>();

        var overlayT = transform.Find("SearchOverlay");
        if (overlayT == null) { Debug.LogError("[Search] SearchOverlay not found"); return; }
        _overlay = overlayT.gameObject;
        _overlayGroup = _overlay.GetComponent<CanvasGroup>();
        if (_overlayGroup == null) _overlayGroup = _overlay.AddComponent<CanvasGroup>();

        _emptyState = overlayT.Find("EmptyState")?.gameObject;
        _resultsScroll = overlayT.Find("Results")?.gameObject;
        _resultsContent = overlayT.Find("Results/Viewport/Content");
        _recentContent = overlayT.Find("EmptyState/RecentList");
        _suggestContent = overlayT.Find("EmptyState/Suggestions");
        _noResultsLabel = overlayT.Find("Results/NoResults")?.GetComponent<TextMeshProUGUI>();

        for (int i = 0; i < _chipNames.Length; i++)
        {
            var c = overlayT.Find("Chips/Chip_" + _chipNames[i]);
            if (c != null)
            {
                _chips[i] = c.GetComponent<Button>();
                _chipLabels[i] = c.Find("Label")?.GetComponent<TextMeshProUGUI>();
            }
        }
        SetChip(0);
    }

    private Transform Find(string name)
    {
        var t = transform.Find(name);
        if (t == null && transform.parent != null) t = transform.parent.Find(name);
        return t;
    }

    // ── primitives ─────────────────────────────────────────────────────────────
    private static GameObject NewGO(string n, Transform p)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static RoundedImage Rounded(GameObject go, Color c, float r)
    {
        var img = go.AddComponent<RoundedImage>();
        img.color = c; img.cornerRadius = r; img.cornerSegments = 10;
        img.raycastTarget = false;
        return img;
    }

    private static GameObject Label(Transform p, string txt, float size,
        FontStyles style, Color color, TextAlignmentOptions align)
    {
        var go = NewGO("Label", p);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = size; t.fontStyle = style;
        t.color = color; t.alignment = align; t.raycastTarget = false;
        return go;
    }

    private static void FixedW(GameObject go, float w)
    {
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = w; le.preferredWidth = w; le.flexibleWidth = 0f;
    }

    private static void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }
}

/// <summary>Lightweight pointer hover tint for result rows.</summary>
public class RowHoverTint : MonoBehaviour,
    UnityEngine.EventSystems.IPointerEnterHandler,
    UnityEngine.EventSystems.IPointerExitHandler
{
    private Graphic _g; private Color _normal, _hover;
    public void Init(Graphic g, Color normal, Color hover) { _g = g; _normal = normal; _hover = hover; }
    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData _) { if (_g) _g.color = _hover; }
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData _) { if (_g) _g.color = _normal; }
}
