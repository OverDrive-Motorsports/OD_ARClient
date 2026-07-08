/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## HomeNavBuilder - Assembles the persistent bottom nav bar + floating home
 ## mini-window screen out of the existing ODNavBar / ODCard organisms.
 ##
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds "HomeNavScreen": the always-on-screen bottom navigation bar (5 tabs)
/// with a floating ODCard above it. The card's content area is left empty —
/// only its title changes to match the selected tab (Home/Championnats/
/// Recherche/Calendrier/Profil). Re-runnable: instantiates the current
/// ODNavBar.prefab / ODCard.prefab, so it always reflects their latest look.
/// WindowHandle (never modified by this builder) is attached to the shared
/// root so the nav bar and the home card always drag together as one group.
/// </summary>
public static class HomeNavBuilder
{
    const string NavBarPrefabPath        = "Assets/_Overdrive/UI/Prefabs/Organisms/ODNavBar.prefab";
    const string CardPrefabPath          = "Assets/_Overdrive/UI/Prefabs/Organisms/ODCard.prefab";
    const string GhostButtonPrefabPath   = "Assets/_Overdrive/UI/Prefabs/Molecules/ODButton_Ghost.prefab";
    const string SaveDir                 = "Assets/_Overdrive/UI/Prefabs/Screens";
    const string SavePath                = SaveDir + "/HomeNavScreen.prefab";

    static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.35f);

    // Order follows the spec as given: home, championnat, search, calendar, profil.
    static readonly string[] TabLabels = { "Home", "Championnats", "Recherche", "Calendrier", "Profil" };

    // Nav bar geometry — the card is anchored relative to these so the gap
    // between the two stays constant no matter how tall the card grows.
    const float NavBarHeight       = 84f;
    const float NavBarBottomOffset = 24f;
    const float CardToNavBarGap    = 16f;

    [MenuItem("Overdrive/Build Home Nav Screen")]
    public static void Build()
    {
        var navBarAsset = AssetDatabase.LoadAssetAtPath<GameObject>(NavBarPrefabPath);
        var cardAsset   = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
        var ghostAsset  = AssetDatabase.LoadAssetAtPath<GameObject>(GhostButtonPrefabPath);
        if (navBarAsset == null || cardAsset == null || ghostAsset == null)
        {
            Debug.LogError("[HomeNavBuilder] ODNavBar/ODCard/ODButton_Ghost prefab not found — run 'Overdrive > Build OD_UI Organisms' first.");
            return;
        }

        // ── Canvas (shared root — WindowHandle is attached here) ───────────────
        GameObject canvasGO = new GameObject("OverdriveHomeNavCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta  = new Vector2(700f, 620f);
        canvasRT.localScale = Vector3.one * 0.001f;

        // ── Nav bar (bottom, floating pill, 5 tabs) ─────────────────────────────
        GameObject navGO = (GameObject)PrefabUtility.InstantiatePrefab(navBarAsset, canvasGO.transform);
        ODNavBar navBar = navGO.GetComponent<ODNavBar>();

        ExpandToFiveTabs(navBar);

        RectTransform navRT = navGO.GetComponent<RectTransform>();
        navRT.anchorMin        = new Vector2(0.5f, 0f);
        navRT.anchorMax        = new Vector2(0.5f, 0f);
        navRT.pivot            = new Vector2(0.5f, 0f);
        navRT.anchoredPosition = new Vector2(0f, NavBarBottomOffset);
        navRT.sizeDelta        = new Vector2(620f, NavBarHeight);

        StylePillNavBar(navGO);

        // ── Home mini-window (ODCard) — empty content, title follows tab ──────
        // Bottom-anchored (pivot.y = 0) at a fixed height above the nav bar:
        // ContentSizeFitter grows sizeDelta.y upward from that fixed point, so
        // the gap to the nav bar stays constant no matter how much content is
        // added later.
        GameObject cardGO = (GameObject)PrefabUtility.InstantiatePrefab(cardAsset, canvasGO.transform);
        ODCard card = cardGO.GetComponent<ODCard>();
        card.showDivider     = true;
        card.showCloseButton = false;
        card.SetTitle(TabLabels[0]); // SetTitle pushes the text immediately — Start()/Sync() never run in Edit mode

        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.anchorMin        = new Vector2(0.5f, 0f);
        cardRT.anchorMax        = new Vector2(0.5f, 0f);
        cardRT.pivot            = new Vector2(0.5f, 0f);
        cardRT.anchoredPosition = new Vector2(0f, NavBarBottomOffset + NavBarHeight + CardToNavBarGap);
        cardRT.sizeDelta        = new Vector2(620f, 0f); // height is driven by ContentSizeFitter; stays minimal since content is empty

        StyleCard(cardGO);
        FixTitleOrientation(cardGO);

        // ── Championship selector buttons (F1/WEC/MotoGP) — only visible on
        // the "Championnats" tab; HomeNavController toggles this and wires
        // the clicks at runtime. ─────────────────────────────────────────────
        GameObject championshipButtons = BuildChampionshipButtons(card.contentArea, ghostAsset);
        championshipButtons.SetActive(false);

        // ── Controller: syncs card title with selected tab, wires championship buttons ──
        HomeNavController controller = canvasGO.AddComponent<HomeNavController>();
        controller.navBar              = navBar;
        controller.homeCard            = card;
        controller.championshipButtons = championshipButtons;

        // ── WindowHandle — attached to the shared root so the nav bar and the
        // home card always move together as a single group. This component is
        // never modified here, only attached. ─────────────────────────────────
        canvasGO.AddComponent<WindowHandle>();

        // ── Save ─────────────────────────────────────────────────────────────
        System.IO.Directory.CreateDirectory(SaveDir);
        PrefabUtility.SaveAsPrefabAsset(canvasGO, SavePath);
        Debug.Log($"[HomeNavBuilder] Home nav screen saved to {SavePath}");

        Selection.activeGameObject = canvasGO;
    }

    /// <summary>
    /// Builds the 3 championship selector buttons (F1/WEC/MotoGP, from
    /// ODChampionshipMockData) inside the home card's content area. Labels
    /// are baked here (SetLabel is a direct text write, no Start() needed);
    /// the click wiring itself happens at runtime in HomeNavController.
    /// </summary>
    static GameObject BuildChampionshipButtons(RectTransform contentArea, GameObject ghostButtonAsset)
    {
        GameObject row = new GameObject("ChampionshipButtons", typeof(RectTransform));
        row.transform.SetParent(contentArea, false);
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 12f;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = false;
        row.AddComponent<LayoutElement>().preferredHeight = 56f;
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);

        foreach (var champ in ODChampionshipMockData.Mocks)
        {
            string shortLabel = ODChampionshipMockData.ShortLabel(champ.id);
            GameObject btnGO = (GameObject)PrefabUtility.InstantiatePrefab(ghostButtonAsset, row.transform);
            btnGO.name = "Btn_" + shortLabel;
            btnGO.GetComponent<ODButton>().SetLabel(shortLabel);
            btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 56f);
        }

        return row;
    }

    /// <summary>
    /// ODNavBar.prefab ships with 3 NavItem children (Home/Race/Profile).
    /// Clones the first one until there are 5, then calls ODNavItem.Setup()
    /// directly on each — ODNavBar.RebuildItems() only runs at runtime Start(),
    /// which never fires in Edit mode, so relying on it alone would save a
    /// prefab whose visible labels don't match the items list.
    /// </summary>
    static void ExpandToFiveTabs(ODNavBar navBar)
    {
        Transform container = navBar.itemsContainer;
        if (container == null || container.childCount == 0)
        {
            Debug.LogError("[HomeNavBuilder] ODNavBar has no itemsContainer/children to clone from.");
            return;
        }

        GameObject template = container.GetChild(0).gameObject;

        // Remove any default items beyond the template (indices 1, 2, ...)
        for (int i = container.childCount - 1; i >= 1; i--)
            Object.DestroyImmediate(container.GetChild(i).gameObject);

        navBar.items = new List<ODNavBar.NavItemData>();
        for (int i = 0; i < TabLabels.Length; i++)
        {
            GameObject itemGO = (i == 0) ? template : Object.Instantiate(template, container);
            itemGO.name = "NavItem_" + i;
            itemGO.GetComponent<ODNavItem>()?.Setup(TabLabels[i], null);
            navBar.items.Add(new ODNavBar.NavItemData { label = TabLabels[i] });
        }
    }

    /// <summary>
    /// Rounds the nav bar into a full stadium/pill shape (radius = half height,
    /// matching the reference mockup) and adds a soft drop shadow so it reads
    /// as a floating element rather than a flat panel.
    /// </summary>
    static void StylePillNavBar(GameObject navGO)
    {
        RectTransform navRT = navGO.GetComponent<RectTransform>();
        Transform bg = navGO.transform.Find("Background");
        if (bg == null) return;

        RoundedImage img = bg.GetComponent<RoundedImage>();
        if (img != null) img.cornerRadius = navRT.sizeDelta.y * 0.5f;

        AddShadow(bg.gameObject);
    }

    /// <summary>
    /// Slightly increases the card's corner radius to better match the
    /// reference mockup and adds the same soft drop shadow as the nav bar.
    /// </summary>
    static void StyleCard(GameObject cardGO)
    {
        Transform bg = cardGO.transform.Find("Background");
        if (bg == null) return;

        RoundedImage img = bg.GetComponent<RoundedImage>();
        if (img != null) img.cornerRadius = 32f;

        AddShadow(bg.gameObject);
    }

    static void AddShadow(GameObject go)
    {
        Shadow shadow = go.GetComponent<Shadow>();
        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectColor    = ShadowColor;
        shadow.effectDistance = new Vector2(0f, -6f);
        shadow.useGraphicAlpha = true;
    }

    /// <summary>
    /// ODCard's Header uses a HorizontalLayoutGroup with childControlWidth = false,
    /// so the Title's own RectTransform width (0, from ODUIBuilder) wins and the
    /// TMP text word-wraps one character per line. Turning childControlWidth on
    /// lets the Title's LayoutElement.flexibleWidth actually take effect, and
    /// disabling word wrap guarantees a single horizontal line either way.
    /// </summary>
    static void FixTitleOrientation(GameObject cardGO)
    {
        Transform header = cardGO.transform.Find("Header");
        if (header == null) return;

        HorizontalLayoutGroup hlg = header.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) hlg.childControlWidth = true;

        Transform title = header.Find("Title");
        if (title == null) return;

        TextMeshProUGUI tmp = title.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Ellipsis;
            tmp.alignment          = TextAlignmentOptions.MidlineLeft;
        }
    }
}
