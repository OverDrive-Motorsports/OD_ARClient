using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Replay gallery, reached from the "Replays" button of the championship page.
/// Cards are generated from data — finished sessions from the backend, or the
/// mobile mocks once the user picks the demo data — in two sections: "Replays"
/// (everything available) and "Mes replays" (saved with the card's star, kept
/// on the headset), both filtered by the search field. Simple cards, no motion
/// (ODReplayTile). Picking one opens the championship page in its replay
/// state (the broadcast page).
/// </summary>
public sealed class ODReplayCatalog : MonoBehaviour
{
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public string grandPrix;
        public string session = "Course";
        public string season = "2024";
        public Button openButton;
    }

    public OverdriveMainMenu menu;
    public ODSportPage sport;
    public Button backButton;
    public ScrollRect scroll;
    public Entry[] entries = Array.Empty<Entry>();
    [Tooltip("Resolved by child name (\"Subtitle\") when left empty.")]
    public TextMeshProUGUI subtitle;
    [Tooltip("Resolved by child name (\"DemoNotice\") when left empty.")]
    public TextMeshProUGUI notice;

    private bool _bound;
    private readonly List<GameObject> _generated = new List<GameObject>();
    private int _loadVersion;

    private void OnEnable()
    {
        ODPassthroughBlur.Acquire(this);   // frosted room behind the glass (Quest 3, camera permission)
        Initialize();
        if (scroll != null)
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }
        Populate();
    }

    private void OnDisable()
    {
        ODPassthroughBlur.Release(this);
        _loadVersion++;
    }

    public void Initialize()
    {
        if (_bound) return;
        _bound = true;
        if (subtitle == null) subtitle = transform.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
        if (notice == null) notice = transform.Find("DemoNotice")?.GetComponent<TextMeshProUGUI>();

        // The prefab's demo cards are hidden: cards are built from data (ODReplayTile).
        foreach (var entry in entries)
            if (entry?.openButton != null) entry.openButton.gameObject.SetActive(false);
        entries = Array.Empty<Entry>();

        backButton?.onClick.AddListener(BackToSport);
        LayoutPage();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Data — backend or mobile mocks
    // ═══════════════════════════════════════════════════════════════════════

    private void Populate()
    {
        ApplyTheme(ODDataSource.UseMocks ? "formula_1" : ODApiConfig.Instance.championshipCode);
        if (ODDataSource.UseMocks) Bind(ODReplayMockData.Catalog, demo: true);
        else LoadFromBackend();
    }

    private async void LoadFromBackend()
    {
        int version = ++_loadVersion;
        SetLabel(notice, "Chargement des replays…");
        try
        {
            var replays = await ODChampionshipDataService.LoadReplaysAsync();
            if (this == null || version != _loadVersion || ODDataSource.UseMocks) return;

            if (replays.Count == 0)
            {
                ShowError("Aucun replay", "Le backend répond mais ne contient aucune course terminée. Lance une ingestion OpenF1, ou affiche les données de démo.");
                return;
            }
            Bind(replays, demo: false);
        }
        catch (Exception error)
        {
            if (this == null || version != _loadVersion) return;
            Debug.LogWarning("[ODReplayCatalog] Backend load failed: " + error);
            ShowError(ODChampionshipDataService.Title(error), ODChampionshipDataService.Describe(error));
        }
    }

    private void ShowError(string title, string message)
    {
        SetLabel(notice, "");
        ODBackendErrorPopup.Show((RectTransform)transform, title, message,
            onRetry: LoadFromBackend,
            onUseMocks: () =>
            {
                ODDataSource.UseMocks = true;
                _loadVersion++;
                Bind(ODReplayMockData.Catalog, demo: true);
            });
    }

    private void Bind(List<ReplaySummary> replays, bool demo)
    {
        _replays = replays.OrderByDescending(r => r.date).ToList();   // most recent first
        entries = new Entry[_replays.Count];
        for (int i = 0; i < _replays.Count; i++)
            entries[i] = new Entry
            {
                id        = _replays[i].id,
                grandPrix = _replays[i].grandPrix,
                session   = _replays[i].sessionName,
                season    = _replays[i].season,
            };

        if (_replays.Count > 0) ApplyTheme(_replays[0].championshipId);
        string championship = _replays.Count > 0 ? ChampionshipLabel(_replays[0]) : "Formule 1";
        SetLabel(subtitle, $"{championship} · {_replays.Count} replay{(_replays.Count > 1 ? "s" : "")} disponible{(_replays.Count > 1 ? "s" : "")}");
        SetLabel(notice, demo ? "Données de démonstration (OD_Mobile)" : "Données du backend · Vidéos non connectées");
        Render();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Sections — "Replays" (everything available) and "Mes replays" (saved
    // with the star, kept on the headset: the backend has no user list yet)
    // ═══════════════════════════════════════════════════════════════════════

    const string SavedKey = "od.replays.mine";

    private List<ReplaySummary> _replays = new List<ReplaySummary>();
    private HashSet<string> _saved;

    private HashSet<string> Saved
    {
        get
        {
            if (_saved == null)
                _saved = new HashSet<string>((PlayerPrefs.GetString(SavedKey, "") ?? "")
                    .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
            return _saved;
        }
    }

    private void ToggleSaved(string id)
    {
        if (!Saved.Remove(id)) Saved.Add(id);
        PlayerPrefs.SetString(SavedKey, string.Join("\n", Saved));
        PlayerPrefs.Save();
        Render();
    }

    /// <summary>Rebuilds both sections for the current data, search text and saved list.</summary>
    private void Render()
    {
        UnwireEntries();
        foreach (var item in _generated) if (item != null) Destroy(item);
        _generated.Clear();
        var content = scroll != null ? scroll.content : (RectTransform)transform;

        string term = _search != null ? _search.text.Trim() : "";
        var matches = new List<int>();
        for (int i = 0; i < _replays.Count; i++)
            if (Matches(_replays[i], term)) matches.Add(i);
        var mine = matches.Where(i => Saved.Contains(_replays[i].id)).ToList();

        float y = 0f;
        y = Section(content, "Replays", matches, y,
            term.Length > 0 ? "Aucun replay ne correspond à ta recherche." : "Aucun replay disponible pour le moment.");
        y += SectionGap;
        y = Section(content, "Mes replays", mine, y,
            term.Length > 0 ? "Aucun de tes replays ne correspond à ta recherche."
                            : "Aucun replay enregistré. Touche l'étoile d'un replay pour le retrouver ici.");

        content.sizeDelta = new Vector2(content.sizeDelta.x, ODDesign.Px(y));
        if (scroll != null)
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
            // No scroll view under the ray when everything fits (a drag would steal the click).
            scroll.enabled = scroll.viewport != null && content.rect.height > scroll.viewport.rect.height + 1f;
        }
    }

    /// <summary>Section title, then its cards in rows of three (or a muted line when empty). Returns the bottom y.</summary>
    private float Section(RectTransform content, string title, List<int> indices, float y, string empty)
    {
        var label = ODDesign.Text(content, "Section", title, 0f, y, 900f, 44f, ODDesign.Title2, ODDesign.Weight.SemiBold);
        _generated.Add(label.gameObject);
        y += 44f + 20f;

        if (indices.Count == 0)
        {
            var hint = ODDesign.Text(content, "Empty", empty, 0f, y, ContentW, 30f, 22f, ODDesign.Weight.Regular, ODDesign.Muted);
            _generated.Add(hint.gameObject);
            return y + 30f;
        }

        for (int n = 0; n < indices.Count; n++)
        {
            int index = indices[n];
            ReplaySummary replay = _replays[index];
            var tile = ODReplayTile.Card(content, TileInfo(replay, Saved.Contains(replay.id)), CardW, CardH);
            ODDesign.Place(tile.transform, (n % 3) * (CardW + CardGap), y + (n / 3) * (CardH + CardGap), CardW, CardH);
            _generated.Add(tile.gameObject);

            UnityAction open = () => OpenReplay(index);
            tile.Button.onClick.AddListener(open);
            _wired.Add((tile.Button, open));
            string id = replay.id;
            tile.SaveButton.onClick.AddListener(() => ToggleSaved(id));
            if (entries[index].openButton == null) entries[index].openButton = tile.Button;
        }
        int rows = (indices.Count + 2) / 3;
        return y + rows * CardH + (rows - 1) * CardGap;
    }

    private static bool Matches(ReplaySummary replay, string term)
    {
        if (string.IsNullOrEmpty(term)) return true;
        string haystack = string.Join(" ", replay.grandPrix, replay.sessionName, replay.season, replay.circuit,
            replay.date != default ? replay.date.ToString("dd/MM/yyyy") : "");
        return Fold(haystack).Contains(Fold(term));
    }

    /// <summary>Lower case without accents: "bahrein" finds "Bahreïn".</summary>
    private static string Fold(string text)
    {
        var decomposed = (text ?? "").ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString();
    }

    private static ODReplayTile.Info TileInfo(ReplaySummary replay, bool saved) => new ODReplayTile.Info
    {
        title  = replay.grandPrix,
        detail = replay.date != default ? $"{replay.sessionName} · {replay.date:dd/MM/yyyy}" : replay.sessionName,
        saved  = saved,
    };

    /// <summary>French name of the championship whatever the source ("Formula 1" in the mocks).</summary>
    private static string ChampionshipLabel(ReplaySummary replay) =>
        ODDesign.Normalise(replay.championshipId) == "f1" ? "Formule 1" : replay.championshipName ?? "";

    // ── Design layout (design px, 1920 x 1200 window) ──────────────────────
    // Header like the championship pages with a search field on the right,
    // then the two sections in one vertical scroll.

    const float GX = 72f, ContentTop = 202f, ContentW = 1920f - 144f, ContentBottom = 1200f - 64f;
    const float CardW = 570.67f, CardH = 160f, CardGap = 32f, SectionGap = 52f;
    const float HeaderMid = 162f - 34f, BadgeW = 120f, SearchW = 520f;

    private TMP_InputField _search;

    private void LayoutPage()
    {
        const float x = GX + 68f + 24f;
        if (backButton != null)
        {
            ODDesign.Place(backButton.transform, GX, HeaderMid - 34f, 68f, 68f);
            if (backButton.targetGraphic is RoundedImage round) round.cornerRadius = ODDesign.Px(34f);
            ODDesign.ApplyButton(backButton, ODDesign.ButtonKind.Icon);
            var icon = backButton.GetComponentInChildren<ODNavigationIcon>(true);
            if (icon != null) { ODDesign.Place(icon.transform, 19f, 19f, 30f, 30f); icon.color = Color.white; }
        }
        if (subtitle != null)
        {
            ODDesign.Place(subtitle.transform, x, 60f, 1000f, 30f);
            ODDesign.Style(subtitle, ODDesign.PageCaps, ODDesign.Weight.SemiBold, ODDesign.Secondary, caps: true);
        }
        var heading = transform.Find("Heading")?.GetComponent<TextMeshProUGUI>();
        if (heading != null)
        {
            ODDesign.Place(heading.transform, x, 97f, 900f, 72f);
            ODDesign.Style(heading, ODDesign.TitleXL, ODDesign.Weight.Bold, ODDesign.Primary);
            heading.text = "Replays";
        }
        var badge = transform.Find("SportBadge");
        if (badge != null)
        {
            ODDesign.Place(badge, 1920f - GX - BadgeW, HeaderMid - 32f, BadgeW, 64f);   // centred on the back button
            if (badge.GetComponent<RoundedImage>() is RoundedImage pill) pill.cornerRadius = ODDesign.Px(32f);
            var label = badge.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                var r = label.rectTransform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                ODDesign.Style(label, ODDesign.CapsSize, ODDesign.Weight.Bold, Color.white, TextAlignmentOptions.Center, trackingEm: .06f);
            }
        }
        _search = BuildSearch(1920f - GX - BadgeW - 16f - SearchW, HeaderMid - 32f, SearchW, 64f);
        _search.onValueChanged.AddListener(_ => Render());

        if (scroll != null)
        {
            ODDesign.Place(scroll.transform, GX, ContentTop, ContentW, ContentBottom - ContentTop);
            scroll.verticalScrollbar = null;
            if (scroll.viewport != null)
            {
                // Viewport = the whole scroll area (the prefab's insets cut the third column).
                scroll.viewport.anchorMin = Vector2.zero;
                scroll.viewport.anchorMax = Vector2.one;
                scroll.viewport.offsetMin = scroll.viewport.offsetMax = Vector2.zero;
            }
            var content = scroll.content;
            if (content != null)
            {
                // Sections are placed by hand: no grid / size fitter driving the content.
                var grid = content.GetComponent<GridLayoutGroup>();
                if (grid != null) grid.enabled = false;
                var fitter = content.GetComponent<ContentSizeFitter>();
                if (fitter != null) fitter.enabled = false;
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(.5f, 1f);
                content.anchoredPosition = Vector2.zero;
                content.offsetMin = new Vector2(0f, content.offsetMin.y);
                content.offsetMax = new Vector2(0f, 0f);
            }
        }
        var track = transform.Find("ScrollTrack");
        if (track != null) track.gameObject.SetActive(false);
        if (notice != null)
        {
            ODDesign.Place(notice.transform, GX, ContentBottom + 14f, 1200f, 30f);
            ODDesign.Style(notice, 18f, ODDesign.Weight.Regular, ODDesign.Tertiary);
        }
    }

    /// <summary>Recessed capsule with a search glyph and a TMP field (the Quest system keyboard types into it).</summary>
    private TMP_InputField BuildSearch(float x, float y, float w, float h)
    {
        var field = ODDesign.Fill(transform, "Search", x, y, w, h, ODDesign.Recessed, h * .5f);
        field.raycastTarget = true;
        field.gameObject.SetActive(false);   // configure the input field before its OnEnable

        var icon = ODDesign.Rect(field.transform, "Icon", 24f, (h - 26f) * .5f, 26f, 26f).gameObject.AddComponent<ODNavigationIcon>();
        icon.symbol = ODNavigationIcon.Symbol.Search;
        icon.color = ODDesign.Secondary;
        icon.raycastTarget = false;

        var area = ODDesign.Rect(field.transform, "TextArea", 64f, 0f, w - 64f - 28f, h);
        area.gameObject.AddComponent<RectMask2D>();
        var placeholder = ODDesign.Text(area, "Placeholder", "Rechercher un replay", 0f, 0f, w - 92f, h, 22f, ODDesign.Weight.Regular, ODDesign.Tertiary);
        var text = ODDesign.Text(area, "Text", "", 0f, 0f, w - 92f, h, 22f, ODDesign.Weight.Regular, ODDesign.Primary);
        text.overflowMode = TextOverflowModes.Overflow;

        var input = field.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = field;
        input.textViewport = area;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.fontAsset = text.font;
        input.pointSize = text.fontSize;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 40;
        input.caretColor = Color.white;
        input.customCaretColor = true;
        input.selectionColor = new Color(1f, 1f, 1f, .25f);
        var colours = input.colors;
        colours.normalColor = Color.white;
        colours.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colours.pressedColor = colours.selectedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        colours.fadeDuration = 0f;   // no motion on the replay pages
        input.colors = colours;
        field.gameObject.SetActive(true);
        return input;
    }

    /// <summary>Championship gradient behind the gallery, accent-tinted sport badge (tiles dress themselves).</summary>
    private void ApplyTheme(string championshipId)
    {
        var palette = ODChampionshipTheme.For(championshipId);
        ODChampionshipTheme.ApplyBackground(GetComponent<ODHomepageSurface>(), palette);
        ODChampionshipTheme.ApplyGlassCards(transform);
        ODChampionshipTheme.ApplyNeutralButtons(transform);
        ODChampionshipTheme.ApplyTypography(transform);
        ODChampionshipTheme.ScaleType(transform);

        var badge = transform.Find("SportBadge")?.GetComponent<Graphic>();
        if (badge != null) badge.color = new Color(palette.accent.r, palette.accent.g, palette.accent.b, .32f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Navigation
    // ═══════════════════════════════════════════════════════════════════════

    public void OpenReplay(int index)
    {
        if (index < 0 || index >= entries.Length || menu == null || sport == null) return;
        sport.SelectReplay(entries[index]);
        menu.ShowSport();
    }

    /// <summary>Back to the championship page, in its own state (not the replay that may have been open).</summary>
    public void BackToSport()
    {
        sport?.LeaveReplay();
        if (menu != null) menu.ShowSport();
    }

    private readonly List<(Button button, UnityAction action)> _wired = new List<(Button, UnityAction)>();

    private void UnwireEntries()
    {
        foreach (var (button, action) in _wired)
            if (button != null) button.onClick.RemoveListener(action);
        _wired.Clear();
        foreach (var entry in entries) if (entry != null) entry.openButton = null;
    }

    private static void SetLabel(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value;
    }

    private void OnDestroy()
    {
        if (!_bound) return;
        UnwireEntries();
        backButton?.onClick.RemoveListener(BackToSport);
    }
}
