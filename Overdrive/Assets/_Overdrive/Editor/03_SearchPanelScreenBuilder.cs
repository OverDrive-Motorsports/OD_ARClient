using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BUILD ORDER — Tier 03 (Screen sub-builder). Requires: 02_MainMenuScreenBuilder
/// must have already saved "OverdriveMenuCanvas/OverdriveMenuPanel" in the scene —
/// this class finds that path by name and aborts with an error if it's missing.
///
/// Upgrades the SearchBar into a working TMP_InputField and builds the
/// SearchOverlay (filter chips + results list + empty-state) beneath it.
/// Adds & auto-wires SearchController. Re-runnable.
/// </summary>
public static class SearchPanelScreenBuilder
{
    const string PANEL = "OverdriveMenuCanvas/OverdriveMenuPanel";

    static Color Overlay;
    static Color Field;
    static Color ChipOff;
    static Color Gold;
    static Color Txt;
    static Color Sub;

    static readonly string[] Chips = { "All", "Races", "Drivers", "Teams", "Circuits", "Seasons" };

    static void PullTheme()
    {
        UITheme theme = UITheme.Instance;
        UITheme fallback = ScriptableObject.CreateInstance<UITheme>();
        if (theme == null) theme = fallback;

        Overlay = theme.panelBackgroundAlt;
        Field = theme.surfaceColor;
        ChipOff = theme.panelBackground;
        Gold = theme.accentGold;
        Txt = theme.textPrimary;
        Sub = theme.textSecondary;

        Object.DestroyImmediate(fallback);
    }

    public static void Build()
    {
        PullTheme();

        var panel = GameObject.Find(PANEL);
        if (panel == null) { Debug.LogError("[Search] Panel not found"); return; }

        UpgradeSearchBar(panel.transform);
        BuildOverlay(panel.transform);

        // Attach controller
        if (panel.GetComponent<SearchController>() == null)
            panel.AddComponent<SearchController>();

        EditorUtility.SetDirty(panel);
        Debug.Log("[Search] Command bar built!");
    }

    // ── SearchBar → TMP_InputField ──────────────────────────────────────────
    static void UpgradeSearchBar(Transform panel)
    {
        var bar = panel.Find("SearchBar");
        if (bar == null) { Debug.LogError("[Search] SearchBar missing"); return; }

        // clear old children (e.g. the old "SearchIcon")
        for (int i = bar.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(bar.GetChild(i).gameObject);

        // remove existing input field if re-running
        var oldIF = bar.GetComponent<TMP_InputField>();
        if (oldIF != null) Object.DestroyImmediate(oldIF);

        var barGO = bar.gameObject;
        var bg = barGO.GetComponent<Graphic>(); // RoundedImage already there

        // magnifier icon (left)
        var icon = Label(bar, "Icon", "🔍", 18f, FontStyles.Normal, Sub, TextAlignmentOptions.Center);
        var ir = icon.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0, 0); ir.anchorMax = new Vector2(0, 1); ir.pivot = new Vector2(0, 0.5f);
        ir.anchoredPosition = new Vector2(14, 0); ir.sizeDelta = new Vector2(28, 0);

        // mic button (right) – visual placeholder for future voice input
        var mic = Label(bar, "Mic", "🎙", 18f, FontStyles.Normal, Gold, TextAlignmentOptions.Center);
        var mr = mic.GetComponent<RectTransform>();
        mr.anchorMin = new Vector2(1, 0); mr.anchorMax = new Vector2(1, 1); mr.pivot = new Vector2(1, 0.5f);
        mr.anchoredPosition = new Vector2(-14, 0); mr.sizeDelta = new Vector2(30, 0);

        // Text Area (viewport)
        var area = NewGO("TextArea", bar);
        var ar = area.GetComponent<RectTransform>();
        ar.anchorMin = Vector2.zero; ar.anchorMax = Vector2.one;
        ar.offsetMin = new Vector2(48, 2); ar.offsetMax = new Vector2(-50, -2);
        area.AddComponent<RectMask2D>();

        var placeholder = Label(area.transform, "Placeholder",
            "Search races, drivers, teams…", 16f, FontStyles.Italic, Sub, TextAlignmentOptions.MidlineLeft);
        Stretch(placeholder);
        var textComp = Label(area.transform, "Text", "", 16f, FontStyles.Normal, Txt, TextAlignmentOptions.MidlineLeft);
        Stretch(textComp);

        // InputField
        var inputField = barGO.AddComponent<TMP_InputField>();
        inputField.targetGraphic = bg;
        inputField.textViewport = ar;
        inputField.textComponent = textComp.GetComponent<TextMeshProUGUI>();
        inputField.placeholder = placeholder.GetComponent<TextMeshProUGUI>();
        inputField.fontAsset = textComp.GetComponent<TextMeshProUGUI>().font;
        inputField.pointSize = 16f;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.customCaretColor = true;
        inputField.caretColor = Gold;
        inputField.selectionColor = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
    }

    // ── Overlay ───────────────────────────────────────────────────────────────
    static void BuildOverlay(Transform panel)
    {
        var old = panel.Find("SearchOverlay");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // Match the search bar geometry (centered, +130 x, 800 wide), open below
        var overlay = NewGO("SearchOverlay", panel);
        var or = overlay.GetComponent<RectTransform>();
        or.anchorMin = new Vector2(0.5f, 1); or.anchorMax = new Vector2(0.5f, 1); or.pivot = new Vector2(0.5f, 1);
        or.anchoredPosition = new Vector2(130, -72); or.sizeDelta = new Vector2(800, 440);
        Rounded(overlay, Overlay, 22f).raycastTarget = true; // catches clicks, blocks behind

        // vertical stack: chips row + body
        var vlg = overlay.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16); vlg.spacing = 14;
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true; vlg.childForceExpandHeight = false;

        BuildChips(overlay.transform);
        BuildResults(overlay.transform);
        BuildEmptyState(overlay.transform);
    }

    static void BuildChips(Transform parent)
    {
        var row = NewGO("Chips", parent);
        row.AddComponent<LayoutElement>().preferredHeight = 40f;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8; hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
        hlg.childControlHeight = true; hlg.childForceExpandHeight = true;

        foreach (var name in Chips)
        {
            var chip = NewGO("Chip_" + name, row.transform);
            var bg = Rounded(chip, ChipOff, 14f); bg.raycastTarget = true;
            var le = chip.AddComponent<LayoutElement>();
            le.minWidth = 84f; le.preferredWidth = 84f;
            var btn = chip.AddComponent<Button>(); btn.targetGraphic = bg;

            var lbl = Label(chip.transform, "Label", name, 14f, FontStyles.Bold, Txt, TextAlignmentOptions.Center);
            Stretch(lbl);
        }
    }

    static void BuildResults(Transform parent)
    {
        var scroll = NewGO("Results", parent);
        scroll.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var sr = scroll.AddComponent<ScrollRect>(); sr.horizontal = false;
        scroll.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        var vp = NewGO("Viewport", scroll.transform);
        Stretch(vp);
        // RectMask2D: never Mask + transparent Image (mesh culled → children hidden)
        vp.AddComponent<RectMask2D>();
        sr.viewport = vp.GetComponent<RectTransform>();

        var content = NewGO("Content", vp.transform);
        var cr = content.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1); cr.pivot = new Vector2(0.5f, 1);
        cr.anchoredPosition = Vector2.zero; cr.sizeDelta = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = cr;

        // "no results" label (centered, hidden by default at runtime)
        var nr = Label(scroll.transform, "NoResults", "No results", 16f, FontStyles.Italic, Sub, TextAlignmentOptions.Center);
        Stretch(nr);
        nr.SetActive(false);

        scroll.SetActive(false); // controller toggles
    }

    static void BuildEmptyState(Transform parent)
    {
        var empty = NewGO("EmptyState", parent);
        empty.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var vlg = empty.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10; vlg.padding = new RectOffset(2, 2, 4, 2);
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false; vlg.childForceExpandHeight = false;

        var recentTitle = Label(empty.transform, "RecentTitle", "RECENT", 13f, FontStyles.Bold, Sub, TextAlignmentOptions.MidlineLeft);
        recentTitle.AddComponent<LayoutElement>().preferredHeight = 22f;

        var recentList = NewGO("RecentList", empty.transform);
        recentList.AddComponent<LayoutElement>().flexibleHeight = 0f;
        var rvlg = recentList.AddComponent<VerticalLayoutGroup>();
        rvlg.spacing = 8; rvlg.childControlWidth = true; rvlg.childForceExpandWidth = true;
        rvlg.childControlHeight = false; rvlg.childForceExpandHeight = false;
        recentList.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var tryTitle = Label(empty.transform, "TryTitle", "TRY", 13f, FontStyles.Bold, Sub, TextAlignmentOptions.MidlineLeft);
        tryTitle.AddComponent<LayoutElement>().preferredHeight = 22f;

        var suggest = NewGO("Suggestions", empty.transform);
        suggest.AddComponent<LayoutElement>().preferredHeight = 40f;
        var shlg = suggest.AddComponent<HorizontalLayoutGroup>();
        shlg.spacing = 8; shlg.childAlignment = TextAnchor.MiddleLeft;
        shlg.childControlWidth = true; shlg.childForceExpandWidth = false;
        shlg.childControlHeight = true; shlg.childForceExpandHeight = true;

        string[] suggestions = { "Wet races", "2024 podiums", "Verstappen wins", "Monaco" };
        foreach (var s in suggestions)
        {
            var chip = NewGO("Sugg_" + s, suggest.transform);
            var bg = Rounded(chip, ChipOff, 14f); bg.raycastTarget = true;
            var le = chip.AddComponent<LayoutElement>(); le.minWidth = 120f;
            var hlg = chip.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 0, 0); hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true; hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true; hlg.childForceExpandHeight = true;
            var lbl = Label(chip.transform, "Label", s, 14f, FontStyles.Normal, Txt, TextAlignmentOptions.Center);
            lbl.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
        }
    }

    // ── primitives ────────────────────────────────────────────────────────────
    static GameObject NewGO(string n, Transform p)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static RoundedImage Rounded(GameObject go, Color c, float r)
    {
        var img = go.AddComponent<RoundedImage>();
        img.color = c; img.cornerRadius = r; img.cornerSegments = 12;
        img.raycastTarget = false;
        return img;
    }

    static GameObject Label(Transform p, string name, string txt, float size,
        FontStyles style, Color color, TextAlignmentOptions align)
    {
        var go = NewGO(name, p);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = size; t.fontStyle = style;
        t.color = color; t.alignment = align; t.raycastTarget = false;
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
