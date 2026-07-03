/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODShowcaseBuilder - Editor script that creates a world-space VR canvas displaying all
 ##                     OD_UI components in three labelled columns, populated with ODMockData.
 ##
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds an "OD_UI_Showcase" WorldSpace Canvas in the active scene.
/// Three labelled columns, one section title per component so every
/// element is immediately identifiable in the Scene / Game view.
///
/// Prerequisites (run in order):
///   1. Overdrive &gt; Build OD_UI Prefabs
///   2. Overdrive &gt; Build OD_UI Organisms
///   3. Overdrive &gt; Build OD_UI Level3
///   4. Overdrive &gt; Build OD_UI Showcase  ← this script
///
/// Canvas: 1800 × 1400 px, WorldSpace, scale 0.001, position (0, 1.6, 2).
/// </summary>
public static class ODShowcaseBuilder
{
    private const string AtomPath = "Assets/_Overdrive/UI/Prefabs/Atoms/";
    private const string MolPath  = "Assets/_Overdrive/UI/Prefabs/Molecules/";
    private const string OrgPath  = "Assets/_Overdrive/UI/Prefabs/Organisms/";

    // Fallback label colors used when UITheme asset is not yet loaded
    private static readonly Color FallbackPrimary = new Color(0.95f, 0.95f, 0.97f);
    private static readonly Color FallbackSecond  = new Color(0.56f, 0.56f, 0.58f);
    private static readonly Color FallbackGold    = new Color(0.788f, 0.659f, 0.298f);

    [MenuItem("Overdrive/Build OD_UI Showcase")]
    public static void BuildShowcase()
    {
        var existing = GameObject.Find("OD_UI_Showcase");
        if (existing != null) Object.DestroyImmediate(existing);

        int placed = 0;

        // ── World-Space Canvas ─────────────────────────────────────────────────────
        var canvasGO  = new GameObject("OD_UI_Showcase");
        var canvas    = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        var canvasRT  = canvasGO.GetComponent<RectTransform>();
        // Tall canvas (1400 px) so columns can overflow below the camera centre line
        canvasRT.sizeDelta  = new Vector2(1800f, 1400f);
        canvasRT.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        canvasRT.position   = new Vector3(0f, 1.6f, 2f);

        // ── Root panel ─────────────────────────────────────────────────────────────
        var panelGO  = new GameObject("Panel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT  = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

        // Near-black background
        MakeFullRect("Background", panelGO.transform, new Color(0.06f, 0.06f, 0.08f, 1f));

        // HLG positions the three columns side-by-side at the top of the panel
        var hlg = panelGO.AddComponent<HorizontalLayoutGroup>();
        hlg.padding              = new RectOffset(32, 32, 32, 32);
        hlg.spacing              = 24f;
        hlg.childAlignment       = TextAnchor.UpperLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth    = false;
        hlg.childControlHeight   = false;

        // ── Three columns ──────────────────────────────────────────────────────────
        placed += PopulateCol1(MakeColumn(panelGO.transform, "Col_Tables",  560f));
        placed += PopulateCol2(MakeColumn(panelGO.transform, "Col_Drivers", 560f));
        placed += PopulateCol3(MakeColumn(panelGO.transform, "Col_Atoms",   480f));

        // Force layout recalculation in edit mode so ContentSizeFitters apply
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRT);

        Selection.activeGameObject = canvasGO;
        Debug.Log($"[ODShowcase] Built. {placed} components placed.");
        EditorUtility.DisplayDialog("OD_UI Showcase",
            $"Showcase built — {placed} components placed.\n\n" +
            "Select 'OD_UI_Showcase' in the Hierarchy and switch to Scene view.\n" +
            "If text appears vertical, rebuild all prefabs first:\n" +
            "  Overdrive > Build OD_UI Prefabs\n" +
            "  Overdrive > Build OD_UI Organisms\n" +
            "  Overdrive > Build OD_UI Level3\n" +
            "  Overdrive > Build OD_UI Showcase", "OK");
    }

    // ── Column populators ──────────────────────────────────────────────────────────

    /// <summary>
    /// Col 1 — TABLES AND DATA:
    ///   F1 timing table (top 8) · Media controls · Session info card.
    /// </summary>
    private static int PopulateCol1(GameObject col)
    {
        int n = 0;
        var t = col.transform;

        ColTitle(t, "TABLES  &  DATA", ref n);
        ColSeparator(t);

        // ── F1 TIMING TABLE ───────────────────────────────────────────────────────
        SectionTitle(t, "F1 TIMING  ·  TOP 8", ref n);
        var f1Go = Prefab(OrgPath + "ODDataTable.prefab", t, ref n);
        if (f1Go != null)
        {
            var tbl = f1Go.GetComponent<ODDataTable>();
            if (tbl != null)
            {
                tbl.SetColumns(ODMockData.F1TimingColumns);
                tbl.SetData(ODMockData.F1TimingRows.GetRange(0, 8),
                    new List<string> { "VER","LEC","NOR","PIA","SAI","HAM","RUS","ALO" });
                tbl.SetHighlightedRow("VER");
            }
        }

        // ── MEDIA CONTROLS ────────────────────────────────────────────────────────
        SectionTitle(t, "MEDIA CONTROLS", ref n);
        var mcGo = Prefab(OrgPath + "ODMediaControls.prefab", t, ref n);
        if (mcGo != null)
        {
            var ctrl = mcGo.GetComponent<ODMediaControls>();
            if (ctrl != null) { ctrl.SetLive(ODMockData.MockIsLive); ctrl.SetProgress(0f); }
            EnsureLE(mcGo).preferredHeight = 72f;
        }

        // ── SESSION INFO CARD ─────────────────────────────────────────────────────
        SectionTitle(t, "SESSION INFO  ·  CARD", ref n);
        var sessionGo = Prefab(OrgPath + "ODCard.prefab", t, ref n);
        if (sessionGo != null)
        {
            var c = sessionGo.GetComponent<ODCard>();
            c?.titleLabel?.SetText(ODMockData.MockSessionTitle);
            if (c?.contentArea != null)
            {
                AddLabel(c.contentArea, ODMockData.MockLapInfo,       ODLabel.TextStyle.Body,    ref n);
                AddLabel(c.contentArea, ODMockData.MockChampionship,  ODLabel.TextStyle.Caption, ref n);
                AddLabel(c.contentArea, "Monaco · Circuit de Monaco", ODLabel.TextStyle.Caption, ref n);
            }
        }

        return n;
    }

    /// <summary>
    /// Col 2 — DRIVER CARDS:
    ///   Leclerc card · Verstappen card · Formula E table (top 5).
    /// </summary>
    private static int PopulateCol2(GameObject col)
    {
        int n = 0;
        var t = col.transform;

        ColTitle(t, "DRIVER  CARDS", ref n);
        ColSeparator(t);

        // ── LECLERC ───────────────────────────────────────────────────────────────
        SectionTitle(t, "DRIVER CARD  ·  LEC  #16", ref n);
        var lecGo = Prefab(OrgPath + "ODDriverCard.prefab", t, ref n);
        if (lecGo != null)
        {
            var dc = lecGo.GetComponent<ODDriverCard>();
            if (dc != null)
            {
                dc.SetDriver("16", "Charles Leclerc");
                dc.SetTeam(null, new Color(0.80f, 0.05f, 0.05f, 1f));
                dc.SetTelemetry(ODMockData.LeclercTelemetry);
            }
        }

        // ── VERSTAPPEN ────────────────────────────────────────────────────────────
        SectionTitle(t, "DRIVER CARD  ·  VER  #1", ref n);
        var verGo = Prefab(OrgPath + "ODDriverCard.prefab", t, ref n);
        if (verGo != null)
        {
            var dc = verGo.GetComponent<ODDriverCard>();
            if (dc != null)
            {
                dc.SetDriver("1", "Max Verstappen");
                dc.SetTeam(null, new Color(0.0f, 0.13f, 0.42f, 1f));
                dc.SetTelemetry(ODMockData.VerstappenTelemetry);
            }
        }

        // ── FORMULA E TABLE ───────────────────────────────────────────────────────
        SectionTitle(t, "FORMULA E  ·  ENERGY  ·  TOP 5", ref n);
        var feGo = Prefab(OrgPath + "ODDataTable.prefab", t, ref n);
        if (feGo != null)
        {
            var tbl = feGo.GetComponent<ODDataTable>();
            if (tbl != null)
            {
                tbl.SetColumns(ODMockData.FormulaEColumns);
                tbl.SetData(ODMockData.FormulaERows.GetRange(0, 5));
            }
        }

        return n;
    }

    /// <summary>
    /// Col 3 — UI COMPONENTS:
    ///   Atoms (badges/states) · Molecules (buttons, input) · Organism (card) · Typography.
    /// </summary>
    private static int PopulateCol3(GameObject col)
    {
        int n = 0;
        var t = col.transform;

        ColTitle(t, "UI  COMPONENTS", ref n);
        ColSeparator(t);

        // ── ATOMS — BADGES & STATES ───────────────────────────────────────────────
        SectionTitle(t, "ATOMS  ·  BADGES  &  STATES", ref n);
        var atomsRow = MakeHGroup(t, "BadgesRow", 12f);
        var lb1 = Prefab(AtomPath + "ODLiveBadge.prefab", atomsRow.transform, ref n);
        lb1?.GetComponent<ODLiveBadge>()?.SetLive(true);
        var lb2 = Prefab(AtomPath + "ODLiveBadge.prefab", atomsRow.transform, ref n);
        lb2?.GetComponent<ODLiveBadge>()?.SetLive(false);
        var bP1 = Prefab(MolPath + "ODBadge.prefab", atomsRow.transform, ref n);
        if (bP1 != null) { var b = bP1.GetComponent<ODBadge>(); b?.SetVariant(ODBadge.BadgeVariant.Gold);    b?.SetText("P1");  }
        var bDnf = Prefab(MolPath + "ODBadge.prefab", atomsRow.transform, ref n);
        if (bDnf != null) { var b = bDnf.GetComponent<ODBadge>(); b?.SetVariant(ODBadge.BadgeVariant.Default); b?.SetText("DNF"); }
        var bDsq = Prefab(MolPath + "ODBadge.prefab", atomsRow.transform, ref n);
        if (bDsq != null) { var b = bDsq.GetComponent<ODBadge>(); b?.SetVariant(ODBadge.BadgeVariant.Danger);  b?.SetText("DSQ"); }
        EnsureLE(atomsRow).preferredHeight = 32f;

        // ── MOLECULES — BUTTONS ───────────────────────────────────────────────────
        SectionTitle(t, "MOLECULES  ·  BUTTONS", ref n);
        var btns = MakeVGroup(t, "ButtonsGroup", 10f);
        var bPri = Prefab(MolPath + "ODButton_Primary.prefab", btns.transform, ref n);
        if (bPri != null) { bPri.GetComponent<ODButton>()?.SetLabel("Watch Live"); EnsureLE(bPri).preferredHeight = 52f; }
        var bGhost = Prefab(MolPath + "ODButton_Ghost.prefab", btns.transform, ref n);
        if (bGhost != null) { bGhost.GetComponent<ODButton>()?.SetLabel("Standings"); EnsureLE(bGhost).preferredHeight = 52f; }
        var bDanger = Prefab(MolPath + "ODButton_Danger.prefab", btns.transform, ref n);
        if (bDanger != null) { bDanger.GetComponent<ODButton>()?.SetLabel("Cancel"); EnsureLE(bDanger).preferredHeight = 52f; }

        // ── MOLECULE — INPUT FIELD ────────────────────────────────────────────────
        SectionTitle(t, "MOLECULE  ·  INPUT FIELD", ref n);
        var input = Prefab(MolPath + "ODInputField.prefab", t, ref n);
        if (input != null)
        {
            input.GetComponent<ODInputField>()?.SetPlaceholder("Search driver...");
            EnsureLE(input).preferredHeight = 52f;
        }

        // ── ORGANISM — CARD ───────────────────────────────────────────────────────
        SectionTitle(t, "ORGANISM  ·  CARD", ref n);
        var audioGo = Prefab(OrgPath + "ODCard.prefab", t, ref n);
        if (audioGo != null)
        {
            var c = audioGo.GetComponent<ODCard>();
            c?.titleLabel?.SetText("Audio Track");
            if (c?.contentArea != null)
            {
                AddLabel(c.contentArea, "Team Radio  [ON]", ODLabel.TextStyle.Body,  ref n);
                AddLabel(c.contentArea, "English",       ODLabel.TextStyle.Body,    ref n);
                AddLabel(c.contentArea, "Deutsch",       ODLabel.TextStyle.Caption, ref n);
                AddLabel(c.contentArea, "Spanish",       ODLabel.TextStyle.Caption, ref n);
            }
        }

        // ── TYPOGRAPHY SCALE ──────────────────────────────────────────────────────
        SectionTitle(t, "TYPOGRAPHY  SCALE", ref n);
        var labels = MakeVGroup(t, "LabelsGroup", 8f);
        AddLabel(labels.transform, "OverDrive",                   ODLabel.TextStyle.H1,      ref n);
        AddLabel(labels.transform, "Formula 1 · Monaco",          ODLabel.TextStyle.H2,      ref n);
        AddLabel(labels.transform, "Live race data · Round 12",   ODLabel.TextStyle.Body,    ref n);
        AddLabel(labels.transform, "All rights reserved · 2026",  ODLabel.TextStyle.Caption, ref n);

        return n;
    }

    // ── Layout helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a VerticalLayoutGroup column (auto-heights via ContentSizeFitter).
    /// Width is fixed via sizeDelta and LayoutElement; the parent HLG respects it.
    /// </summary>
    private static GameObject MakeColumn(Transform parent, string name, float w)
    {
        var col = new GameObject(name, typeof(RectTransform));
        col.transform.SetParent(parent, false);
        col.GetComponent<RectTransform>().sizeDelta = new Vector2(w, 0f);

        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 16f;
        vlg.childAlignment       = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;

        col.AddComponent<LayoutElement>().preferredWidth = w;
        col.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return col;
    }

    /// <summary>Auto-sized horizontal group.</summary>
    private static GameObject MakeHGroup(Transform parent, string name, float spacing)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = spacing;
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return go;
    }

    /// <summary>Auto-sized vertical group.</summary>
    private static GameObject MakeVGroup(Transform parent, string name, float spacing)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = spacing;
        vlg.childAlignment       = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return go;
    }

    // ── Label / title helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Large white bold title placed at the very top of a column (e.g. "TABLES  &  DATA").
    /// </summary>
    private static void ColTitle(Transform parent, string text, ref int count)
    {
        UITheme th = UITheme.Instance;
        var go  = MakeTMP(parent, "ColTitle");
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = th != null ? th.h2Size : 28f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = th != null ? th.textPrimary : FallbackPrimary;
        go.AddComponent<LayoutElement>().preferredHeight = 40f;
        count++;
    }

    /// <summary>
    /// Full-width 2 px gold separator line placed just below the column title.
    /// </summary>
    private static void ColSeparator(Transform parent)
    {
        UITheme th = UITheme.Instance;
        var go  = new GameObject("ColSep", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        Color gold = th != null ? th.accentGold : FallbackGold;
        img.color = new Color(gold.r, gold.g, gold.b, 0.50f);
        img.raycastTarget = false;
        go.AddComponent<LayoutElement>().preferredHeight = 2f;
    }

    /// <summary>
    /// Gold caption label that identifies the next component (e.g. "F1 TIMING · TOP 8").
    /// Acts as a visual section header inside the column. An 8 px invisible spacer
    /// is inserted before the label to give breathing room after the previous component.
    /// </summary>
    private static void SectionTitle(Transform parent, string text, ref int count)
    {
        // Spacer created first so it sits above the label in sibling order
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(parent, false);
        spacer.AddComponent<LayoutElement>().preferredHeight = 8f;

        UITheme th  = UITheme.Instance;
        var go  = MakeTMP(parent, "Sec_" + text.Substring(0, Mathf.Min(text.Length, 16)));
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = th != null ? th.captionSize : 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = th != null ? th.accentGold : FallbackGold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        go.AddComponent<LayoutElement>().preferredHeight = 24f;
        count++;
    }

    /// <summary>
    /// Inline TMP body/caption label — no prefab dependency.
    /// Width defaults to 400 px so TMP can word-wrap before the layout group
    /// sets the final width (avoids the "vertical text" artefact on first frame).
    /// </summary>
    private static void AddLabel(Transform parent, string text, ODLabel.TextStyle style, ref int count)
    {
        UITheme th = UITheme.Instance;
        var go  = MakeTMP(parent, "Lbl");
        var tmp = go.GetComponent<TextMeshProUGUI>();

        float fs     = th != null ? th.bodySize : 22f;
        FontStyles fw = FontStyles.Normal;
        Color  col   = th != null ? th.textPrimary : FallbackPrimary;
        float  h     = 28f;

        switch (style)
        {
            case ODLabel.TextStyle.H1:
                fs = th != null ? th.h1Size : 36f;  fw = FontStyles.Bold; h = 44f; break;
            case ODLabel.TextStyle.H2:
                fs = th != null ? th.h2Size : 28f;  fw = FontStyles.Bold; h = 36f; break;
            case ODLabel.TextStyle.Caption:
                fs = th != null ? th.captionSize : 18f;
                col = th != null ? th.textSecondary : FallbackSecond;
                h = 24f;
                break;
        }

        tmp.text      = text;
        tmp.fontSize  = fs;
        tmp.fontStyle = fw;
        tmp.color     = col;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        go.AddComponent<ODLabel>().textStyle = style;
        go.AddComponent<LayoutElement>().preferredHeight = h;
        count++;
    }

    // ── Low-level factory helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a RectTransform + TextMeshProUGUI GO with a default 400 px width so
    /// TMP can word-wrap on the first frame before the layout group applies its width.
    /// </summary>
    private static GameObject MakeTMP(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        // Give TMP a fallback width so it doesn't wrap characters vertically on
        // the first frame while the layout group is still being evaluated.
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 40f);
        go.AddComponent<TextMeshProUGUI>().enableWordWrapping = true;
        return go;
    }

    /// <summary>
    /// Creates a full-rect Image child (used for column background panels).
    /// ignoreLayout = true so it doesn't participate in the column's VLG.
    /// </summary>
    private static void MakeFullRect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    /// <summary>
    /// Loads a prefab by path and instantiates it as a direct child of <paramref name="parent"/>.
    /// Returns null and logs a warning if the asset is missing.
    /// </summary>
    private static GameObject Prefab(string path, Transform parent, ref int count)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            Debug.LogWarning($"[ODShowcase] Missing prefab: {path}  — rebuild prefabs first.");
            return null;
        }
        count++;
        return (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
    }

    /// <summary>Gets the LayoutElement on <paramref name="go"/> or adds one if absent.</summary>
    private static LayoutElement EnsureLE(GameObject go)
        => go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
}
