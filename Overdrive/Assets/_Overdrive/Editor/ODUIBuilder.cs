/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODUIBuilder - Unity Editor script. Generates all OD_UI prefabs programmatically via Overdrive menu items.
 ##
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Generates all OD_UI prefabs via three Overdrive menu items:
/// <list type="bullet">
///   <item>Overdrive &gt; Build OD_UI Prefabs — atoms + molecules</item>
///   <item>Overdrive &gt; Build OD_UI Organisms — ODCard, ODModal, ODNavBar</item>
///   <item>Overdrive &gt; Build OD_UI Level3 — gold border, table, driver card, media controls</item>
/// </list>
/// Re-run at any time to reset prefabs to their defaults. All baked color constants
/// must match UITheme.cs default values exactly so prefabs look correct before theme is loaded.
/// </summary>
public static class ODUIBuilder
{
    private const string ThemePath   = "Assets/_Overdrive/UI/Resources/UITheme.asset";
    private const string AtomPath    = "Assets/_Overdrive/UI/Prefabs/Atoms/";
    private const string MolPath     = "Assets/_Overdrive/UI/Prefabs/Molecules/";
    private const string OrgPath     = "Assets/_Overdrive/UI/Prefabs/Organisms/";

    // ── Brand colors baked into prefabs (must match UITheme defaults exactly) ────
    private static readonly Color GoldColor        = new Color(0.788f, 0.659f, 0.298f, 1.00f); // #C9A84C
    private static readonly Color GoldBorderColorA = new Color(0.788f, 0.659f, 0.298f, 0.60f); // #C9A84C 60%
    private static readonly Color DangerColor      = new Color(0.910f, 0.000f, 0.176f, 1.00f); // #E8002D
    private static readonly Color PanelBg          = new Color(0.110f, 0.110f, 0.125f, 0.78f); // rgba(28,28,32,0.78)
    private static readonly Color SurfaceColor     = new Color(0.173f, 0.173f, 0.204f, 0.90f); // rgba(44,44,52,0.90)
    private static readonly Color TextPrimary      = new Color(0.949f, 0.949f, 0.969f, 1.00f); // #F2F2F7
    private static readonly Color TextSecondary    = new Color(0.557f, 0.557f, 0.576f, 1.00f); // #8E8E93
    private static readonly Color BorderColor      = new Color(1.000f, 1.000f, 1.000f, 0.10f); // white 10%

    // ── Menu items ───────────────────────────────────────────────────────────────

    /// <summary>Creates or refreshes all Atom and Molecule prefabs in Assets/_Overdrive/UI/Prefabs/.</summary>
    [MenuItem("Overdrive/Build OD_UI Prefabs")]
    public static void BuildAll()
    {
        EnsureDirectories();
        EnsureUIThemeAsset();
        BuildAtomPrefabs();
        BuildMoleculePrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ODUIBuilder] All OD_UI prefabs built successfully.");
        EditorUtility.DisplayDialog("OD_UI Builder", "Atoms + Molecules created in Assets/_Overdrive/UI/Prefabs/", "OK");
    }

    /// <summary>Creates or refreshes ODCard, ODModal, and ODNavBar prefabs in Assets/_Overdrive/UI/Prefabs/Organisms/.</summary>
    [MenuItem("Overdrive/Build OD_UI Organisms")]
    public static void BuildOrganisms()
    {
        EnsureDirectories();
        EnsureUIThemeAsset();
        BuildOrganismPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ODUIBuilder] Organism prefabs built successfully.");
        EditorUtility.DisplayDialog("OD_UI Builder", "Organisms created in Assets/_Overdrive/UI/Prefabs/Organisms/", "OK");
    }

    // ── Directories ──────────────────────────────────────────────────────────────

    static void EnsureDirectories()
    {
        string[] dirs = {
            "Assets/_Overdrive/UI",
            "Assets/_Overdrive/UI/Resources",
            "Assets/_Overdrive/UI/Theme",
            "Assets/_Overdrive/UI/Atoms",
            "Assets/_Overdrive/UI/Molecules",
            "Assets/_Overdrive/UI/Organisms",
            "Assets/_Overdrive/UI/Prefabs",
            "Assets/_Overdrive/UI/Prefabs/Atoms",
            "Assets/_Overdrive/UI/Prefabs/Molecules",
            "Assets/_Overdrive/UI/Prefabs/Organisms",
        };
        foreach (string dir in dirs)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                int sep = dir.LastIndexOf('/');
                AssetDatabase.CreateFolder(dir.Substring(0, sep), dir.Substring(sep + 1));
            }
        }
    }

    // ── UITheme asset ────────────────────────────────────────────────────────────

    static void EnsureUIThemeAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath) != null) return;
        UITheme theme = ScriptableObject.CreateInstance<UITheme>();
        AssetDatabase.CreateAsset(theme, ThemePath);
        Debug.Log("[ODUIBuilder] Created UITheme.asset at " + ThemePath);
    }

    // ── Atoms ────────────────────────────────────────────────────────────────────

    static void BuildAtomPrefabs()
    {
        BuildODBackground();
        BuildODLabel();
        BuildODIcon();
        BuildODDivider();
    }

    static void BuildODBackground()
    {
        GameObject go = new GameObject("ODBackground");
        RoundedImage img = go.AddComponent<RoundedImage>();
        img.color         = PanelBg;
        img.cornerRadius  = 24f;
        img.raycastTarget = false;
        go.AddComponent<ODBackground>();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Save(go, AtomPath + "ODBackground.prefab");
        Object.DestroyImmediate(go);
    }

    static void BuildODLabel()
    {
        GameObject go      = new GameObject("ODLabel");
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Label";
        tmp.fontSize  = 22f;
        tmp.color     = TextPrimary;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        ODLabel lbl   = go.AddComponent<ODLabel>();
        lbl.textStyle = ODLabel.TextStyle.Body;

        Save(go, AtomPath + "ODLabel.prefab");
        Object.DestroyImmediate(go);
    }

    static void BuildODIcon()
    {
        GameObject go = new GameObject("ODIcon");
        Image img     = go.AddComponent<Image>();
        img.color     = GoldColor;
        img.preserveAspect = true;
        ODIcon ic = go.AddComponent<ODIcon>();
        ic.tint   = GoldColor;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(48f, 48f);

        Save(go, AtomPath + "ODIcon.prefab");
        Object.DestroyImmediate(go);
    }

    static void BuildODDivider()
    {
        GameObject go = new GameObject("ODDivider");
        Image img     = go.AddComponent<Image>();
        img.color     = BorderColor;
        img.raycastTarget = false;
        go.AddComponent<ODDivider>();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 2f);

        Save(go, AtomPath + "ODDivider.prefab");
        Object.DestroyImmediate(go);
    }

    // ── Molecules ────────────────────────────────────────────────────────────────

    static void BuildMoleculePrefabs()
    {
        BuildODButton(ODButton.ButtonStyle.Primary, "ODButton_Primary");
        BuildODButton(ODButton.ButtonStyle.Ghost,   "ODButton_Ghost");
        BuildODButton(ODButton.ButtonStyle.Danger,  "ODButton_Danger");
        BuildODInputField();
        BuildODBadge();
    }

    static void BuildODButton(ODButton.ButtonStyle style, string prefabName)
    {
        // ── Root ──────────────────────────────────────────────────────────────
        GameObject root = new GameObject(prefabName, typeof(RectTransform));
        root.AddComponent<CanvasGroup>();
        ODButton btn = root.AddComponent<ODButton>();
        btn.buttonStyle = style;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 52f);

        // ── Background ────────────────────────────────────────────────────────
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        RoundedImage bgImg = bgGO.AddComponent<RoundedImage>();
        bgImg.cornerRadius  = 24f;
        bgImg.raycastTarget = false;
        ODBackground bg = bgGO.AddComponent<ODBackground>();
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        switch (style)
        {
            case ODButton.ButtonStyle.Primary: bgImg.color = GoldColor;         break;
            case ODButton.ButtonStyle.Ghost:   bgImg.color = Color.clear;       break;
            case ODButton.ButtonStyle.Danger:  bgImg.color = DangerColor;       break;
        }

        if (style == ODButton.ButtonStyle.Ghost)
        {
            Outline ol = bgGO.AddComponent<Outline>();
            ol.effectColor    = BorderColor;
            ol.effectDistance = new Vector2(1.5f, -1.5f);
        }

        // ── Content ───────────────────────────────────────────────────────────
        GameObject content = new GameObject("Content");
        content.transform.SetParent(root.transform, false);
        HorizontalLayoutGroup hlg = GetOrAdd<HorizontalLayoutGroup>(content);
        hlg.spacing                = 8f;
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(16f, 0f);
        contentRT.offsetMax = new Vector2(-16f, 0f);

        // ── Icon ──────────────────────────────────────────────────────────────
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(content.transform, false);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.color          = style == ODButton.ButtonStyle.Ghost ? GoldColor : Color.white;
        iconImg.preserveAspect = true;
        ODIcon ic = iconGO.AddComponent<ODIcon>();
        ic.tint   = style == ODButton.ButtonStyle.Ghost ? GoldColor : Color.white;
        iconGO.GetComponent<RectTransform>().sizeDelta = new Vector2(24f, 24f);
        LayoutElement iconLE = iconGO.AddComponent<LayoutElement>();
        iconLE.preferredWidth  = 24f;
        iconLE.preferredHeight = 24f;
        iconGO.SetActive(false);

        // ── Label ─────────────────────────────────────────────────────────────
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(content.transform, false);
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text         = prefabName;
        tmp.fontSize     = 22f;
        tmp.color        = (style == ODButton.ButtonStyle.Ghost) ? TextPrimary : Color.white;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        ODLabel lbl      = labelGO.AddComponent<ODLabel>();
        lbl.textStyle    = ODLabel.TextStyle.Body;
        labelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 40f);

        btn.background = bg;
        btn.label      = lbl;
        btn.icon       = ic;

        Save(root, MolPath + prefabName + ".prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildODInputField()
    {
        GameObject root = new GameObject("ODInputField", typeof(RectTransform));
        ODInputField odf = root.AddComponent<ODInputField>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 52f);

        // Border
        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(root.transform, false);
        RoundedImage borderImg = borderGO.AddComponent<RoundedImage>();
        borderImg.color         = BorderColor;
        borderImg.cornerRadius  = 16f;
        borderImg.raycastTarget = false;
        RectTransform borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = Vector2.zero;
        borderRT.offsetMax = Vector2.zero;

        // Background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        RoundedImage bgImg = bgGO.AddComponent<RoundedImage>();
        bgImg.color         = PanelBg;
        bgImg.cornerRadius  = 15f;
        bgImg.raycastTarget = false;
        ODBackground bg     = bgGO.AddComponent<ODBackground>();
        RectTransform bgRT  = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(1.5f,  1.5f);
        bgRT.offsetMax = new Vector2(-1.5f, -1.5f);

        // TMP_InputField
        GameObject ifGO = new GameObject("TMP_InputField", typeof(RectTransform));
        ifGO.transform.SetParent(root.transform, false);
        TMP_InputField inputField = ifGO.AddComponent<TMP_InputField>();
        RectTransform ifRT = ifGO.GetComponent<RectTransform>();
        ifRT.anchorMin = Vector2.zero;
        ifRT.anchorMax = Vector2.one;
        ifRT.offsetMin = new Vector2(14f, 0f);
        ifRT.offsetMax = new Vector2(-14f, 0f);

        // Text Area
        GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(ifGO.transform, false);
        RectTransform taRT = textArea.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = Vector2.zero;
        taRT.offsetMax = Vector2.zero;
        textArea.AddComponent<RectMask2D>();

        // Placeholder
        GameObject phGO       = new GameObject("Placeholder");
        phGO.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI phTmp = phGO.AddComponent<TextMeshProUGUI>();
        phTmp.text      = "Enter text…";
        phTmp.fontSize  = 22f;
        phTmp.color     = TextSecondary;
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero;
        phRT.offsetMax = Vector2.zero;

        // Input Text
        GameObject itGO        = new GameObject("Input Text");
        itGO.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI itTmp  = itGO.AddComponent<TextMeshProUGUI>();
        itTmp.text      = "";
        itTmp.fontSize  = 22f;
        itTmp.color     = TextPrimary;
        itTmp.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform itRT = itGO.GetComponent<RectTransform>();
        itRT.anchorMin = Vector2.zero;
        itRT.anchorMax = Vector2.one;
        itRT.offsetMin = Vector2.zero;
        itRT.offsetMax = Vector2.zero;

        inputField.textComponent = itTmp;
        inputField.placeholder   = phTmp;
        inputField.textViewport  = taRT;

        odf.background  = bg;
        odf.inputField  = inputField;
        odf.borderImage = borderImg;

        Save(root, MolPath + "ODInputField.prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildODBadge()
    {
        GameObject root  = new GameObject("ODBadge", typeof(RectTransform));
        ODBadge badge    = root.AddComponent<ODBadge>();
        badge.variant    = ODBadge.BadgeVariant.Default;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 28f);

        // Background
        GameObject bgGO    = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        RoundedImage bgImg  = bgGO.AddComponent<RoundedImage>();
        bgImg.color         = new Color(0.557f, 0.557f, 0.576f, 0.12f); // Default variant baked
        bgImg.cornerRadius  = 24f;
        bgImg.raycastTarget = false;
        ODBackground bg     = bgGO.AddComponent<ODBackground>();
        bg.backgroundStyle  = ODBackground.Style.Subtle;
        RectTransform bgRT  = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Label
        GameObject labelGO  = new GameObject("Label");
        labelGO.transform.SetParent(root.transform, false);
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Badge";
        tmp.fontSize  = 18f;
        tmp.color     = TextSecondary;
        tmp.alignment = TextAlignmentOptions.Center;
        ODLabel lbl   = labelGO.AddComponent<ODLabel>();
        lbl.textStyle = ODLabel.TextStyle.Caption;
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(8f, 0f);
        labelRT.offsetMax = new Vector2(-8f, 0f);

        badge.background = bg;
        badge.label      = lbl;

        Save(root, MolPath + "ODBadge.prefab");
        Object.DestroyImmediate(root);
    }

    // ── Organisms ────────────────────────────────────────────────────────────────

    static void BuildOrganismPrefabs()
    {
        BuildODCard();
        BuildODModal();
        BuildODNavBar();
    }

    static void BuildODCard()
    {
        // ── Root — grows vertically to fit its content ────────────────────────
        GameObject root = new GameObject("ODCard", typeof(RectTransform));
        ODCard card     = root.AddComponent<ODCard>();   // [RequireComponent(CanvasGroup)] adds it automatically
        // Width is set by parent; height is driven by ContentSizeFitter
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 0f);

        var rootVlg = root.AddComponent<VerticalLayoutGroup>();
        rootVlg.childForceExpandWidth  = true;
        rootVlg.childForceExpandHeight = false;
        rootVlg.childControlWidth      = true;
        rootVlg.childControlHeight     = false;
        rootVlg.spacing                = 0f;
        root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Background — absolute fill, outside layout flow ───────────────────
        GameObject bgGO    = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        RoundedImage bgImg  = bgGO.AddComponent<RoundedImage>();
        bgImg.color         = PanelBg;
        bgImg.cornerRadius  = 24f;
        bgImg.raycastTarget = false;
        ODBackground bg     = bgGO.AddComponent<ODBackground>();
        bg.backgroundStyle  = ODBackground.Style.Card;
        RectTransform bgRT  = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<LayoutElement>().ignoreLayout = true;

        // ── Header — layout-driven, fixed preferred height ────────────────────
        GameObject header = new GameObject("Header");
        header.transform.SetParent(root.transform, false);
        HorizontalLayoutGroup hlg = GetOrAdd<HorizontalLayoutGroup>(header);
        hlg.padding                = new RectOffset(20, 16, 16, 0);
        hlg.spacing                = 8f;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = false;
        header.AddComponent<LayoutElement>().preferredHeight = 56f;

        // Title
        GameObject titleGO  = new GameObject("Title");
        titleGO.transform.SetParent(header.transform, false);
        TextMeshProUGUI titleTmp = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text      = "Card Title";
        titleTmp.fontSize  = 28f;
        titleTmp.color     = TextPrimary;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        ODLabel titleLbl   = titleGO.AddComponent<ODLabel>();
        titleLbl.textStyle = ODLabel.TextStyle.H2;
        LayoutElement titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.flexibleWidth = 1f;
        titleGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 40f);

        // CloseButton
        GameObject closeGO = new GameObject("CloseButton", typeof(RectTransform));
        closeGO.transform.SetParent(header.transform, false);
        closeGO.AddComponent<CanvasGroup>();
        ODButton closeBtn = closeGO.AddComponent<ODButton>();
        closeBtn.buttonStyle = ODButton.ButtonStyle.Ghost;
        closeGO.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
        LayoutElement closeLE = closeGO.AddComponent<LayoutElement>();
        closeLE.preferredWidth  = 32f;
        closeLE.preferredHeight = 32f;

        // CloseButton > Background
        GameObject closeBgGO = new GameObject("Background");
        closeBgGO.transform.SetParent(closeGO.transform, false);
        RoundedImage closeBgImg = closeBgGO.AddComponent<RoundedImage>();
        closeBgImg.color        = Color.clear;
        closeBgImg.cornerRadius = 16f;
        closeBgImg.raycastTarget = false;
        ODBackground closeBg    = closeBgGO.AddComponent<ODBackground>();
        RectTransform closeBgRT = closeBgGO.GetComponent<RectTransform>();
        closeBgRT.anchorMin = Vector2.zero;
        closeBgRT.anchorMax = Vector2.one;
        closeBgRT.offsetMin = Vector2.zero;
        closeBgRT.offsetMax = Vector2.zero;
        Outline closeOl = closeBgGO.AddComponent<Outline>();
        closeOl.effectColor    = BorderColor;
        closeOl.effectDistance = new Vector2(1.5f, -1.5f);

        // CloseButton > Label
        GameObject closeLabelGO = new GameObject("Label");
        closeLabelGO.transform.SetParent(closeGO.transform, false);
        TextMeshProUGUI closeTmp = closeLabelGO.AddComponent<TextMeshProUGUI>();
        closeTmp.text      = "×";
        closeTmp.fontSize  = 22f;
        closeTmp.color     = TextPrimary;
        closeTmp.alignment = TextAlignmentOptions.Center;
        ODLabel closeLbl   = closeLabelGO.AddComponent<ODLabel>();
        closeLbl.textStyle = ODLabel.TextStyle.Body;
        RectTransform closeLabelRT = closeLabelGO.GetComponent<RectTransform>();
        closeLabelRT.anchorMin = Vector2.zero;
        closeLabelRT.anchorMax = Vector2.one;
        closeLabelRT.offsetMin = Vector2.zero;
        closeLabelRT.offsetMax = Vector2.zero;

        closeBtn.background = closeBg;
        closeBtn.label      = closeLbl;
        closeGO.SetActive(false);

        // ── Divider — 1 px hairline, layout-driven ────────────────────────────
        GameObject divGO = new GameObject("Divider");
        divGO.transform.SetParent(root.transform, false);
        Image divImg     = divGO.AddComponent<Image>();
        divImg.color     = BorderColor;
        divImg.raycastTarget = false;
        // LayoutElement makes the divider participate in the root VLG instead of
        // using the absolute anchors that ODDivider.Start() would otherwise apply.
        divGO.AddComponent<ODDivider>();
        divGO.AddComponent<LayoutElement>().preferredHeight = 1f;

        // ── Content — grows with its children ─────────────────────────────────
        GameObject content = new GameObject("Content");
        content.transform.SetParent(root.transform, false);
        VerticalLayoutGroup vlg = GetOrAdd<VerticalLayoutGroup>(content);
        vlg.padding                = new RectOffset(16, 16, 16, 16);
        vlg.spacing                = 12f;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        ContentSizeFitter csf = GetOrAdd<ContentSizeFitter>(content);
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.sizeDelta = Vector2.zero;

        // ── Wire ODCard ───────────────────────────────────────────────────────
        card.titleLabel  = titleLbl;
        card.closeButton = closeBtn;
        card.divider     = divGO;
        card.contentArea = contentRT;

        Save(root, OrgPath + "ODCard.prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildODModal()
    {
        // ── Root ──────────────────────────────────────────────────────────────
        GameObject root    = new GameObject("ODModal", typeof(RectTransform));
        ODModal modal      = root.AddComponent<ODModal>(); // [RequireComponent(CanvasGroup)] adds it automatically
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        // ── Overlay ───────────────────────────────────────────────────────────
        GameObject overlayGO = new GameObject("Overlay");
        overlayGO.transform.SetParent(root.transform, false);
        Image overlayImg     = overlayGO.AddComponent<Image>();
        overlayImg.color     = new Color(0f, 0f, 0f, 0.45f);
        overlayImg.raycastTarget = true;
        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        // ── Card ──────────────────────────────────────────────────────────────
        // Load the ODCard prefab we just created, or build inline
        GameObject cardGO  = new GameObject("Card", typeof(RectTransform));
        cardGO.transform.SetParent(root.transform, false);
        ODCard card        = cardGO.AddComponent<ODCard>();
        cardGO.AddComponent<ODBlurBackground>();
        cardGO.AddComponent<CanvasGroup>();
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.anchorMin     = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax     = new Vector2(0.5f, 0.5f);
        cardRT.pivot         = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta     = new Vector2(440f, 300f);
        cardRT.anchoredPosition = Vector2.zero;

        // Background
        GameObject bgGO    = new GameObject("Background");
        bgGO.transform.SetParent(cardGO.transform, false);
        RoundedImage bgImg  = bgGO.AddComponent<RoundedImage>();
        bgImg.color         = SurfaceColor;
        bgImg.cornerRadius  = 24f;
        bgImg.raycastTarget = true;
        ODBackground bg     = bgGO.AddComponent<ODBackground>();
        bg.backgroundStyle  = ODBackground.Style.Modal;
        RectTransform bgRT  = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Header
        GameObject header = new GameObject("Header");
        header.transform.SetParent(cardGO.transform, false);
        HorizontalLayoutGroup hlg = GetOrAdd<HorizontalLayoutGroup>(header);
        hlg.padding   = new RectOffset(20, 16, 16, 0);
        hlg.spacing   = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = false;
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = Vector2.one;
        headerRT.pivot     = new Vector2(0.5f, 1f);
        headerRT.sizeDelta = new Vector2(0f, 56f);
        headerRT.anchoredPosition = Vector2.zero;

        // Title
        GameObject titleGO       = new GameObject("Title");
        titleGO.transform.SetParent(header.transform, false);
        TextMeshProUGUI titleTmp  = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text      = "Modal Title";
        titleTmp.fontSize  = 28f;
        titleTmp.color     = TextPrimary;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        ODLabel titleLbl   = titleGO.AddComponent<ODLabel>();
        titleLbl.textStyle = ODLabel.TextStyle.H2;
        LayoutElement titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.flexibleWidth = 1f;
        titleGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 40f);

        // Divider
        GameObject divGO = new GameObject("Divider");
        divGO.transform.SetParent(cardGO.transform, false);
        Image divImg     = divGO.AddComponent<Image>();
        divImg.color     = BorderColor;
        divImg.raycastTarget = false;
        divGO.AddComponent<ODDivider>();
        RectTransform divRT = divGO.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0f, 1f);
        divRT.anchorMax = Vector2.one;
        divRT.pivot     = new Vector2(0.5f, 1f);
        divRT.anchoredPosition = new Vector2(0f, -56f);
        divRT.sizeDelta = new Vector2(0f, 1f);

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(cardGO.transform, false);
        VerticalLayoutGroup vlg = GetOrAdd<VerticalLayoutGroup>(content);
        vlg.padding   = new RectOffset(16, 16, 16, 16);
        vlg.spacing   = 12f;
        vlg.childControlWidth      = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        ContentSizeFitter csf = GetOrAdd<ContentSizeFitter>(content);
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = new Vector2(0f, -57f);

        card.titleLabel  = titleLbl;
        card.divider     = divGO;
        card.contentArea = contentRT;

        modal.overlay = overlayImg;
        modal.card    = card;

        Save(root, OrgPath + "ODModal.prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildODNavBar()
    {
        // ── Root ──────────────────────────────────────────────────────────────
        GameObject root  = new GameObject("ODNavBar", typeof(RectTransform));
        ODNavBar navbar  = root.AddComponent<ODNavBar>();
        root.AddComponent<ODBlurBackground>();
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(400f, 72f);

        // ── Background ────────────────────────────────────────────────────────
        GameObject bgGO    = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        RoundedImage bgImg  = bgGO.AddComponent<RoundedImage>();
        bgImg.color         = PanelBg;
        bgImg.cornerRadius  = 20f;
        bgImg.raycastTarget = false;
        ODBackground bg     = bgGO.AddComponent<ODBackground>();
        bg.backgroundStyle  = ODBackground.Style.Card;
        RectTransform bgRT  = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // ── ItemsContainer ────────────────────────────────────────────────────
        GameObject container = new GameObject("ItemsContainer");
        container.transform.SetParent(root.transform, false);
        HorizontalLayoutGroup hlg = GetOrAdd<HorizontalLayoutGroup>(container);
        hlg.padding                = new RectOffset(8, 8, 8, 8);
        hlg.spacing                = 0f;
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        RectTransform containerRT = container.GetComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;

        navbar.itemsContainer = container.transform;

        // ── 3 default NavItems (labels match ODNavBar default items list) ─────
        string[] defaultLabels = { "Home", "Race", "Profile" };
        for (int i = 0; i < 3; i++)
            BuildNavItem(container.transform, i, defaultLabels[i]);

        Save(root, OrgPath + "ODNavBar.prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildNavItem(Transform parent, int index, string defaultLabel = "")
    {
        GameObject itemGO = new GameObject("NavItem_" + index, typeof(RectTransform));
        itemGO.transform.SetParent(parent, false);
        itemGO.AddComponent<Button>();
        ODNavItem navItem = itemGO.AddComponent<ODNavItem>();

        // Icon
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(itemGO.transform, false);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.color          = TextSecondary;
        iconImg.preserveAspect = true;
        ODIcon ic = iconGO.AddComponent<ODIcon>();
        ic.tint   = TextSecondary;
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin        = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax        = new Vector2(0.5f, 0.5f);
        iconRT.pivot            = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = new Vector2(0f, 8f);
        iconRT.sizeDelta        = new Vector2(24f, 24f);

        // Label
        string labelText = string.IsNullOrEmpty(defaultLabel) ? "Tab " + index : defaultLabel;
        GameObject labelGO  = new GameObject("Label");
        labelGO.transform.SetParent(itemGO.transform, false);
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = labelText;
        tmp.fontSize  = 18f;
        tmp.color     = TextSecondary;
        tmp.alignment = TextAlignmentOptions.Center;
        ODLabel lbl   = labelGO.AddComponent<ODLabel>();
        lbl.textStyle = ODLabel.TextStyle.Caption;
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin        = new Vector2(0f, 0f);
        labelRT.anchorMax        = new Vector2(1f, 0f);
        labelRT.pivot            = new Vector2(0.5f, 0f);
        labelRT.anchoredPosition = new Vector2(0f, 4f);
        labelRT.sizeDelta        = new Vector2(0f, 20f);

        navItem.icon      = ic;
        navItem.label     = lbl;
        navItem.itemLabel = labelText;
    }

    // ── Level 3 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates Level3 prefabs (ODGoldBorder, ODLiveBadge, ODBackgroundMedia, ODTelemetryCell,
    /// ODTableRow, ODDataTable, ODDriverCard, ODMediaControls) and refreshes atoms + molecules
    /// with the current dark-theme color constants.
    /// </summary>
    [MenuItem("Overdrive/Build OD_UI Level3")]
    public static void BuildLevel3()
    {
        EnsureDirectories();
        EnsureUIThemeAsset();
        BuildODGoldBorder();
        BuildODLiveBadge();
        BuildODBackgroundMedia();
        BuildODTelemetryCell();
        BuildODTableRow();
        BuildODDataTable();
        BuildODDriverCard();
        BuildODMediaControls();
        // Refresh atoms + molecules with updated dark theme colours
        BuildAtomPrefabs();
        BuildMoleculePrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ODUIBuilder] Level3 prefabs built successfully.");
        EditorUtility.DisplayDialog("OD_UI Builder", "Level3 (8 prefabs) created in Assets/_Overdrive/UI/Prefabs/", "OK");
    }

    static void BuildODGoldBorder()
    {
        var go = new GameObject("ODGoldBorder", typeof(RectTransform));
        go.AddComponent<Image>();
        go.AddComponent<ODGoldBorder>();
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 100f);
        Save(go, AtomPath + "ODGoldBorder.prefab");
        Object.DestroyImmediate(go);
    }

    static void BuildODLiveBadge()
    {
        var root      = new GameObject("ODLiveBadge", typeof(RectTransform));
        ODLiveBadge badge = root.AddComponent<ODLiveBadge>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 28f);

        var hlg = GetOrAdd<HorizontalLayoutGroup>(root);
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.spacing                = 4f;
        hlg.padding                = new RectOffset(10, 10, 0, 0);
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // Background — pill, absolute (ignored by layout)
        var bgGO       = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        RoundedImage bgImg  = bgGO.AddComponent<RoundedImage>();
        bgImg.cornerRadius  = 14f;
        bgImg.color         = new Color(DangerColor.r, DangerColor.g, DangerColor.b, 0.20f);
        bgImg.raycastTarget = false;
        bgGO.AddComponent<LayoutElement>().ignoreLayout = true;
        var bgRT            = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Dot
        var dotGO = new GameObject("Dot");
        dotGO.transform.SetParent(root.transform, false);
        Image dotImg  = dotGO.AddComponent<Image>();
        dotImg.color  = DangerColor;
        var dotLE     = dotGO.AddComponent<LayoutElement>();
        dotLE.preferredWidth  = 8f;
        dotLE.preferredHeight = 8f;
        dotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(8f, 8f);

        // Label
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(root.transform, false);
        TextMeshProUGUI lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
        lblTmp.text      = "LIVE";
        lblTmp.fontSize  = 18f;
        lblTmp.color     = TextPrimary;
        lblTmp.fontStyle = FontStyles.Bold;
        lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
        var lblLE        = lblGO.AddComponent<LayoutElement>();
        lblLE.preferredWidth  = 34f;
        lblLE.preferredHeight = 20f;
        lblGO.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 20f);

        badge.background = bgImg;
        badge.dot        = dotImg;
        badge.badgeLabel = lblTmp;

        Save(root, AtomPath + "ODLiveBadge.prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildODBackgroundMedia()
    {
        var root  = new GameObject("ODBackgroundMedia", typeof(RectTransform));
        ODBackgroundMedia media = root.AddComponent<ODBackgroundMedia>();
        var rt    = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // MediaLayer
        var mlGO  = new GameObject("MediaLayer");
        mlGO.transform.SetParent(root.transform, false);
        RawImage mlImg  = mlGO.AddComponent<RawImage>();
        mlImg.color     = Color.white;
        var mlRT        = mlGO.GetComponent<RectTransform>();
        mlRT.anchorMin  = Vector2.zero;
        mlRT.anchorMax  = Vector2.one;
        mlRT.offsetMin  = mlRT.offsetMax = Vector2.zero;

        // BlurOverlay
        var blurGO  = new GameObject("BlurOverlay");
        blurGO.transform.SetParent(root.transform, false);
        RawImage blurImg  = blurGO.AddComponent<RawImage>();
        blurImg.color     = new Color(PanelBg.r, PanelBg.g, PanelBg.b, 0.65f);
        blurGO.AddComponent<ODBlurBackground>();
        var blurRT        = blurGO.GetComponent<RectTransform>();
        blurRT.anchorMin  = Vector2.zero;
        blurRT.anchorMax  = Vector2.one;
        blurRT.offsetMin  = blurRT.offsetMax = Vector2.zero;

        // GradientOverlay (bottom half darkening)
        var gradGO  = new GameObject("GradientOverlay");
        gradGO.transform.SetParent(root.transform, false);
        Image gradImg   = gradGO.AddComponent<Image>();
        gradImg.color   = new Color(0f, 0f, 0f, 0.80f);
        gradImg.raycastTarget = false;
        var gradRT      = gradGO.GetComponent<RectTransform>();
        gradRT.anchorMin = new Vector2(0f, 0f);
        gradRT.anchorMax = new Vector2(1f, 0.5f);
        gradRT.offsetMin = gradRT.offsetMax = Vector2.zero;

        media.mediaLayer      = mlImg;
        media.blurOverlay     = blurImg;
        media.gradientOverlay = gradImg;

        Save(root, AtomPath + "ODBackgroundMedia.prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildODTelemetryCell()
    {
        var root     = new GameObject("ODTelemetryCell", typeof(RectTransform));
        ODTelemetryCell cell = root.AddComponent<ODTelemetryCell>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 60f);

        var vlg = GetOrAdd<VerticalLayoutGroup>(root);
        vlg.childAlignment         = TextAnchor.MiddleCenter;
        vlg.spacing                = 2f;
        vlg.padding                = new RectOffset(0, 0, 4, 4);
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Label
        var lblGO   = new GameObject("Label");
        lblGO.transform.SetParent(root.transform, false);
        TextMeshProUGUI lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
        lblTmp.text      = "LABEL";
        lblTmp.fontSize  = 18f;
        lblTmp.color     = TextSecondary;
        lblTmp.alignment = TextAlignmentOptions.Center;
        lblGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 22f);

        // Value
        var valGO   = new GameObject("Value");
        valGO.transform.SetParent(root.transform, false);
        TextMeshProUGUI valTmp = valGO.AddComponent<TextMeshProUGUI>();
        valTmp.text      = "—";
        valTmp.fontSize  = 28f;
        valTmp.color     = TextPrimary;
        valTmp.fontStyle = FontStyles.Bold;
        valTmp.alignment = TextAlignmentOptions.Center;
        valGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 36f);

        cell.labelText = lblTmp;
        cell.valueText = valTmp;

        Save(root, AtomPath + "ODTelemetryCell.prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildODTableRow()
    {
        var root = new GameObject("ODTableRow", typeof(RectTransform));
        root.AddComponent<CanvasGroup>();
        // Add HLG before ODTableRow so [RequireComponent] doesn't add a second one
        var hlg = GetOrAdd<HorizontalLayoutGroup>(root);
        ODTableRow row = root.AddComponent<ODTableRow>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 44f);

        hlg.spacing                = 0f;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.padding                = new RectOffset(12, 12, 0, 0);

        // Background — absolute, ignored by layout
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.clear;
        bgGO.AddComponent<LayoutElement>().ignoreLayout = true;
        var bgRT    = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // AccentBorder — 3 px left strip, absolute
        var abGO = new GameObject("AccentBorder");
        abGO.transform.SetParent(root.transform, false);
        Image abImg = abGO.AddComponent<Image>();
        abImg.color = GoldColor;
        abGO.AddComponent<LayoutElement>().ignoreLayout = true;
        var abRT    = abGO.GetComponent<RectTransform>();
        abRT.anchorMin = new Vector2(0f, 0f);
        abRT.anchorMax = new Vector2(0f, 1f);
        abRT.sizeDelta = new Vector2(3f, 0f);
        abRT.offsetMin = abRT.offsetMax = Vector2.zero;
        abGO.SetActive(false);

        row.bgImage      = bgImg;
        row.accentBorder = abImg;

        Save(root, OrgPath + "ODTableRow.prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildODDataTable()
    {
        var root  = new GameObject("ODDataTable", typeof(RectTransform));
        ODDataTable table = root.AddComponent<ODDataTable>();
        // Width is fixed; height auto-calculated by ContentSizeFitter as rows are added
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 0f);

        // Root stacks Header → Divider → RowsContainer vertically and grows with content
        var rootVlg = root.AddComponent<VerticalLayoutGroup>();
        rootVlg.childForceExpandWidth  = true;
        rootVlg.childForceExpandHeight = false;
        rootVlg.childControlWidth      = true;
        rootVlg.childControlHeight     = false;
        rootVlg.spacing                = 0f;
        root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Background — absolute fill, excluded from the root VLG
        var bgGO   = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        RoundedImage bgImg  = bgGO.AddComponent<RoundedImage>();
        bgImg.color         = PanelBg;
        bgImg.cornerRadius  = 16f;
        bgImg.raycastTarget = false;
        ODBackground bg     = bgGO.AddComponent<ODBackground>();
        var bgRT            = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<LayoutElement>().ignoreLayout = true;

        // GoldBorder — absolute fill, excluded from the root VLG
        var gbGO = new GameObject("GoldBorder");
        gbGO.transform.SetParent(root.transform, false);
        gbGO.AddComponent<Image>();
        ODGoldBorder gb = gbGO.AddComponent<ODGoldBorder>();
        var gbRT = gbGO.GetComponent<RectTransform>();
        gbRT.anchorMin = Vector2.zero;
        gbRT.anchorMax = Vector2.one;
        gbRT.offsetMin = gbRT.offsetMax = Vector2.zero;
        gbGO.AddComponent<LayoutElement>().ignoreLayout = true;

        // HeaderRow — layout-driven; height fixed at 36 px via LayoutElement
        var headerGO  = new GameObject("HeaderRow");
        headerGO.transform.SetParent(root.transform, false);
        var headerHlg = GetOrAdd<HorizontalLayoutGroup>(headerGO);
        headerHlg.childForceExpandHeight = true;
        headerHlg.childForceExpandWidth  = false;
        headerHlg.padding                = new RectOffset(12, 12, 0, 0);
        var headerRT  = headerGO.GetComponent<RectTransform>();
        headerGO.AddComponent<LayoutElement>().preferredHeight = 36f;

        // HeaderDivider — 1 px separator between header and rows
        var hdivGO  = new GameObject("HeaderDivider");
        hdivGO.transform.SetParent(root.transform, false);
        Image hdivImg     = hdivGO.AddComponent<Image>();
        hdivImg.color     = BorderColor;
        hdivImg.raycastTarget = false;
        hdivGO.AddComponent<LayoutElement>().preferredHeight = 1f;

        // RowsContainer — grows as AppendRow() adds children
        var rowsGO  = new GameObject("RowsContainer");
        rowsGO.transform.SetParent(root.transform, false);
        var rowsVlg = GetOrAdd<VerticalLayoutGroup>(rowsGO);
        rowsVlg.spacing                = 0f;
        rowsVlg.childForceExpandWidth  = true;
        rowsVlg.childForceExpandHeight = false;
        rowsVlg.childControlHeight     = false;
        GetOrAdd<ContentSizeFitter>(rowsGO).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var rowsRT  = rowsGO.GetComponent<RectTransform>();

        table.background    = bg;
        table.goldBorder    = gb;
        table.headerRow     = headerRT;
        table.rowsContainer = rowsRT;

        Save(root, OrgPath + "ODDataTable.prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildODDriverCard()
    {
        var root  = new GameObject("ODDriverCard", typeof(RectTransform));
        ODDriverCard card = root.AddComponent<ODDriverCard>();
        // Width is fixed; height grows with telemetry cell count via ContentSizeFitter
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 0f);

        // Root stacks Header → Divider → TelemetryGrid vertically and grows with them
        var rootVlg = root.AddComponent<VerticalLayoutGroup>();
        rootVlg.childForceExpandWidth  = true;
        rootVlg.childForceExpandHeight = false;
        rootVlg.childControlWidth      = true;
        rootVlg.childControlHeight     = false;
        rootVlg.spacing                = 0f;
        root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Background — absolute fill, excluded from root VLG
        var bgGO   = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        RoundedImage bgImg  = bgGO.AddComponent<RoundedImage>();
        bgImg.color         = PanelBg;
        bgImg.cornerRadius  = 24f;
        bgImg.raycastTarget = false;
        ODBackground bg     = bgGO.AddComponent<ODBackground>();
        bg.backgroundStyle  = ODBackground.Style.Card;
        var bgRT            = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<LayoutElement>().ignoreLayout = true;

        // GoldBorder — absolute fill, excluded from root VLG
        var gbGO = new GameObject("GoldBorder");
        gbGO.transform.SetParent(root.transform, false);
        gbGO.AddComponent<Image>();
        ODGoldBorder gb = gbGO.AddComponent<ODGoldBorder>();
        var gbRT = gbGO.GetComponent<RectTransform>();
        gbRT.anchorMin = Vector2.zero;
        gbRT.anchorMax = Vector2.one;
        gbRT.offsetMin = gbRT.offsetMax = Vector2.zero;
        gbGO.AddComponent<LayoutElement>().ignoreLayout = true;

        // TeamColorBar — 4 px left strip, absolute, excluded from root VLG
        var tcbGO  = new GameObject("TeamColorBar");
        tcbGO.transform.SetParent(root.transform, false);
        Image tcbImg   = tcbGO.AddComponent<Image>();
        tcbImg.color   = GoldColor;
        var tcbRT      = tcbGO.GetComponent<RectTransform>();
        tcbRT.anchorMin = new Vector2(0f, 0f);
        tcbRT.anchorMax = new Vector2(0f, 1f);
        tcbRT.sizeDelta = new Vector2(4f, 0f);
        tcbRT.offsetMin = tcbRT.offsetMax = Vector2.zero;
        tcbGO.AddComponent<LayoutElement>().ignoreLayout = true;

        // Header — layout-driven, fixed preferred height of 64 px
        var headerGO  = new GameObject("Header");
        headerGO.transform.SetParent(root.transform, false);
        var headerHlg = GetOrAdd<HorizontalLayoutGroup>(headerGO);
        headerHlg.padding                = new RectOffset(20, 20, 16, 0);
        headerHlg.spacing                = 12f;
        headerHlg.childAlignment         = TextAnchor.MiddleLeft;
        headerHlg.childForceExpandWidth  = false;
        headerHlg.childForceExpandHeight = false;
        headerGO.AddComponent<LayoutElement>().preferredHeight = 64f;

        // DriverNumber
        var numGO   = new GameObject("DriverNumber");
        numGO.transform.SetParent(headerGO.transform, false);
        TextMeshProUGUI numTmp = numGO.AddComponent<TextMeshProUGUI>();
        numTmp.text      = "44";
        numTmp.fontSize  = 36f;
        numTmp.color     = GoldColor;
        numTmp.fontStyle = FontStyles.Bold;
        numTmp.alignment = TextAlignmentOptions.MidlineLeft;
        numGO.GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 48f);

        // Logo
        var logoGO = new GameObject("Logo");
        logoGO.transform.SetParent(headerGO.transform, false);
        Image logoImg   = logoGO.AddComponent<Image>();
        logoImg.preserveAspect = true;
        logoImg.color   = Color.white;
        logoGO.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        logoGO.GetComponent<RectTransform>().sizeDelta = new Vector2(48f, 48f);

        // DriverName
        var nameGO  = new GameObject("DriverName");
        nameGO.transform.SetParent(headerGO.transform, false);
        TextMeshProUGUI nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
        nameTmp.text      = "Driver Name";
        nameTmp.fontSize  = 22f;
        nameTmp.color     = TextPrimary;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
        nameGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 40f);
        nameGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Divider — 1 px, layout-driven
        var divGO   = new GameObject("Divider");
        divGO.transform.SetParent(root.transform, false);
        Image divImg = divGO.AddComponent<Image>();
        divImg.color = BorderColor;
        divImg.raycastTarget = false;
        divGO.AddComponent<LayoutElement>().preferredHeight = 1f;

        // TelemetryGrid — 2-column GridLayout; CSF expands height as cells are added
        var gridGO  = new GameObject("TelemetryGrid");
        gridGO.transform.SetParent(root.transform, false);
        GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(gridGO);
        grid.cellSize        = new Vector2(140f, 60f);
        grid.spacing         = new Vector2(12f, 8f);
        grid.padding         = new RectOffset(20, 20, 12, 12);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        GetOrAdd<ContentSizeFitter>(gridGO).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var gridRT  = gridGO.GetComponent<RectTransform>();

        card.background       = bg;
        card.goldBorder       = gb;
        card.teamColorBar     = tcbImg;
        card.driverNumberText = numTmp;
        card.teamLogoImage    = logoImg;
        card.driverNameText   = nameTmp;
        card.divider          = divImg;
        card.telemetryGrid    = gridRT;

        Save(root, OrgPath + "ODDriverCard.prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildODMediaControls()
    {
        var root  = new GameObject("ODMediaControls", typeof(RectTransform));
        ODMediaControls ctrl = root.AddComponent<ODMediaControls>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 72f);

        // Background — pill shape (cornerRadius = 36)
        var bgGO   = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        RoundedImage bgImg  = bgGO.AddComponent<RoundedImage>();
        bgImg.color         = PanelBg;
        bgImg.cornerRadius  = 36f;
        bgImg.raycastTarget = false;
        bgGO.AddComponent<ODBackground>();
        var bgRT            = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // GoldBorder
        var gbGO = new GameObject("GoldBorder");
        gbGO.transform.SetParent(root.transform, false);
        gbGO.AddComponent<Image>();
        gbGO.AddComponent<ODGoldBorder>();
        var gbRT = gbGO.GetComponent<RectTransform>();
        gbRT.anchorMin = Vector2.zero;
        gbRT.anchorMax = Vector2.one;
        gbRT.offsetMin = gbRT.offsetMax = Vector2.zero;

        // Container
        var containerGO = new GameObject("Container");
        containerGO.transform.SetParent(root.transform, false);
        var hlg = GetOrAdd<HorizontalLayoutGroup>(containerGO);
        hlg.padding                = new RectOffset(16, 16, 12, 12);
        hlg.spacing                = 8f;
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = true;
        var containerRT = containerGO.GetComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = containerRT.offsetMax = Vector2.zero;

        // Transport buttons
        ODButton rewindBtn    = BuildInlineODButton(containerGO.transform, "RewindBtn",    ODButton.ButtonStyle.Ghost,   "↺", 48f);
        ODButton playPauseBtn = BuildInlineODButton(containerGO.transform, "PlayPauseBtn", ODButton.ButtonStyle.Primary, "▶", 48f);
        ODButton forwardBtn   = BuildInlineODButton(containerGO.transform, "ForwardBtn",   ODButton.ButtonStyle.Ghost,   "↻", 48f);

        // Separator
        var sepGO  = new GameObject("Separator");
        sepGO.transform.SetParent(containerGO.transform, false);
        Image sepImg  = sepGO.AddComponent<Image>();
        sepImg.color  = BorderColor;
        var sepLE     = sepGO.AddComponent<LayoutElement>();
        sepLE.preferredWidth  = 1f;
        sepLE.preferredHeight = 32f;
        sepGO.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 32f);

        // LiveBadge (inline)
        var lbGO      = new GameObject("LiveBadge", typeof(RectTransform));
        lbGO.transform.SetParent(containerGO.transform, false);
        ODLiveBadge liveBadge = lbGO.AddComponent<ODLiveBadge>();
        lbGO.GetComponent<RectTransform>().sizeDelta = new Vector2(90f, 28f);
        var lbLE = lbGO.AddComponent<LayoutElement>();
        lbLE.preferredWidth  = 90f;
        lbLE.preferredHeight = 28f;

        var lbHlg = GetOrAdd<HorizontalLayoutGroup>(lbGO);
        lbHlg.childAlignment         = TextAnchor.MiddleCenter;
        lbHlg.spacing                = 4f;
        lbHlg.padding                = new RectOffset(8, 8, 0, 0);
        lbHlg.childForceExpandWidth  = false;
        lbHlg.childForceExpandHeight = true;

        var lbBgGO      = new GameObject("Background");
        lbBgGO.transform.SetParent(lbGO.transform, false);
        RoundedImage lbBgImg  = lbBgGO.AddComponent<RoundedImage>();
        lbBgImg.cornerRadius  = 14f;
        lbBgImg.color         = new Color(DangerColor.r, DangerColor.g, DangerColor.b, 0.20f);
        lbBgImg.raycastTarget = false;
        lbBgGO.AddComponent<LayoutElement>().ignoreLayout = true;
        var lbBgRT      = lbBgGO.GetComponent<RectTransform>();
        lbBgRT.anchorMin = Vector2.zero;
        lbBgRT.anchorMax = Vector2.one;
        lbBgRT.offsetMin = lbBgRT.offsetMax = Vector2.zero;

        var lbDotGO     = new GameObject("Dot");
        lbDotGO.transform.SetParent(lbGO.transform, false);
        Image lbDotImg  = lbDotGO.AddComponent<Image>();
        lbDotImg.color  = DangerColor;
        var lbDotLE     = lbDotGO.AddComponent<LayoutElement>();
        lbDotLE.preferredWidth  = 8f;
        lbDotLE.preferredHeight = 8f;
        lbDotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(8f, 8f);

        var lbLblGO     = new GameObject("Label");
        lbLblGO.transform.SetParent(lbGO.transform, false);
        TextMeshProUGUI lbTmp = lbLblGO.AddComponent<TextMeshProUGUI>();
        lbTmp.text      = "LIVE";
        lbTmp.fontSize  = 18f;
        lbTmp.color     = TextPrimary;
        lbTmp.fontStyle = FontStyles.Bold;
        lbTmp.alignment = TextAlignmentOptions.MidlineLeft;
        lbLblGO.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 20f);

        liveBadge.background = lbBgImg;
        liveBadge.dot        = lbDotImg;
        liveBadge.badgeLabel = lbTmp;

        // Flexible spacer before slider
        var spacerGO = new GameObject("Spacer");
        spacerGO.transform.SetParent(containerGO.transform, false);
        spacerGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Progress Slider
        Slider slider = BuildSlider(containerGO.transform);

        ctrl.background     = bgImg;
        ctrl.rewindBtn      = rewindBtn;
        ctrl.playPauseBtn   = playPauseBtn;
        ctrl.forwardBtn     = forwardBtn;
        ctrl.liveBadge      = liveBadge;
        ctrl.progressSlider = slider;

        Save(root, OrgPath + "ODMediaControls.prefab");
        Object.DestroyImmediate(root);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────────

    /// <summary>Creates an ODButton inline (no prefab save). Used inside composite organisms.</summary>
    static ODButton BuildInlineODButton(Transform parent, string name,
                                        ODButton.ButtonStyle style, string btnLabel, float size = 48f)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        root.AddComponent<CanvasGroup>();
        ODButton btn    = root.AddComponent<ODButton>();
        btn.buttonStyle = style;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);
        var le = root.AddComponent<LayoutElement>();
        le.preferredWidth  = size;
        le.preferredHeight = size;

        // Background
        var bgGO   = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        RoundedImage bgImg  = bgGO.AddComponent<RoundedImage>();
        bgImg.cornerRadius  = size * 0.5f;
        bgImg.raycastTarget = false;
        ODBackground bg     = bgGO.AddComponent<ODBackground>();
        switch (style)
        {
            case ODButton.ButtonStyle.Primary: bgImg.color = GoldColor;   break;
            case ODButton.ButtonStyle.Ghost:   bgImg.color = Color.clear; break;
            case ODButton.ButtonStyle.Danger:  bgImg.color = DangerColor; break;
        }
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Label
        var lblGO  = new GameObject("Label");
        lblGO.transform.SetParent(root.transform, false);
        TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = btnLabel;
        tmp.fontSize  = 22f;
        tmp.color     = (style == ODButton.ButtonStyle.Ghost) ? TextPrimary : Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        ODLabel lbl   = lblGO.AddComponent<ODLabel>();
        lbl.textStyle = ODLabel.TextStyle.Body;
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

        btn.background = bg;
        btn.label      = lbl;
        return btn;
    }

    /// <summary>Creates a basic Unity Slider with Fill and Handle. Returns the Slider component.</summary>
    static Slider BuildSlider(Transform parent)
    {
        var root  = new GameObject("ProgressSlider", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        Slider slider    = root.AddComponent<Slider>();
        slider.minValue  = 0f;
        slider.maxValue  = 1f;
        slider.value     = 0f;
        slider.direction = Slider.Direction.LeftToRight;
        var le = root.AddComponent<LayoutElement>();
        le.flexibleWidth   = 1f;
        le.preferredHeight = 8f;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 8f);

        // Background track
        var bgGO   = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = BorderColor;
        var bgRT    = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Fill Area
        var faGO   = new GameObject("Fill Area", typeof(RectTransform));
        faGO.transform.SetParent(root.transform, false);
        var faRT   = faGO.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero;
        faRT.anchorMax = Vector2.one;
        faRT.offsetMin = Vector2.zero;
        faRT.offsetMax = new Vector2(-10f, 0f);

        // Fill
        var fillGO   = new GameObject("Fill");
        fillGO.transform.SetParent(faGO.transform, false);
        Image fillImg  = fillGO.AddComponent<Image>();
        fillImg.color  = GoldColor;
        var fillRT     = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        // Handle Slide Area
        var haGO   = new GameObject("Handle Slide Area", typeof(RectTransform));
        haGO.transform.SetParent(root.transform, false);
        var haRT   = haGO.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero;
        haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10f, 0f);
        haRT.offsetMax = new Vector2(-10f, 0f);

        // Handle
        var handleGO  = new GameObject("Handle");
        handleGO.transform.SetParent(haGO.transform, false);
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = GoldBorderColorA;
        var handleRT    = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0f, 0.5f);
        handleRT.anchorMax = new Vector2(0f, 0.5f);
        handleRT.sizeDelta = new Vector2(16f, 16f);

        slider.fillRect      = fillRT;
        slider.handleRect    = handleRT;
        slider.targetGraphic = handleImg;

        return slider;
    }

    // ── Utilities ────────────────────────────────────────────────────────────────

    static T GetOrAdd<T>(GameObject go) where T : Component
        => go.GetComponent<T>() ?? go.AddComponent<T>();

    static void Save(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Debug.Log("[ODUIBuilder] Saved: " + path);
    }
}
