using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BUILD ORDER — Tier 02 (Screens). Requires: 01_ODUIBuilder (UITheme.asset only —
/// this screen builds its own GameObjects from scratch, no ODCard/ODButton prefabs).
/// Downstream: 03_SearchPanelScreenBuilder, 03_ProfilePanelScreenBuilder,
/// 03_VideoPlayerScreenBuilder (soft) all attach to the hierarchy this class saves,
/// so re-run this BEFORE any of them if OverdriveMenuPanel.prefab doesn't exist yet.
/// </summary>
public static class MainMenuScreenBuilder
{
    // Palette pulled from UITheme.Instance at build time — see PullTheme(). Falls
    // back to UITheme's own default values if no asset exists yet in Resources.
    private static Color BgColor;
    private static Color SidebarColor;
    private static Color ContentAreaColor;
    private static Color SearchBarColor;
    private static Color HoverOverlay;
    private static Color PressedOverlay;
    private static Color GoldColor;
    private static Color TextColor;
    private static Color SubTextColor;
    private static float CornerRadius;

    static void PullTheme()
    {
        UITheme theme = UITheme.Instance;
        UITheme fallback = ScriptableObject.CreateInstance<UITheme>();
        if (theme == null) theme = fallback;

        BgColor          = theme.panelBackgroundAlt;
        SidebarColor     = theme.panelBackground;
        ContentAreaColor = theme.panelBackgroundAlt;
        SearchBarColor   = theme.surfaceColor;
        HoverOverlay     = theme.hoverOverlay;
        PressedOverlay   = theme.pressedOverlay;
        GoldColor        = theme.accentGold;
        TextColor        = theme.textPrimary;
        SubTextColor     = theme.textSecondary;
        CornerRadius     = theme.cornerRadius;

        Object.DestroyImmediate(fallback);
    }

    public static void Build()
    {
        PullTheme();

        // -- Canvas --
        GameObject canvasGO = new GameObject("OverdriveMenuCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(1920, 1080);
        canvasRT.localScale = Vector3.one * 0.001f;

        // -- Root Panel (rounded dark bg) --
        GameObject panel = CreatePanel("OverdriveMenuPanel", canvasGO.transform,
            new Vector2(0, 0), new Vector2(1400, 800), BgColor, CornerRadius);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;

        // -- Title "Overdrive" --
        GameObject title = CreateText("Title", panel.transform, "Overdrive",
            36, FontStyles.Bold, GoldColor);
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(0, 1);
        titleRT.pivot = new Vector2(0, 1);
        titleRT.anchoredPosition = new Vector2(20, -16);
        titleRT.sizeDelta = new Vector2(220, 50);

        // -- Search Bar (fully rounded pill: radius = height / 2) --
        GameObject searchBg = CreatePanel("SearchBar", panel.transform,
            new Vector2(0, 0), new Vector2(800, 44), SearchBarColor, 22f);
        RectTransform searchRT = searchBg.GetComponent<RectTransform>();
        searchRT.anchorMin = new Vector2(0.5f, 1);
        searchRT.anchorMax = new Vector2(0.5f, 1);
        searchRT.pivot = new Vector2(0.5f, 1);
        searchRT.anchoredPosition = new Vector2(130, -20);

        GameObject searchIcon = CreateText("SearchIcon", searchBg.transform,
            "🎤 Search", 16, FontStyles.Normal, SubTextColor);
        RectTransform siRT = searchIcon.GetComponent<RectTransform>();
        siRT.anchorMin = Vector2.zero; siRT.anchorMax = Vector2.one;
        siRT.offsetMin = new Vector2(12, 0); siRT.offsetMax = Vector2.zero;

        // -- Body: Sidebar + Content (horizontal layout) --
        GameObject body = new GameObject("Body");
        body.transform.SetParent(panel.transform, false);
        RectTransform bodyRT = body.AddComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0, 0);
        bodyRT.anchorMax = new Vector2(1, 1);
        bodyRT.offsetMin = new Vector2(0, 0);
        bodyRT.offsetMax = new Vector2(0, -80);
        HorizontalLayoutGroup hlg = body.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        // -- Sidebar --
        GameObject sidebar = CreatePanel("Sidebar", body.transform,
            Vector2.zero, Vector2.zero, SidebarColor);
        LayoutElement sideLE = sidebar.AddComponent<LayoutElement>();
        sideLE.preferredWidth = 220;
        sideLE.flexibleWidth = 0;

        VerticalLayoutGroup vlg = sidebar.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 20, 20);
        vlg.spacing = 4;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;

        string[] navItems = { "Races", "Rankings", "Profile", "Settings" };
        foreach (string nav in navItems)
            CreateNavButton(nav, sidebar.transform);

        // -- Content Area --
        GameObject contentArea = CreatePanel("ContentArea", body.transform,
            Vector2.zero, Vector2.zero, ContentAreaColor);
        LayoutElement contentLE = contentArea.AddComponent<LayoutElement>();
        contentLE.flexibleWidth = 1;

        // Category dropdown label (visual)
        GameObject catLabel = CreateText("CategoryLabel", contentArea.transform,
            "F1  ▾", 20, FontStyles.Bold, TextColor);
        RectTransform catRT = catLabel.GetComponent<RectTransform>();
        catRT.anchorMin = new Vector2(0, 1);
        catRT.anchorMax = new Vector2(0, 1);
        catRT.pivot = new Vector2(0, 1);
        catRT.anchoredPosition = new Vector2(16, -14);
        catRT.sizeDelta = new Vector2(120, 36);

        // ScrollView for grid
        GameObject scrollView = new GameObject("GridScrollView");
        scrollView.transform.SetParent(contentArea.transform, false);
        ScrollRect sr = scrollView.AddComponent<ScrollRect>();
        sr.horizontal = false;
        Image srImg = scrollView.AddComponent<Image>();
        srImg.color = Color.clear;
        RectTransform srRT = scrollView.GetComponent<RectTransform>();
        srRT.anchorMin = new Vector2(0, 0);
        srRT.anchorMax = new Vector2(1, 1);
        srRT.offsetMin = new Vector2(12, 12);
        srRT.offsetMax = new Vector2(-12, -60);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        // RectMask2D: clips without a graphic. NEVER use Mask + transparent
        // Image — its mesh gets culled and ALL masked children vanish.
        viewport.AddComponent<RectMask2D>();
        sr.viewport = vpRT;

        // Content (grid)
        GameObject gridContent = new GameObject("GridContent");
        gridContent.transform.SetParent(viewport.transform, false);
        RectTransform gcRT = gridContent.AddComponent<RectTransform>();
        gcRT.anchorMin = new Vector2(0, 1);
        gcRT.anchorMax = new Vector2(1, 1);
        gcRT.pivot = new Vector2(0.5f, 1);
        gcRT.anchoredPosition = Vector2.zero;
        GridLayoutGroup grid = gridContent.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(340, 200);
        grid.spacing = new Vector2(16, 16);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        ContentSizeFitter csf = gridContent.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = gcRT;

        // Placeholder grid items
        for (int i = 0; i < 6; i++)
            CreateGridItem($"Item_{i}", gridContent.transform);

        // Wire ContentGridView
        ContentGridView cgv = contentArea.AddComponent<ContentGridView>();
        cgv.gridContainer = gcRT;

        // Wire OverdriveMainMenu
        OverdriveMainMenu menu = panel.AddComponent<OverdriveMainMenu>();
        cgv.gridContainer = gcRT;

        // Save prefab
        string path = "Assets/_Overdrive/UI/Prefabs/Screens/OverdriveMenuPanel.prefab";
        System.IO.Directory.CreateDirectory("Assets/_Overdrive/UI/Prefabs/Screens");
        PrefabUtility.SaveAsPrefabAsset(canvasGO, path);
        Debug.Log($"Overdrive menu prefab saved to {path}");

        Selection.activeGameObject = canvasGO;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // radius = 0 keeps a flat Image (used for panels flush against another
    // panel's straight edge, e.g. Sidebar/ContentArea against the rounded root).
    static GameObject CreatePanel(string name, Transform parent, Vector2 pos, Vector2 size, Color color, float radius = 0f)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (radius > 0f)
        {
            RoundedImage rounded = go.AddComponent<RoundedImage>();
            rounded.cornerRadius = radius;
            rounded.color        = color;
        }
        else
        {
            Image img = go.AddComponent<Image>();
            img.color = color;
        }
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    static GameObject CreateText(string name, Transform parent, string text,
        float size, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return go;
    }

    static void CreateNavButton(string label, Transform parent)
    {
        GameObject go = new GameObject($"Nav_{label}");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220, 48);
        Button btn = go.AddComponent<Button>();
        Image bg = go.AddComponent<Image>();
        bg.color = Color.clear;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.clear;
        cb.highlightedColor = HoverOverlay;
        cb.pressedColor = PressedOverlay;
        btn.colors = cb;
        btn.targetGraphic = bg;

        // Label
        GameObject labelGO = CreateText("Label", go.transform, label,
            17, FontStyles.Normal, TextColor);
        RectTransform lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(20, 0); lrt.offsetMax = new Vector2(-30, 0);

        // Arrow "›"
        GameObject arrow = CreateText("Arrow", go.transform, "›",
            20, FontStyles.Normal, SubTextColor);
        RectTransform art = arrow.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(1, 0.5f);
        art.anchorMax = new Vector2(1, 0.5f);
        art.pivot = new Vector2(1, 0.5f);
        art.anchoredPosition = new Vector2(-12, 0);
        art.sizeDelta = new Vector2(20, 30);
    }

    static void CreateGridItem(string name, Transform parent)
    {
        GameObject go = CreatePanel(name, parent, Vector2.zero, Vector2.zero, ContentAreaColor, CornerRadius);

        go.AddComponent<ContentGridItem>();

        // Thumbnail placeholder — same radius as its parent card so corners align exactly
        GameObject thumb = CreatePanel("Thumbnail", go.transform, Vector2.zero, Vector2.zero, SearchBarColor, CornerRadius);
        RectTransform trt = thumb.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        Image thumbImg = thumb.GetComponent<Image>();

        // Wire thumbnail to ContentGridItem
        go.GetComponent<ContentGridItem>().thumbnail = thumbImg;
    }
}
