using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static CategoryPagesSetup;

/// <summary>One-time authoring of saved replay UI; no build buttons or runtime generation.</summary>
public static class ReplayPagesSetup
{
    public const string CatalogPath = "Assets/_Overdrive/UI/Prefabs/Screens/OverdriveReplayCatalog.prefab";

    public static string Install()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/_Overdrive/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Open SampleScene in Edit mode first.");
        var menu = UnityEngine.Object.FindAnyObjectByType<OverdriveMainMenu>(FindObjectsInactive.Include);
        if (menu == null || menu.sportPanel == null || menu.replayCatalogPanel != null)
            throw new InvalidOperationException("Existing sport page missing or replay already installed.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CatalogPath) != null)
            throw new InvalidOperationException("Catalogue asset already exists; do not overwrite.");
        const string backup = "Assets/_Overdrive/Scenes/SampleScene_BeforeReplay.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(backup) == null && !EditorSceneManager.SaveScene(scene, backup, true))
            throw new InvalidOperationException("Scene snapshot failed.");
        const string recovery = "Assets/_Recovery/BeforeReplay";
        if (!AssetDatabase.IsValidFolder(recovery)) AssetDatabase.CreateFolder("Assets/_Recovery", "BeforeReplay");
        string sportBackup = recovery + "/OverdriveF1Session.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(sportBackup) == null && !AssetDatabase.CopyAsset(SportPath, sportBackup))
            throw new InvalidOperationException("Sport prefab snapshot failed.");

        UpgradeSportPrefab();
        SaveCatalog();
        var catalogGO = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CatalogPath), scene);
        Undo.RegisterCreatedObjectUndo(catalogGO, "Add replay catalogue");
        catalogGO.transform.SetParent(menu.transform, false);
        Stretch((RectTransform)catalogGO.transform);
        catalogGO.SetActive(false);
        var catalog = catalogGO.GetComponent<ODReplayCatalog>();
        var sport = menu.sportPanel.GetComponent<ODSportPage>();
        if (sport.modePanels.Length != 4 || sport.replayButton == null)
            throw new InvalidOperationException("Sport prefab upgrade has not reached the scene instance.");
        Undo.RecordObjects(new UnityEngine.Object[] { menu, catalog }, "Connect replay navigation");
        menu.replayCatalogPanel = catalogGO;
        catalog.menu = menu;
        catalog.sport = sport;
        EditorUtility.SetDirty(menu);
        EditorUtility.SetDirty(catalog);
        PrefabUtility.RecordPrefabInstancePropertyModifications(catalog);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        return "Saved Replay header button, six-entry mock catalogue and fourth race-page state. Homepage, wrist tabs and input bridges preserved.";
    }

    private static void UpgradeSportPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(SportPath);
        try
        {
            var sport = root.GetComponent<ODSportPage>();
            if (sport.modePanels.Length != 3 || sport.replayButton != null)
                throw new InvalidOperationException("Unexpected sport prefab: preserve existing replay work.");
            var heading = sport.heading.rectTransform;
            heading.sizeDelta = new Vector2(590, heading.sizeDelta.y);
            var subtitle = sport.subtitle.rectTransform;
            subtitle.sizeDelta = new Vector2(590, subtitle.sizeDelta.y);
            sport.statusSurface.rectTransform.anchoredPosition = new Vector2(728, -32);
            sport.replayButton = Button(root.transform, "ReplayCatalogButton", "Replay", 1018, 32, 150, 52);

            var replay = UnityEngine.Object.Instantiate(sport.modePanels[2], root.transform, false);
            replay.name = "Replay";
            replay.transform.Find("TVLive").name = "TVReplay";
            replay.transform.Find("TVReplay/Label").GetComponent<TextMeshProUGUI>().text = "TV Replay";
            replay.transform.Find("SessionStandings/Heading").GetComponent<TextMeshProUGUI>().text = "Classement de la course";
            Text(replay.transform, "DemoMediaNotice", "Démo · Vidéo à connecter", 32, 495, 404, 20, 14, secondary: true);
            sport.modePanels = sport.modePanels.Concat(new[] { replay }).ToArray();
            sport.SetMode(ODSportPage.Mode.Weekend);
            // Header controls stay above all presentation panels in the hierarchy.
            sport.replayButton.transform.SetAsLastSibling();
            PrefabUtility.SaveAsPrefabAsset(root, SportPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void SaveCatalog()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = Root("OverdriveReplayCatalog", scene);
            var catalog = root.AddComponent<ODReplayCatalog>();
            catalog.backButton = Button(root.transform, "BackToSport", "", 32, 32, 52, 52);
            Icon(catalog.backButton.transform, "BackArrow", ODNavigationIcon.Symbol.Back, 11, 11, 30);
            Text(root.transform, "Heading", "Replays", 104, 22, 780, 66, 44, true);
            Text(root.transform, "Subtitle", "Formule 1 · Catalogue de démonstration", 104, 89, 1000, 36, 23, secondary: true);
            var badge = Fill(root.transform, "SportBadge", 1018, 32, 150, 52, new Color(.46f, .77f, 1, .20f), 26);
            Text(badge.transform, "Label", "F1", 12, 0, 126, 52, 24, true, true);

            var scroll = Rect(root.transform, "ReplayScroll", 32, 174, 1100, 646);
            var viewport = Rect(scroll, "Viewport", 0, 0, 1100, 646);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, .001f);
            var content = Rect(viewport, "Content", 0, 0, 1100, 744);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.sizeDelta = new Vector2(0, 744);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(538, 232);
            grid.spacing = new Vector2(24, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperLeft;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            catalog.scroll = scroll.gameObject.AddComponent<ScrollRect>();
            catalog.scroll.content = content;
            catalog.scroll.viewport = viewport;
            catalog.scroll.horizontal = false;
            catalog.scroll.vertical = true;
            catalog.scroll.movementType = ScrollRect.MovementType.Clamped;
            catalog.scroll.scrollSensitivity = 35;
            var track = Fill(root.transform, "ScrollTrack", 1148, 180, 16, 634, new Color(1, 1, 1, .12f), 8);
            track.raycastTarget = true;
            var handle = Fill(track.transform, "Handle", 0, 0, 16, 240, new Color(1, 1, 1, .72f), 8);
            Stretch(handle.rectTransform);
            handle.raycastTarget = true;
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            catalog.scroll.verticalScrollbar = scrollbar;
            catalog.scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            string[] names = { "GP de Singapour", "GP de Monaco", "GP d'Italie", "GP de Belgique", "GP du Japon", "GP d'Abu Dhabi" };
            string[] ids = { "singapore", "monaco", "italy", "belgium", "japan", "abu-dhabi" };
            catalog.entries = new ODReplayCatalog.Entry[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var card = Card(content, "Replay_" + ids[i], (i % 2) * 562, (i / 2) * 256, 538, 232);
                var graphic = card.GetComponent<Graphic>();
                graphic.raycastTarget = true;
                var open = card.gameObject.AddComponent<Button>();
                open.targetGraphic = graphic;
                open.navigation = new Navigation { mode = Navigation.Mode.None };
                var colors = open.colors;
                colors.normalColor = colors.selectedColor = Color.white;
                colors.highlightedColor = new Color(.78f, .87f, 1);
                colors.pressedColor = new Color(.56f, .70f, .95f);
                open.colors = colors;
                Text(card, "Season", "FORMULE 1 · SAISON 2024", 28, 20, 482, 30, 18, secondary: true);
                Text(card, "GrandPrix", names[i], 28, 59, 482, 54, 32, true);
                Text(card, "Session", "Course · Replay démo", 28, 117, 482, 30, 22, secondary: true);
                var cta = Fill(card, "OpenReplaySurface", 28, 164, 482, 48, new Color(.46f, .77f, 1, .15f), 24);
                Text(cta.transform, "Label", "Ouvrir le replay", 12, 0, 458, 48, 22, true, true);
                catalog.entries[i] = new ODReplayCatalog.Entry { id = "demo-f1-2024-" + ids[i], grandPrix = names[i], openButton = open };
            }
            Text(root.transform, "DemoNotice", "Courses de démonstration · Vidéos et données non connectées", 32, 834, 1100, 26, 18, secondary: true);
            root.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            PrefabUtility.SaveAsPrefabAsset(root, CatalogPath);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
