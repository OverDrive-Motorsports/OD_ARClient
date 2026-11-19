/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODWidgetCatalogPage - Widget catalogue, a window of its own like the
 ## race TV: the "Telemetry" button of the championship page (live and
 ## replay states) opens it right in front of the user, closer than the
 ## other windows (Distance) and centred on where they look, at eye height;
 ## the windows behind stay where they are (no ODWindowLayout), the race
 ## keeps running. It has no window bar: it is not moved, it reopens in
 ## front of the user.
 ## A recentre (Meta button) brings it back in front too. The close button (or "Telemetry"
 ## again) hides it in one action; leaving the race page hides it too.
 ## Holds the catalogue (categories, the telemetry widgets of Widgets), the
 ## selected category, the search, the picked widget and its format, and
 ## hands them to ODWidgetCatalogView.
 ## Previews are still pictures in the Small format: one sample of the feed,
 ## taken when the catalogue is first opened, painted once (ShowStill).
 ## "Add to my space" puts the picked widget, in the picked format, in the
 ## user's space (ODWidgetSpace), where it is live and can be moved.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ODWidgetCatalogPage : MonoBehaviour
{
    const float FadeSeconds = .18f;
    /// <summary>Metres from the headset: closer than the other windows (ODWindowPlacement.Distance, 1.3).</summary>
    const float Distance = .9f;
    /// <summary>Scale against the race window: about the size it had at 1.3 m (2/3), brought closer.</summary>
    const float SizeRatio = .5f;

    /// <summary>UI type of each widget of the catalogue, by id. A new widget: a line here and its definition below.</summary>
    static readonly Dictionary<string, Type> Widgets = new()
    {
        { "speed", typeof(ODSpeedWidget) },
        { "pedals", typeof(ODPedalsWidget) },
        { "sectors", typeof(ODSectorsWidget) },
        { "laptime", typeof(ODLapTimeWidget) },
    };

    readonly WidgetCatalog _catalog = DefaultCatalog();
    ODWidgetCatalogView _view;
    ODWidgetSpace _space;
    CanvasGroup _group;
    int _category = 1;   // "Car data", as in the design
    string _search = "";
    string _picked;
    WidgetFormat _format = WidgetFormat.Small;
    bool _justAdded;
    TelemetryFrame _sample;
    float _alpha;

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>
    /// Creates the (hidden) catalogue window: a world canvas of the design window size, at 2/3 of
    /// the race window's scale, without a window bar. Added widgets go to <paramref name="space"/>.
    /// </summary>
    public static ODWidgetCatalogPage Create(RectTransform raceWindow, ODWidgetSpace space)
    {
        var canvas = ODPointableCanvas.Create("WidgetCatalogWindow", ODDesign.WindowSize);
        var go = canvas.gameObject;
        go.SetActive(false);
        go.transform.localScale = raceWindow.lossyScale * SizeRatio;

        var page = go.AddComponent<ODWidgetCatalogPage>();
        page._group = go.AddComponent<CanvasGroup>();
        page._space = space;
        if (space != null) space.Changed += page.SyncSpace;
        page._view = go.AddComponent<ODWidgetCatalogView>();
        page._view.BuildPreview = page.BuildPreview;
        page._view.CloseClicked += page.Close;
        page._view.CategorySelected += page.SelectCategory;
        page._view.SearchChanged += page.Search;
        page._view.WidgetSelected += page.Pick;
        page._view.FormatChanged += page.SelectFormat;
        page._view.AddClicked += page.AddToSpace;
        return page;
    }

    /// <summary>Shows the window in front of the user, on a fresh search; the other windows don't move.</summary>
    public void Open()
    {
        PlaceInFront();
        gameObject.SetActive(true);
        GetComponent<Canvas>().worldCamera = Camera.main;
        _search = "";
        _justAdded = false;
        _view.ResetSearch();
        SyncCounts();
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

    /// <summary>Centred on the headset's gaze (yaw only), at eye height, Distance away, facing the user.</summary>
    void PlaceInFront()
    {
        var head = Camera.main != null ? Camera.main.transform : null;
        if (head == null) return;
        var pose = ODWindowPlacement.RootPose(transform, (RectTransform)transform, ODWindowPlacement.Arc.InFront(head, Distance).At(0f));
        transform.SetPositionAndRotation(pose.position, pose.rotation);
    }

    void OnEnable()
    {
        ODPassthroughBlur.Acquire(this);   // frosted room behind the glass
        if (OVRManager.instance != null && OVRManager.display != null) OVRManager.display.RecenteredPose += PlaceInFront;
    }

    void OnDisable()
    {
        ODPassthroughBlur.Release(this);
        if (OVRManager.instance != null && OVRManager.display != null) OVRManager.display.RecenteredPose -= PlaceInFront;
    }

    void OnDestroy()
    {
        if (_space != null) _space.Changed -= SyncSpace;
    }

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

    /// <summary>A widget card was pressed: its details on the right, in the Small format first.</summary>
    void Pick(string id)
    {
        if (_catalog.Find(id) == null) return;
        if (_picked != id) _format = WidgetFormat.Small;
        _picked = id;
        _justAdded = false;
        Repaint();
    }

    void SelectFormat(WidgetFormat format)
    {
        _format = format;
        _justAdded = false;
        Repaint();
    }

    void AddToSpace()
    {
        var widget = _catalog.Find(_picked);
        if (widget == null || _space == null || !Widgets.TryGetValue(widget.id, out var type)) return;
        _space.Add(widget.id, type, _format, (RectTransform)transform);   // not hidden behind the catalogue; Changed → SyncSpace
        _justAdded = true;
        Repaint();
    }

    /// <summary>A widget was added to or removed from the space.</summary>
    void SyncSpace()
    {
        SyncCounts();
        if (IsOpen) Repaint();
    }

    void SyncCounts()
    {
        foreach (var widget in _catalog.widgets) widget.inSpace = _space != null ? _space.CountOf(widget.id) : 0;
    }

    void Repaint() => _view.Show(_catalog, _category, Filtered(), _search, _catalog.Find(_picked), _format, _justAdded);

    List<WidgetDefinition> Filtered()
    {
        string term = _search.Trim().ToLowerInvariant();
        // Searching looks through every category; otherwise the selected one only.
        return term.Length > 0
            ? _catalog.widgets.FindAll(w => (w.name ?? "").ToLowerInvariant().Contains(term) || (w.description ?? "").ToLowerInvariant().Contains(term))
            : _catalog.In(_catalog.categories[_category].id);
    }

    /// <summary>Still preview of <paramref name="widget"/>: built on <paramref name="parent"/>, painted once with the sample.</summary>
    ODTelemetryWidget BuildPreview(WidgetDefinition widget, WidgetFormat format, Transform parent)
    {
        if (widget == null || !Widgets.TryGetValue(widget.id, out var type)) return null;
        var preview = ODTelemetryWidget.Create(type, parent, 0f, 0f, format);
        preview.ShowStill(_sample ??= Snapshot());
        return preview;
    }

    /// <summary>A copy of the feed's current frame: the feed keeps changing it in place, the previews must not.</summary>
    static TelemetryFrame Snapshot() =>
        JsonUtility.FromJson<TelemetryFrame>(JsonUtility.ToJson(ODMockTelemetry.Shared.Frame));

    void Update()
    {
        if (_alpha >= 1f) return;
        _alpha = Mathf.Min(1f, _alpha + ODTime.Delta / FadeSeconds);
        _group.alpha = ODRuntimeUI.EaseOutCubic(_alpha);
    }

    /// <summary>Categories of the side bar and the telemetry widgets (each one also needs its type in Widgets).</summary>
    static WidgetCatalog DefaultCatalog() => new WidgetCatalog
    {
        categories = new List<WidgetCategory>
        {
            new WidgetCategory { id = WidgetCategory.SuggestionsId, name = "Suggestions", description = "Picked for this session" },
            new WidgetCategory { id = "car", name = "Car data", description = "The car of the followed driver" },
            new WidgetCategory { id = "tyres", name = "Tyres & strategy", description = "Compounds, stints and pit stops" },
            new WidgetCategory { id = "timing", name = "Timing", description = "Lap times and sectors" },
            new WidgetCategory { id = "race", name = "Race", description = "Gaps, positions and race events" },
        },
        widgets = new List<WidgetDefinition>
        {
            new WidgetDefinition
            {
                id = "speed", categoryId = "car", name = "Speed", suggested = true,
                description = "Live speed of the followed car, between the slowest and the fastest point of the lap. Large adds the speed trace of the lap.",
            },
            new WidgetDefinition
            {
                id = "pedals", categoryId = "car", name = "Pedals",
                description = "Throttle and brake of the followed car. Large adds the throttle line and the braking zones over the lap.",
            },
            new WidgetDefinition
            {
                id = "sectors", categoryId = "timing", name = "Sectors", suggested = true,
                description = "S1, S2 and S3 of the running lap with the F1 colour code. Large adds the 24 mini-sectors against a rival.",
            },
            new WidgetDefinition
            {
                id = "laptime", categoryId = "timing", name = "Lap time", suggested = true,
                description = "Running lap clock, best lap and live delta. Large adds the last five laps and their gap to the best.",
            },
        },
    };
}
