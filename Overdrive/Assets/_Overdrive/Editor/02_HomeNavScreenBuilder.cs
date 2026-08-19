/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## HomeNavScreenBuilder - Assembles the persistent bottom nav bar + floating home
 ## mini-window screen out of the existing ODNavBar / ODCard organisms.
 ##
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BUILD ORDER — Tier 02 (Screens). Requires: 01_ODUIBuilder must have already
/// produced ODNavBar, ODCard, ODButton_Ghost, ODButton_Danger, ODMenuOverlay and
/// ODPopup (run "Overdrive &gt; Build OD_UI Base" first, or the matching
/// OD_AllUnit entries) — Build() logs an error and aborts otherwise.
///
/// Builds "HomeNavScreen": the always-on-screen bottom navigation bar (5 tabs)
/// with a floating ODCard above it. The card's content area is left empty —
/// only its title changes to match the selected tab (Home/Championnats/
/// Recherche/Calendrier/Profil). Re-runnable: instantiates the current
/// ODNavBar.prefab / ODCard.prefab, so it always reflects their latest look.
/// WindowHandle (never modified by this builder) is attached to the shared
/// root so the nav bar and the home card always drag together as one group.
/// </summary>
public static class HomeNavScreenBuilder
{
    const string NavBarPrefabPath = "Assets/_Overdrive/UI/Prefabs/Organisms/ODNavBar.prefab";
    const string CardPrefabPath = "Assets/_Overdrive/UI/Prefabs/Organisms/ODCard.prefab";
    const string GhostButtonPrefabPath = "Assets/_Overdrive/UI/Prefabs/Molecules/ODButton_Ghost.prefab";
    const string DangerButtonPrefabPath = "Assets/_Overdrive/UI/Prefabs/Molecules/ODButton_Danger.prefab";
    const string MenuOverlayPrefabPath = "Assets/_Overdrive/UI/Prefabs/Organisms/ODMenuOverlay.prefab";
    const string PopupPrefabPath = "Assets/_Overdrive/UI/Prefabs/Organisms/ODPopup.prefab";
    const string SaveDir = "Assets/_Overdrive/UI/Prefabs/Screens";
    const string SavePath = SaveDir + "/HomeNavScreen.prefab";

    static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.35f);

    // Order follows the spec as given: home, championnat, search, calendar, profil.
    static readonly string[] TabLabels = { "Home", "Championnats", "Recherche", "Calendrier", "Profil" };

    // Nav bar geometry — the card is anchored relative to these so the gap
    // between the two stays constant no matter how tall the card grows.
    const float NavBarHeight = 84f;
    const float NavBarBottomOffset = 24f;
    const float CardToNavBarGap = 16f;

    public static void Build()
    {
        var navBarAsset = AssetDatabase.LoadAssetAtPath<GameObject>(NavBarPrefabPath);
        var cardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
        var ghostAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GhostButtonPrefabPath);
        var dangerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(DangerButtonPrefabPath);
        var menuOverlayAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MenuOverlayPrefabPath);
        var popupAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
        if (navBarAsset == null || cardAsset == null || ghostAsset == null ||
            dangerAsset == null || menuOverlayAsset == null || popupAsset == null)
        {
            Debug.LogError("[HomeNavScreenBuilder] A required prefab (ODNavBar/ODCard/ODButton_Ghost/ODButton_Danger/ODMenuOverlay/ODPopup) was not found — run 'Overdrive > Build OD_UI Base' first.");
            return;
        }

        // ── Canvas (shared root — WindowHandle is attached here) ───────────────
        GameObject canvasGO = new GameObject("OverdriveHomeNavCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(700f, 620f);
        canvasRT.localScale = Vector3.one * 0.001f;

        // ── Nav bar (bottom, floating pill, 5 tabs) ─────────────────────────────
        GameObject navGO = (GameObject)PrefabUtility.InstantiatePrefab(navBarAsset, canvasGO.transform);
        ODNavBar navBar = navGO.GetComponent<ODNavBar>();

        ExpandToFiveTabs(navBar);

        RectTransform navRT = navGO.GetComponent<RectTransform>();
        navRT.anchorMin = new Vector2(0.5f, 0f);
        navRT.anchorMax = new Vector2(0.5f, 0f);
        navRT.pivot = new Vector2(0.5f, 0f);
        navRT.anchoredPosition = new Vector2(0f, NavBarBottomOffset);
        navRT.sizeDelta = new Vector2(620f, NavBarHeight);

        StylePillNavBar(navGO);

        // ── Home mini-window (ODCard) — empty content, title follows tab ──────
        // Bottom-anchored (pivot.y = 0) at a fixed height above the nav bar:
        // ContentSizeFitter grows sizeDelta.y upward from that fixed point, so
        // the gap to the nav bar stays constant no matter how much content is
        // added later.
        GameObject cardGO = (GameObject)PrefabUtility.InstantiatePrefab(cardAsset, canvasGO.transform);
        ODCard card = cardGO.GetComponent<ODCard>();
        card.showDivider = true;
        card.showCloseButton = false;
        card.SetTitle(TabLabels[0]); // SetTitle pushes the text immediately — Start()/Sync() never run in Edit mode

        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0f);
        cardRT.anchorMax = new Vector2(0.5f, 0f);
        cardRT.pivot = new Vector2(0.5f, 0f);
        cardRT.anchoredPosition = new Vector2(0f, NavBarBottomOffset + NavBarHeight + CardToNavBarGap);
        cardRT.sizeDelta = new Vector2(620f, 0f); // height is driven by ContentSizeFitter; stays minimal since content is empty

        StyleCard(cardGO);
        FixTitleOrientation(cardGO);

        // ── Championship selector buttons (F1/WEC/MotoGP) — only visible on
        // the "Championnats" tab; HomeNavController toggles this and wires
        // the clicks at runtime. ─────────────────────────────────────────────
        GameObject championshipButtons = BuildChampionshipButtons(card.contentArea, ghostAsset);
        championshipButtons.SetActive(false);

        // ── Profile area (account + Réglages) — only visible on the "Profil"
        // tab; ProfileSectionController builds both views and switches
        // between them via the top-right ODMenuOverlay. ───────────────────────
        GameObject profileArea = BuildProfileArea(card.contentArea, menuOverlayAsset,
            out RectTransform profileContent, out RectTransform settingsContent, out ODMenuOverlay profileMenuOverlay);
        profileArea.SetActive(false);

        // ODPopup must sit directly under the canvas (not inside the scrollable
        // card content) so its full-screen overlay actually covers everything.
        GameObject popupGO = (GameObject)PrefabUtility.InstantiatePrefab(popupAsset, canvasGO.transform);
        ODPopup popup = popupGO.GetComponent<ODPopup>();

        ProfileSectionController profileController = canvasGO.AddComponent<ProfileSectionController>();
        profileController.menuOverlay = profileMenuOverlay;
        profileController.profileContent = profileContent;
        profileController.settingsContent = settingsContent;
        profileController.popup = popup;
        profileController.ghostButtonPrefab = ghostAsset;
        profileController.dangerButtonPrefab = dangerAsset;
        profileController.menuOverlayPrefab = menuOverlayAsset;

        // ── Controller: syncs card title with selected tab, wires championship buttons ──
        HomeNavController controller = canvasGO.AddComponent<HomeNavController>();
        controller.navBar = navBar;
        controller.homeCard = card;
        controller.championshipButtons = championshipButtons;
        controller.profileArea = profileArea;

        // ── WindowHandle — attached to the shared root so the nav bar and the
        // home card always move together as a single group. This component is
        // never modified here, only attached. ─────────────────────────────────
        canvasGO.AddComponent<WindowHandle>();

        // ── Save ─────────────────────────────────────────────────────────────
        System.IO.Directory.CreateDirectory(SaveDir);
        PrefabUtility.SaveAsPrefabAsset(canvasGO, SavePath);
        Debug.Log($"[HomeNavScreenBuilder] Home nav screen saved to {SavePath}");

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
        hlg.spacing = 12f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
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
    /// Builds the "Profil" tab's whole area: a header row with the
    /// ODMenuOverlay pushed to the top-right, and 2 empty content containers
    /// (ProfileContent / SettingsContent) that ProfileSectionController fills
    /// and switches between at runtime.
    /// </summary>
    static GameObject BuildProfileArea(RectTransform contentArea, GameObject menuOverlayAsset,
        out RectTransform profileContent, out RectTransform settingsContent, out ODMenuOverlay menuOverlay)
    {
        GameObject area = new GameObject("ProfileArea", typeof(RectTransform));
        area.transform.SetParent(contentArea, false);
        VerticalLayoutGroup areaVlg = area.AddComponent<VerticalLayoutGroup>();
        areaVlg.spacing = 16f;
        areaVlg.childAlignment = TextAnchor.UpperLeft;
        areaVlg.childControlWidth = true;
        areaVlg.childControlHeight = false;
        areaVlg.childForceExpandWidth = true;
        areaVlg.childForceExpandHeight = false;
        area.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        area.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        // Header row — menu overlay pushed to the right via MiddleRight alignment
        GameObject header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(area.transform, false);
        HorizontalLayoutGroup headerHlg = header.AddComponent<HorizontalLayoutGroup>();
        headerHlg.childAlignment = TextAnchor.MiddleRight;
        headerHlg.childControlWidth = false;
        headerHlg.childControlHeight = true;
        header.AddComponent<LayoutElement>().preferredHeight = 48f;
        header.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 48f);

        GameObject menuOverlayGO = (GameObject)PrefabUtility.InstantiatePrefab(menuOverlayAsset, header.transform);
        menuOverlayGO.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 44f);
        menuOverlay = menuOverlayGO.GetComponent<ODMenuOverlay>();

        // ProfileContent — visible by default (ProfileSectionController.ShowProfile() at Start())
        GameObject profileGO = new GameObject("ProfileContent", typeof(RectTransform));
        profileGO.transform.SetParent(area.transform, false);
        VerticalLayoutGroup profileVlg = profileGO.AddComponent<VerticalLayoutGroup>();
        profileVlg.spacing = 12f;
        profileVlg.childAlignment = TextAnchor.UpperLeft;
        profileVlg.childControlWidth = true;
        profileVlg.childControlHeight = false;
        profileVlg.childForceExpandWidth = true;
        profileVlg.childForceExpandHeight = false;
        profileGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        profileContent = profileGO.GetComponent<RectTransform>();
        profileContent.sizeDelta = Vector2.zero;

        // SettingsContent — same structure, hidden by default
        GameObject settingsGO = new GameObject("SettingsContent", typeof(RectTransform));
        settingsGO.transform.SetParent(area.transform, false);
        VerticalLayoutGroup settingsVlg = settingsGO.AddComponent<VerticalLayoutGroup>();
        settingsVlg.spacing = 12f;
        settingsVlg.childAlignment = TextAnchor.UpperLeft;
        settingsVlg.childControlWidth = true;
        settingsVlg.childControlHeight = false;
        settingsVlg.childForceExpandWidth = true;
        settingsVlg.childForceExpandHeight = false;
        settingsGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        settingsContent = settingsGO.GetComponent<RectTransform>();
        settingsContent.sizeDelta = Vector2.zero;
        settingsGO.SetActive(false);

        return area;
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
            Debug.LogError("[HomeNavScreenBuilder] ODNavBar has no itemsContainer/children to clone from.");
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
        shadow.effectColor = ShadowColor;
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
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }
}
