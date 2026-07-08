/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ChampionshipPageBuilder - Assembles the championship page's EMPTY skeleton:
 ## an ODCard with 5 named section containers and a persistent Replay footer.
 ## ChampionshipPageController fills the sections at runtime, entirely from
 ## whichever ChampionshipData is passed to Open() — this builder has no
 ## knowledge of any specific championship's content.
 ##
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds "ChampionshipPage". WindowHandle (never modified by this builder)
/// is attached to the root so the page is draggable, like every other
/// floating window.
/// </summary>
public static class ChampionshipPageBuilder
{
    const string CardPrefabPath        = "Assets/_Overdrive/UI/Prefabs/Organisms/ODCard.prefab";
    const string GhostButtonPrefabPath = "Assets/_Overdrive/UI/Prefabs/Molecules/ODButton_Ghost.prefab";
    const string DataTablePrefabPath   = "Assets/_Overdrive/UI/Prefabs/Organisms/ODDataTable.prefab";
    const string SaveDir  = "Assets/_Overdrive/UI/Prefabs/Screens";
    const string SavePath = SaveDir + "/ChampionshipPage.prefab";

    [MenuItem("Overdrive/Build Championship Page")]
    public static void Build()
    {
        var cardAsset   = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
        var ghostAsset  = AssetDatabase.LoadAssetAtPath<GameObject>(GhostButtonPrefabPath);
        var tableAsset  = AssetDatabase.LoadAssetAtPath<GameObject>(DataTablePrefabPath);
        if (cardAsset == null || ghostAsset == null || tableAsset == null)
        {
            Debug.LogError("[ChampionshipPageBuilder] ODCard/ODButton_Ghost/ODDataTable prefabs not found — run 'Overdrive > Build OD_UI Prefabs' and 'Build OD_UI Organisms' first.");
            return;
        }

        // ── Canvas (WindowHandle attaches here) ─────────────────────────────────
        GameObject canvasGO = new GameObject("ChampionshipPageCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta  = new Vector2(760f, 900f);
        canvasRT.localScale = Vector3.one * 0.001f;

        // ── Card ─────────────────────────────────────────────────────────────
        GameObject cardGO = (GameObject)PrefabUtility.InstantiatePrefab(cardAsset, canvasGO.transform);
        ODCard card = cardGO.GetComponent<ODCard>();
        card.showDivider     = true;
        card.showCloseButton = false;
        card.SetTitle("Championnat");

        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        cardRT.pivot            = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.sizeDelta        = new Vector2(680f, 0f); // height driven by ContentSizeFitter

        FixTitleOrientation(cardGO);

        // ── 5 empty section containers, direct siblings in the content area ────
        // ChampionshipPageController shows/hides/populates each one at Open()
        // time based purely on which fields are present on the data passed in.
        RectTransform liveSection           = NewSection(card.contentArea, "LiveSection");
        RectTransform circuitWeatherSection = NewSection(card.contentArea, "CircuitWeatherSection");
        RectTransform nextEventSection      = NewSection(card.contentArea, "NextEventSection");
        RectTransform scheduleSection       = NewSection(card.contentArea, "ScheduleSection");
        RectTransform standingsSection      = NewSection(card.contentArea, "StandingsSection");

        // ── Persistent footer — Replay is available no matter the status ──────
        GameObject footer = NewSectionGO(card.contentArea, "Footer");
        HorizontalLayoutGroup footerHlg = footer.AddComponent<HorizontalLayoutGroup>();
        footerHlg.childControlWidth  = false;
        footerHlg.childControlHeight = true;
        footer.AddComponent<LayoutElement>().preferredHeight = 56f;
        footer.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);

        GameObject replayGO = (GameObject)PrefabUtility.InstantiatePrefab(ghostAsset, footer.transform);
        replayGO.GetComponent<ODButton>().SetLabel("Replay");
        replayGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 56f);

        // ── Controller ───────────────────────────────────────────────────────
        ChampionshipPageController controller = canvasGO.AddComponent<ChampionshipPageController>();
        controller.card                  = card;
        controller.liveSection           = liveSection;
        controller.circuitWeatherSection = circuitWeatherSection;
        controller.nextEventSection      = nextEventSection;
        controller.scheduleSection       = scheduleSection;
        controller.standingsSection      = standingsSection;
        controller.ghostButtonPrefab     = ghostAsset;
        controller.dataTablePrefab       = tableAsset;

        // ── WindowHandle — attached only, never modified ─────────────────────
        canvasGO.AddComponent<WindowHandle>();

        // ── Save ─────────────────────────────────────────────────────────────
        System.IO.Directory.CreateDirectory(SaveDir);
        PrefabUtility.SaveAsPrefabAsset(canvasGO, SavePath);
        Debug.Log($"[ChampionshipPageBuilder] Championship page saved to {SavePath}");

        Selection.activeGameObject = canvasGO;
    }

    static GameObject NewSectionGO(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform NewSection(Transform parent, string name)
    {
        GameObject go = NewSectionGO(parent, name);
        VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 12f;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = Vector2.zero;
        return rt;
    }

    /// <summary>Same ODCard title-wrapping fix as HomeNavBuilder — see its comment for why.</summary>
    static void FixTitleOrientation(GameObject cardGO)
    {
        Transform header = cardGO.transform.Find("Header");
        if (header == null) return;

        HorizontalLayoutGroup hlg = header.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) hlg.childControlWidth = true;

        Transform title = header.Find("Title");
        if (title == null) return;

        TMPro.TextMeshProUGUI tmp = title.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TMPro.TextOverflowModes.Ellipsis;
            tmp.alignment          = TMPro.TextAlignmentOptions.MidlineLeft;
        }
    }
}
