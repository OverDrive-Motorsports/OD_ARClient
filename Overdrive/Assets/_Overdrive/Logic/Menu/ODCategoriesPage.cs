using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Behaviour of the saved category directory, laid out as the design's catalogue
/// (Canva 02.2): caps line + "Championnats", Tous / Favoris / En direct filter and
/// a round search button, then the championship posters in a 3 x 2 grid.
/// </summary>
public sealed class ODCategoriesPage : MonoBehaviour
{
    [Serializable]
    public sealed class Card
    {
        public string id;
        public string searchTerms;
        public bool available;
        public GameObject root;
        public Button openButton;
        public Button favoriteButton;
        public ODNavigationIcon favoriteIcon;
    }

    public OverdriveMainMenu menu;
    public ODSportPage sport;
    public TMP_InputField search;
    public ScrollRect scroll;
    public Button profileButton;
    public GameObject noResults;
    public Card[] cards = Array.Empty<Card>();
    public bool persistFavorites = true;

    private bool _bound;
    private bool[] _favorites;
    private UnityAction[] _favoriteActions;
    private const string FavoritePrefix = "Overdrive.CategoryFavorite.";

    // Catalogue content, in card order (the design's order). Card ids stay the builder's
    // ("f1", "coming-N"); only the F1 card opens for now. Season figures are illustrative.
    private static readonly ODChampionshipTile.Info[] Championships =
    {
        Tile("f1",      "F1",  "Formule 1",       "Monoplaces · 24 Grands Prix",        "Manche 19 / 24",  "États-Unis · dans 11 j", .79f, false),
        Tile("motogp",  "MGP", "MotoGP",          "Grand Prix moto · 22 manches",       "Manche 19 / 22",  "Australie · ce week-end", .86f, true),
        Tile("wec",     "WEC", "World Endurance", "Prototypes & GT · 8 manches",        "Manche 7 / 8",    "Bahreïn · dans 12 j",    .88f, false),
        Tile("fe",      "FE",  "Formula E",       "Monoplaces électriques",             "Pré-saison",      "Tests · demain",         0f,   false),
        Tile("indycar", "IC",  "IndyCar",         "Ovales & circuits routiers",         "Saison terminée", "Reprise en mars",        1f,   false),
        Tile("wrc",     "WRC", "WRC",             "Championnat du monde des rallyes",   "Manche 13 / 14",  "Espagne · ce week-end",  .93f, false),
    };

    private static ODChampionshipTile.Info Tile(string id, string code, string name, string subtitle, string round, string next, float progress, bool live) =>
        new ODChampionshipTile.Info { id = id, code = code, name = name, subtitle = subtitle, round = round, next = next, progress = progress, live = live };

    private enum Scope { All, Favorites, Live }
    private Scope _scope;
    private ODSegmentedControl _filter;
    private bool _searching;

    private void OnEnable()
    {
        ODPassthroughBlur.Acquire(this);   // frosted room behind the glass (Quest 3, camera permission)
        Initialize();
        Filter(search != null ? search.text : "");
    }

    private void OnDisable() => ODPassthroughBlur.Release(this);

    public void Initialize()
    {
        if (_bound || cards.Length == 0) return;
        _bound = true;
        LayoutPage();
        _favorites = new bool[cards.Length];
        _favoriteActions = new UnityAction[cards.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            var card = cards[i];
            _favorites[i] = persistFavorites && PlayerPrefs.GetInt(FavoritePrefix + card.id, 0) == 1;
            _favoriteActions[i] = () => ToggleFavorite(index);
            card.favoriteButton?.onClick.AddListener(_favoriteActions[i]);
            if (card.available) card.openButton?.onClick.AddListener(OpenF1);
            ApplyFavorite(i);
        }
        search?.onValueChanged.AddListener(Filter);
        profileButton?.onClick.AddListener(OpenProfile);
        ApplyTheme();
    }

    // ── Design layout (design px, 1920 x 1200 window) ─────────────────────────

    private void LayoutPage()
    {
        var caps = transform.Find("Brand")?.GetComponent<TextMeshProUGUI>();
        if (caps != null)
        {
            ODDesign.Place(caps.transform, 72f, 60f, 900f, 30f);
            ODDesign.Style(caps, ODDesign.PageCaps, ODDesign.Weight.SemiBold, ODDesign.Secondary, caps: true);
            caps.text = $"Catalogue · {cards.Length} compétitions";
        }
        var title = transform.Find("Heading")?.GetComponent<TextMeshProUGUI>();
        if (title != null)
        {
            ODDesign.Place(title.transform, 72f, 94f, 900f, 74f);
            ODDesign.Style(title, ODDesign.TitleXL, ODDesign.Weight.Bold, ODDesign.Primary);
        }
        if (profileButton != null) profileButton.gameObject.SetActive(false);   // the tab bar holds Profil

        // Right of the header: filter + round search button; the search field takes the filter's place when open.
        const float headerBottom = 162f;
        var searchButton = ODDesign.IconButton(transform, "SearchButton", 1920f - 72f - 68f, headerBottom - 68f, 68f, ODNavigationIcon.Symbol.Search);
        searchButton.onClick.AddListener(ToggleSearch);
        _filter = ODSegmentedControl.Create(transform, ODDesign.Px(1920f - 72f - 68f - 16f - 470f), ODDesign.Px(headerBottom - 68f),
            ODDesign.Px(470f), ODDesign.Px(68f), new[] { "Tous", "Favoris", "En direct" }, 0, ODDesign.Px(22f));
        _filter.Changed += index => { _scope = (Scope)index; Filter(search != null ? search.text : ""); };

        if (search != null)
        {
            ODDesign.Place(search.transform, 1920f - 72f - 68f - 16f - 560f, headerBottom - 68f, 560f, 68f);
            if (search.targetGraphic is RoundedImage field) { field.cornerRadius = ODDesign.Px(34f); field.color = ODDesign.Recessed; }
            var o = search.GetComponent<Outline>();
            if (o != null) o.enabled = false;
            search.gameObject.SetActive(false);
        }

        if (scroll != null)
        {
            ODDesign.Place(scroll.transform, 72f, 202f, 1920f - 144f, 1200f - 202f - 64f);
            var grid = scroll.content != null ? scroll.content.GetComponent<GridLayoutGroup>() : null;
            if (grid != null)
            {
                grid.cellSize = new Vector2(ODDesign.Px(ODChampionshipTile.W), ODDesign.Px(ODChampionshipTile.H));
                grid.spacing = new Vector2(ODDesign.Px(32f), ODDesign.Px(32f));
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                grid.padding = new RectOffset(0, 0, 0, 0);
            }
            scroll.verticalScrollbar = null;
        }
        var track = transform.Find("ScrollTrack");
        if (track != null) track.gameObject.SetActive(false);
        if (noResults != null)
        {
            ODDesign.Place(noResults.transform, 72f, 520f, 1920f - 144f, 60f);
            var label = noResults.GetComponent<TextMeshProUGUI>();
            if (label != null) ODDesign.Style(label, 27f, ODDesign.Weight.SemiBold, ODDesign.Secondary, TextAlignmentOptions.Center);
        }
    }

    private void ToggleSearch()
    {
        _searching = !_searching;
        if (search != null)
        {
            search.gameObject.SetActive(_searching);
            if (_searching) search.ActivateInputField();
            else search.text = "";
        }
        if (_filter != null) _filter.gameObject.SetActive(!_searching);
    }

    /// <summary>Neutral glass window; each card becomes a championship tile in that championship's colours.</summary>
    private void ApplyTheme()
    {
        ODChampionshipTheme.ApplyBackground(GetComponent<ODHomepageSurface>(), ODChampionshipTheme.NeutralPalette);
        ODChampionshipTheme.ApplyNeutralButtons(transform);
        ODChampionshipTheme.ApplyTypography(transform);

        var heading = transform.Find("Heading")?.GetComponent<TextMeshProUGUI>();
        if (heading != null) heading.text = "Championnats";
        if (search != null && search.placeholder is TextMeshProUGUI hint) hint.text = "Rechercher un championnat";
        var empty = noResults != null ? noResults.GetComponent<TextMeshProUGUI>() : null;
        if (empty != null) empty.text = "Aucun championnat trouvé";

        // The 3-column grid runs slightly past the scroll viewport: widen its mask so the right column isn't cut.
        var mask = scroll != null && scroll.viewport != null ? scroll.viewport.GetComponent<RectMask2D>() : null;
        if (mask != null) mask.padding = new Vector4(-24f, -24f, -24f, -16f);

        for (int i = 0; i < cards.Length && i < Championships.Length; i++)
        {
            var card = cards[i];
            if (card.root == null) continue;
            var info = Championships[i];
            info.available = card.available;
            card.searchTerms = info.name + " " + info.code + " " + info.subtitle + (info.id == "f1" ? " Formula 1 F1" : "");
            ODChampionshipTile.Dress(card.root, info, i);
        }
        ODChampionshipTheme.ScaleType(transform);   // builder labels and tile faces, once
    }

    public void Filter(string query)
    {
        string term = (query ?? "").Trim();
        int visible = 0;
        for (int i = 0; i < cards.Length; i++)
        {
            var card = cards[i];
            bool match = term.Length == 0 ||
                (card.searchTerms ?? "").IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
            if (_scope == Scope.Favorites) match &= _favorites != null && i < _favorites.Length && _favorites[i];
            if (_scope == Scope.Live) match &= i < Championships.Length && Championships[i].live;
            if (card.root != null) card.root.SetActive(match);
            if (match) visible++;
        }
        if (noResults != null) noResults.SetActive(visible == 0);
        if (scroll != null)
        {
            if (scroll.content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
            // When the posters fit, the scroll view must not exist for the pointer: as the drag
            // handler under every poster, a slight ray shake during a press would cancel the click.
            scroll.enabled = scroll.content != null && scroll.viewport != null &&
                             scroll.content.rect.height > scroll.viewport.rect.height + 1f;
        }
    }

    public bool IsFavorite(int index)
    {
        Initialize();
        return _favorites != null && index >= 0 && index < _favorites.Length && _favorites[index];
    }

    public void ToggleFavorite(int index)
    {
        Initialize();
        if (_favorites == null || index < 0 || index >= cards.Length) return;
        _favorites[index] = !_favorites[index];
        if (persistFavorites)
        {
            PlayerPrefs.SetInt(FavoritePrefix + cards[index].id, _favorites[index] ? 1 : 0);
            PlayerPrefs.Save();
        }
        ApplyFavorite(index);
    }

    private void ApplyFavorite(int index)
    {
        var icon = cards[index].favoriteIcon;
        if (icon != null) icon.color = _favorites[index] ? ODDesign.Hex("F5D67E") : new Color(1f, 1f, 1f, .65f);
    }

    public void OpenF1()
    {
        if (menu == null || sport == null) return;
        sport.SetMode(ODSportPage.Mode.Weekend);
        menu.ShowSport();
    }

    public void OpenProfile()
    {
        if (menu != null) menu.ShowProfile();
    }

    private void OnDestroy()
    {
        if (!_bound) return;
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].favoriteButton?.onClick.RemoveListener(_favoriteActions[i]);
            if (cards[i].available) cards[i].openButton?.onClick.RemoveListener(OpenF1);
        }
        search?.onValueChanged.RemoveListener(Filter);
        profileButton?.onClick.RemoveListener(OpenProfile);
    }
}
