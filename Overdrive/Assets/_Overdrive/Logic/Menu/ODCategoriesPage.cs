using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Behaviour for the saved category directory; never constructs its UI at runtime.</summary>
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

    private void OnEnable()
    {
        Initialize();
        Filter(search != null ? search.text : "");
    }

    public void Initialize()
    {
        if (_bound || cards.Length == 0) return;
        _bound = true;
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
    }

    public void Filter(string query)
    {
        string term = (query ?? "").Trim();
        int visible = 0;
        foreach (var card in cards)
        {
            bool match = term.Length == 0 ||
                (card.searchTerms ?? "").IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
            if (card.root != null) card.root.SetActive(match);
            if (match) visible++;
        }
        if (noResults != null) noResults.SetActive(visible == 0);
        if (scroll != null)
        {
            if (scroll.content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
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
        if (icon != null) icon.color = _favorites[index] ? new Color(1f, .80f, .35f) : new Color(1f, 1f, 1f, .65f);
    }

    public void OpenF1()
    {
        if (menu == null || sport == null) return;
        sport.SetMode(ODSportPage.Mode.Weekend);
        menu.ShowSport();
    }

    public void OpenProfile()
    {
        if (menu != null) menu.SelectNav(2);
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
