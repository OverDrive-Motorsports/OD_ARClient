using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Replay gallery. Cards are generated from data — finished sessions from the
/// backend, or the mobile mocks once the user picks the demo data — by cloning
/// the first card baked in the prefab, so the look stays the builder's.
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
    private UnityAction[] _openActions = Array.Empty<UnityAction>();
    private GameObject _template;
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

        // The prefab's demo cards are hidden; the first one becomes the card template.
        foreach (var entry in entries)
            if (entry?.openButton != null) entry.openButton.gameObject.SetActive(false);
        if (entries.Length > 0 && entries[0]?.openButton != null) _template = entries[0].openButton.gameObject;
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
        UnwireEntries();
        foreach (var card in _generated) if (card != null) Destroy(card);
        _generated.Clear();

        entries = new Entry[replays.Count];
        _openActions = new UnityAction[replays.Count];
        for (int i = 0; i < replays.Count; i++)
        {
            ReplaySummary replay = replays[i];
            Button open = _template != null ? CreateCard(replay) : null;
            entries[i] = new Entry
            {
                id         = replay.id,
                grandPrix  = replay.grandPrix,
                session    = replay.sessionName,
                season     = replay.season,
                openButton = open,
            };
            int index = i;
            _openActions[i] = () => OpenReplay(index);
            open?.onClick.AddListener(_openActions[i]);
        }

        if (replays.Count > 0) ApplyTheme(replays[0].championshipId);
        string championship = replays.Count > 0 ? replays[0].championshipName : "Formule 1";
        SetLabel(subtitle, $"{championship} · {replays.Count} replay{(replays.Count > 1 ? "s" : "")}");
        SetLabel(notice, demo ? "Données de démonstration (OD_Mobile)" : "Données du backend · Vidéos non connectées");
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }

    private Button CreateCard(ReplaySummary replay)
    {
        var card = Instantiate(_template, _template.transform.parent);
        card.name = "Replay_" + replay.id;
        card.SetActive(true);
        _generated.Add(card);

        SetChild(card.transform, "Season",    $"{(replay.championshipName ?? "").ToUpperInvariant()} · SAISON {replay.season}");
        SetChild(card.transform, "GrandPrix", replay.grandPrix);
        SetChild(card.transform, "Session",   replay.date != default
            ? $"{replay.sessionName} · {replay.date:dd/MM/yyyy}"
            : replay.sessionName);
        StyleCard(card.transform);
        return card.GetComponent<Button>();
    }

    // ── Design layout (design px, 1920 x 1200 window): header like the championship pages, 3-column plateaux ──

    const float CardW = 570.67f, CardH = 300f;

    private void LayoutPage()
    {
        const float headerBottom = 162f, x = 72f + 68f + 24f;
        if (backButton != null)
        {
            ODDesign.Place(backButton.transform, 72f, headerBottom - 68f, 68f, 68f);
            if (backButton.targetGraphic is RoundedImage round) round.cornerRadius = ODDesign.Px(34f);
            ODDesign.ApplyButton(backButton, ODDesign.ButtonKind.Icon);
            var icon = backButton.GetComponentInChildren<ODNavigationIcon>(true);
            if (icon != null) { ODDesign.Place(icon.transform, 19f, 19f, 30f, 30f); icon.color = Color.white; }
        }
        if (subtitle != null)
        {
            ODDesign.Place(subtitle.transform, x, 60f, 1100f, 30f);
            ODDesign.Style(subtitle, ODDesign.PageCaps, ODDesign.Weight.SemiBold, ODDesign.Secondary, caps: true);
        }
        var heading = transform.Find("Heading")?.GetComponent<TextMeshProUGUI>();
        if (heading != null)
        {
            ODDesign.Place(heading.transform, x, 97f, 1100f, 72f);
            ODDesign.Style(heading, ODDesign.TitleXL, ODDesign.Weight.Bold, ODDesign.Primary);
        }
        var badge = transform.Find("SportBadge");
        if (badge != null)
        {
            ODDesign.Place(badge, 1920f - 72f - 120f, headerBottom - 64f, 120f, 64f);
            if (badge.GetComponent<RoundedImage>() is RoundedImage pill) pill.cornerRadius = ODDesign.Px(32f);
            var label = badge.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                var r = label.rectTransform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                ODDesign.Style(label, ODDesign.CapsSize, ODDesign.Weight.Bold, Color.white, TextAlignmentOptions.Center, trackingEm: .06f);
            }
        }
        if (scroll != null)
        {
            ODDesign.Place(scroll.transform, 72f, 202f, 1920f - 144f, 1200f - 202f - 64f - 40f);
            var grid = scroll.content != null ? scroll.content.GetComponent<GridLayoutGroup>() : null;
            if (grid != null)
            {
                grid.cellSize = new Vector2(ODDesign.Px(CardW), ODDesign.Px(CardH));
                grid.spacing = new Vector2(ODDesign.Px(32f), ODDesign.Px(32f));
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
            }
            scroll.verticalScrollbar = null;
        }
        var track = transform.Find("ScrollTrack");
        if (track != null) track.gameObject.SetActive(false);
        if (notice != null)
        {
            ODDesign.Place(notice.transform, 72f, 1200f - 64f - 30f, 1200f, 30f);
            ODDesign.Style(notice, 18f, ODDesign.Weight.Regular, ODDesign.Tertiary);
        }
    }

    /// <summary>Replay plateau: caps season line, 34-px Grand Prix, session line, "Ouvrir le replay" capsule.</summary>
    private static void StyleCard(Transform card)
    {
        Style(card, "Season", 36f, 34f, CardW - 72f, 26f, 18f, ODDesign.Weight.SemiBold, ODDesign.Secondary, caps: true);
        Style(card, "GrandPrix", 36f, 70f, CardW - 72f, 44f, 34f, ODDesign.Weight.SemiBold, ODDesign.Primary);
        Style(card, "Session", 36f, 120f, CardW - 72f, 30f, 21f, ODDesign.Weight.Regular, ODDesign.Muted);
        var open = card.Find("OpenReplaySurface");
        if (open != null)
        {
            ODDesign.Place(open, 36f, CardH - 36f - 64f, CardW - 72f, 64f);
            if (open.GetComponent<RoundedImage>() is RoundedImage pill) pill.cornerRadius = ODDesign.Px(32f);
            foreach (var label in open.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                var r = label.rectTransform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                ODDesign.Style(label, 22f, ODDesign.Weight.SemiBold, Color.white, TextAlignmentOptions.Center);
            }
        }
    }

    private static void Style(Transform card, string child, float x, float y, float w, float h, float size, ODDesign.Weight weight, Color colour, bool caps = false)
    {
        var label = card.Find(child)?.GetComponent<TextMeshProUGUI>();
        if (label == null) return;
        ODDesign.FitLine(ref y, ref h, size);
        ODDesign.Place(label.transform, x, y, w, h);
        ODDesign.Style(label, size, weight, colour, caps: caps);
    }

    /// <summary>Championship gradient behind the gallery, glass cards, accent-tinted sport badge.</summary>
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
        foreach (var cta in GetComponentsInChildren<Transform>(true))
            if (cta.name == "OpenReplaySurface" && cta.TryGetComponent(out Graphic graphic))
                graphic.color = new Color(1f, 1f, 1f, .14f);
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

    public void BackToSport()
    {
        if (menu != null) menu.ShowSport();
    }

    private void UnwireEntries()
    {
        for (int i = 0; i < entries.Length && i < _openActions.Length; i++)
            entries[i].openButton?.onClick.RemoveListener(_openActions[i]);
    }

    private static void SetChild(Transform card, string child, string value)
    {
        var label = card.Find(child)?.GetComponent<TextMeshProUGUI>();
        if (label != null) label.text = value;
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
