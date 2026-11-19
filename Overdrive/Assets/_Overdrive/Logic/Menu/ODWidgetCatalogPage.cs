/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODWidgetCatalogPage - Widget catalogue, a window of its own like the
 ## race TV: the "Telemetry" button of the championship page (live and
 ## replay states) opens it on the left of the centre window (the TV when it
 ## is open, else the race window), at its height and facing the user
 ## (ODWindowLayout), the race keeps running, and its grab bar
 ## moves it anywhere while it is open. The close button (or "Telemetry"
 ## again) hides it in one action; leaving the race page hides it too.
 ## Holds the catalogue (categories, widgets), the selected category and the
 ## search, and hands them to ODWidgetCatalogView.
 ## No telemetry widget exists yet: every category is empty.
 ##
 */

using System.Collections.Generic;
using UnityEngine;

public sealed class ODWidgetCatalogPage : MonoBehaviour
{
    const float FadeSeconds = .18f;
    /// <summary>The catalogue is a secondary window: 2/3 of the race window's size, same layout.</summary>
    const float SizeRatio = 2f / 3f;

    readonly WidgetCatalog _catalog = DefaultCatalog();
    ODWidgetCatalogView _view;
    CanvasGroup _group;
    int _category = 1;   // "Car data", as in the design
    string _search = "";
    float _alpha;

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>
    /// Creates the (hidden) catalogue window: a world canvas of the design window size, at 2/3 of
    /// the race window's scale, with the same grab bar as the TV.
    /// </summary>
    public static ODWidgetCatalogPage Create(RectTransform raceWindow)
    {
        var canvas = ODPointableCanvas.Create("WidgetCatalogWindow", ODDesign.WindowSize);
        var go = canvas.gameObject;
        go.SetActive(false);
        go.transform.localScale = raceWindow.lossyScale * SizeRatio;
        go.AddComponent<WindowHandle>();

        var page = go.AddComponent<ODWidgetCatalogPage>();
        page._group = go.AddComponent<CanvasGroup>();
        page._view = go.AddComponent<ODWidgetCatalogView>();
        page._view.CloseClicked += page.Close;
        page._view.CategorySelected += page.SelectCategory;
        page._view.SearchChanged += page.Search;
        return page;
    }

    /// <summary>Shows the window left of the race window (wherever it was moved to), on a fresh search.</summary>
    public void Open()
    {
        ODWindowLayout.OpenOnLeft((RectTransform)transform);
        gameObject.SetActive(true);
        GetComponent<Canvas>().worldCamera = Camera.main;
        _search = "";
        _view.ResetSearch();
        Repaint();
        _alpha = 0f;
        _group.alpha = 0f;
    }

    /// <summary>Back to the race, in one action.</summary>
    public void Close() => gameObject.SetActive(false);

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    void OnEnable() => ODPassthroughBlur.Acquire(this);   // frosted room behind the glass

    void OnDisable() => ODPassthroughBlur.Release(this);

    void SelectCategory(int index)
    {
        if (index < 0 || index >= _catalog.categories.Count) return;
        _category = index;
        Repaint();
    }

    void Search(string text)
    {
        _search = text ?? "";
        Repaint();
    }

    void Repaint() => _view.Show(_catalog, _category, Filtered(), _search);

    List<WidgetDefinition> Filtered()
    {
        string term = _search.Trim().ToLowerInvariant();
        string id = _catalog.categories[_category].id;
        // Searching looks through every category; otherwise the selected one only.
        return _catalog.widgets.FindAll(w => term.Length > 0
            ? (w.name ?? "").ToLowerInvariant().Contains(term) || (w.description ?? "").ToLowerInvariant().Contains(term)
            : w.categoryId == id);
    }

    void Update()
    {
        if (_alpha >= 1f) return;
        _alpha = Mathf.Min(1f, _alpha + ODTime.Delta / FadeSeconds);
        _group.alpha = ODRuntimeUI.EaseOutCubic(_alpha);
    }

    /// <summary>Categories of the side bar. Widgets come with the telemetry ones (none yet).</summary>
    static WidgetCatalog DefaultCatalog() => new WidgetCatalog
    {
        categories = new List<WidgetCategory>
        {
            new WidgetCategory { id = "suggestions", name = "Suggestions", description = "Picked for this session" },
            new WidgetCategory { id = "car", name = "Car data", description = "The car of the followed driver" },
            new WidgetCategory { id = "tyres", name = "Tyres & strategy", description = "Compounds, stints and pit stops" },
            new WidgetCategory { id = "timing", name = "Timing", description = "Lap times and sectors" },
            new WidgetCategory { id = "race", name = "Race", description = "Gaps, positions and race events" },
        },
    };
}
