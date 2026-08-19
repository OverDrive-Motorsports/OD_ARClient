using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BUILD ORDER — Tier 03 (Screen sub-builder), but standalone: builds its own
/// canvas from scratch and doesn't depend on 02_MainMenuScreenBuilder or any
/// 01_ODUIBuilder prefab. Safe to run any time.
///
/// Builds the live race standings widget (liquid-glass vertical panel):
/// F1 RACE header, LAP counter, and 20 driver rows driven at runtime by
/// RaceRankingManager (random overtakes + lap progression). Movable via
/// WindowHandle. Explicit fixed positions everywhere — no layout groups.
/// Re-runnable.
/// </summary>
public static class RankingWidgetScreenBuilder
{
    // ── layout ────────────────────────────────────────────────────────────────
    const float W = 420f, H = 1500f;
    const float ROW_H = 56f, ROW_GAP = 8f, SIDE_PAD = 16f;

    // ── palette (liquid glass) — Glass/RowBg/Txt pulled from UITheme.Instance,
    // F1Red stays a literal since it's the F1 brand mark, not a themeable UI color.
    static Color Glass;
    static Color RowBg;
    static readonly Color F1Red = new Color(0.88f, 0.06f, 0.10f, 1f);
    static Color Txt;

    static void PullTheme()
    {
        UITheme theme = UITheme.Instance;
        UITheme fallback = ScriptableObject.CreateInstance<UITheme>();
        if (theme == null) theme = fallback;

        Glass = theme.panelBackground;
        RowBg = Color.Lerp(theme.panelBackground, Color.black, 0.3f);
        Txt = theme.textPrimary;

        Object.DestroyImmediate(fallback);
    }

    public static void Build()
    {
        PullTheme();

        // remove old (also inactive)
        foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
            if (c != null && c.gameObject.name == "RankingWidget" && c.gameObject.scene.IsValid())
                Object.DestroyImmediate(c.gameObject);

        // ── canvas root ───────────────────────────────────────────────────────
        var root = new GameObject("RankingWidget");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();
        var rootRT = root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(W, H);
        rootRT.localScale = Vector3.one * 0.001f;
        root.transform.position = new Vector3(0.85f, 1.15f, 1.5f);

        // glass background
        var bg = NewGO("Glass", root.transform);
        StretchRT(bg);
        Rounded(bg, Glass, 32f);

        // ── header : [F1] RACE ────────────────────────────────────────────────
        // group centered: badge (76) + gap (14) + "RACE" (~118) = 208
        float groupX = (W - 208f) * 0.5f;

        var badge = NewGO("F1Badge", root.transform);
        TL(badge, groupX, -26f, 76f, 40f);
        Rounded(badge, F1Red, 9f);
        var f1 = Label(badge.transform, "F1Text", "F1", 26f,
            FontStyles.Bold | FontStyles.Italic, Color.white, TextAlignmentOptions.Center);
        StretchRT(f1);

        var race = Label(root.transform, "RaceLabel", "RACE", 36f,
            FontStyles.Bold, Txt, TextAlignmentOptions.MidlineLeft);
        TL(race, groupX + 90f, -24f, 140f, 44f);

        // ── LAP counter ───────────────────────────────────────────────────────
        var lap = Label(root.transform, "LapText", "LAP 1/54", 27f,
            FontStyles.Bold | FontStyles.Italic, Txt, TextAlignmentOptions.Center);
        TL(lap, 0f, -78f, W, 40f);

        // ── rows container ────────────────────────────────────────────────────
        var rows = NewGO("Rows", root.transform);
        var rowsRT = rows.GetComponent<RectTransform>();
        rowsRT.anchorMin = new Vector2(0, 1); rowsRT.anchorMax = new Vector2(1, 1);
        rowsRT.pivot = new Vector2(0.5f, 1);
        rowsRT.anchoredPosition = new Vector2(0, -136f);
        rowsRT.sizeDelta = new Vector2(0, H - 136f - 20f);

        // ── row template ──────────────────────────────────────────────────────
        var row = BuildRowTemplate(rows.transform);

        // ── manager wiring ────────────────────────────────────────────────────
        var mgr = root.AddComponent<RaceRankingManager>();
        mgr.cardTemplate = row;
        mgr.container = rowsRT;
        mgr.lapText = lap.GetComponent<TextMeshProUGUI>();
        mgr.rowHeight = ROW_H;
        mgr.rowSpacing = ROW_GAP;

        row.SetActive(false);

        // draggable pill
        root.AddComponent<WindowHandle>();

        EditorUtility.SetDirty(root);
        Debug.Log("[Ranking] Widget built — enters simulation at Play.");
        Selection.activeGameObject = root;
    }

    // ── row template ──────────────────────────────────────────────────────────
    static GameObject BuildRowTemplate(Transform parent)
    {
        var row = NewGO("RowTemplate", parent);
        var rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(-SIDE_PAD * 2f, ROW_H);
        Rounded(row, RowBg, ROW_H * 0.5f);

        // rank number (left)
        var rank = Label(row.transform, "RankText", "1", 21f,
            FontStyles.Bold, Txt, TextAlignmentOptions.Center);
        VLeft(rank, 14f, 36f, 30f);

        // white circle + team-colored dot
        var circle = NewGO("LogoCircle", row.transform);
        VLeft(circle, 54f, 36f, 36f);
        Rounded(circle, Color.white, 18f);
        var dot = NewGO("TeamDot", circle.transform);
        var dRT = dot.GetComponent<RectTransform>();
        dRT.anchorMin = dRT.anchorMax = new Vector2(0.5f, 0.5f);
        dRT.pivot = new Vector2(0.5f, 0.5f);
        dRT.anchoredPosition = Vector2.zero;
        dRT.sizeDelta = new Vector2(20f, 20f);
        var dotImg = dot.AddComponent<RoundedImage>();
        dotImg.color = Color.gray; dotImg.cornerRadius = 10f;
        dotImg.cornerSegments = 12; dotImg.raycastTarget = false;

        // driver code
        var code = Label(row.transform, "CodeText", "DRV", 22f,
            FontStyles.Bold, Txt, TextAlignmentOptions.MidlineLeft);
        VLeft(code, 104f, 140f, 32f);

        // gap (right-aligned)
        var gap = Label(row.transform, "GapText", "+0.000", 20f,
            FontStyles.Bold, Txt, TextAlignmentOptions.MidlineRight);
        var gRT = gap.GetComponent<RectTransform>();
        gRT.anchorMin = new Vector2(1, 0.5f); gRT.anchorMax = new Vector2(1, 0.5f);
        gRT.pivot = new Vector2(1, 0.5f);
        gRT.anchoredPosition = new Vector2(-18f, 0f);
        gRT.sizeDelta = new Vector2(140f, 30f);

        // runtime component
        var card = row.AddComponent<RaceStandingCard>();
        card.rankText = rank.GetComponent<TextMeshProUGUI>();
        card.codeText = code.GetComponent<TextMeshProUGUI>();
        card.gapText = gap.GetComponent<TextMeshProUGUI>();
        card.teamDot = dotImg;

        return row;
    }

    // ── primitives ───────────────────────────────────────────────────────────
    static GameObject NewGO(string n, Transform p)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static RectTransform StretchRT(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    static RoundedImage Rounded(GameObject go, Color c, float r)
    {
        var img = go.AddComponent<RoundedImage>();
        img.color = c; img.cornerRadius = r; img.cornerSegments = 12;
        img.raycastTarget = false;
        return img;
    }

    // top-left anchored
    static void TL(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // vertically centred, left anchored
    static void VLeft(GameObject go, float x, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(w, h);
    }

    static GameObject Label(Transform p, string name, string txt, float size,
        FontStyles style, Color color, TextAlignmentOptions align)
    {
        var go = NewGO(name, p);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = size; t.fontStyle = style;
        t.color = color; t.alignment = align; t.raycastTarget = false;
        t.enableWordWrapping = false;
        return go;
    }
}
