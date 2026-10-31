using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Isolated editor smoke tests and screenshots. Never changes the open scene or preferences.</summary>
public static class CategoryPagesVerification
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static string RunAndCapture()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var source = UnityEngine.Object.FindFirstObjectByType<OverdriveMainMenu>(FindObjectsInactive.Include);
        Check(source != null && source.categoriesPanel != null && source.sportPanel != null, "Installed pages missing.");
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var canvasGO = new GameObject("IsolatedCategoryPreview", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasGO, preview);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1200, 880);
            canvas.transform.localScale = Vector3.one * .001f;
            var clone = UnityEngine.Object.Instantiate(source.gameObject, canvas.transform, false);
            var menu = clone.GetComponent<OverdriveMainMenu>();
            var directory = clone.GetComponentInChildren<ODCategoriesPage>(true);
            var sport = clone.GetComponentInChildren<ODSportPage>(true);
            sport.circuitPreview = null;
            if (sport.television != null && sport.television.videoPanel != null)
            {
                var media = UnityEngine.Object.Instantiate(sport.television.videoPanel);
                SceneManager.MoveGameObjectToScene(media, preview);
                media.GetComponent<ODCourseVideoDock>().courseWindow = (RectTransform)menu.transform;
                media.GetComponent<ODCourseVideoDock>().Align();
                CourseMediaSetup.ConnectPanel(sport.television, media);
            }
            var catalog = clone.GetComponentInChildren<ODReplayCatalog>(true);
            Check(catalog != null && menu.replayCatalogPanel == catalog.gameObject, "Replay catalogue wiring missing.");
            directory.persistFavorites = false;
            directory.menu = menu;
            directory.sport = sport;
            sport.menu = menu;
            catalog.menu = menu;
            catalog.sport = sport;
            catalog.Initialize();
            catalog.Initialize();
            var navGO = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Overdrive/UI/Prefabs/Screens/FloatingNavigation.prefab"), preview);
            navGO.transform.position = new Vector3(-5, 0, 0);
            var nav = navGO.GetComponent<FloatingNavigation>();
            nav.menu = menu;
            nav.contentWindow = canvas;
            typeof(FloatingNavigation).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(nav, null);
            directory.Initialize();
            directory.Initialize(); // Repeated opening must not duplicate handlers.
            sport.Initialize();
            Check(nav.GetComponentsInChildren<Button>(true).Length == 4, "Wrist tab count changed.");
            Check(nav.categoriesButton.GetComponentInChildren<TextMeshProUGUI>(true).text == "Championnats", "Wrong wrist label.");
            Check(nav.categoriesButton.GetComponentInChildren<ODNavigationIcon>(true).symbol == ODNavigationIcon.Symbol.Trophy, "Wrong wrist icon.");
            nav.homepageButton.onClick.Invoke();
            Check(menu.homepagePanel.activeSelf && !directory.gameObject.activeSelf && !sport.gameObject.activeSelf, "Startup Homepage route failed.");
            nav.categoriesButton.onClick.Invoke();
            Check(directory.gameObject.activeSelf && !menu.homepagePanel.activeSelf, "Categories route failed.");
            Check(directory.cards.Length == 6 && directory.cards.Count(c => c.available) == 1, "Expected F1 plus five placeholders.");
            Check(directory.cards.Skip(1).All(c => !c.openButton.interactable), "Unavailable cards must not open a sport.");
            directory.search.text = "F1";
            Check(directory.cards.Count(c => c.root.activeSelf) == 1 && directory.cards[0].root.activeSelf, "F1 search failed.");
            directory.search.text = "missing-category";
            Check(directory.noResults.activeSelf && directory.cards.All(c => !c.root.activeSelf), "Empty search state failed.");
            directory.search.text = "";
            Check(directory.cards.All(c => c.root.activeSelf) && !directory.noResults.activeSelf, "Clearing search failed.");
            directory.cards[0].favoriteButton.onClick.Invoke();
            Check(directory.IsFavorite(0), "Favorite toggle failed.");
            directory.cards[0].favoriteButton.onClick.Invoke();
            Check(!directory.IsFavorite(0) && directory.gameObject.activeSelf, "Favorite click opened the sport or duplicated handlers.");
            LayoutRebuilder.ForceRebuildLayoutImmediate(directory.scroll.content);
            Check(directory.scroll.content.rect.height > directory.scroll.viewport.rect.height, "Directory has no scroll range.");
            directory.scroll.verticalNormalizedPosition = 0;
            Check(directory.scroll.content.anchoredPosition.y > 0, "Scroll to final row failed.");
            directory.scroll.verticalNormalizedPosition = 1;
            directory.profileButton.onClick.Invoke();
            Check(!directory.gameObject.activeSelf && !sport.gameObject.activeSelf && clone.transform.Find("Body").gameObject.activeSelf, "Profile shortcut failed.");
            nav.categoriesButton.onClick.Invoke();
            directory.cards[0].openButton.onClick.Invoke();
            Check(sport.gameObject.activeSelf && !directory.gameObject.activeSelf && sport.currentMode == ODSportPage.Mode.Weekend, "F1 must open Weekend first.");
            sport.demoButton.onClick.Invoke();
            Check(sport.currentMode == ODSportPage.Mode.LiveSession && sport.status.text == "Live - Course", "Live demo state failed.");
            sport.demoButton.onClick.Invoke();
            Check(sport.currentMode == ODSportPage.Mode.Replay && sport.status.text == "Replay", "Fourth demo state failed.");
            sport.demoButton.onClick.Invoke();
            Check(sport.currentMode == ODSportPage.Mode.OffWeekend, "Off-weekend demo state failed.");
            sport.demoButton.onClick.Invoke();
            Check(sport.currentMode == ODSportPage.Mode.Weekend && sport.status.text == "Week-end en cours", "Weekend demo state failed.");
            Check(sport.modePanels.Count(p => p.activeSelf) == 1, "Multiple states overlap.");
            Check(sport.modePanels.Length == 4 && catalog.entries.Length == 6, "Expected four states and six mock replays.");
            for (int i = 0; i < 4; i++)
            {
                sport.SetMode((ODSportPage.Mode)i);
                sport.replayButton.onClick.Invoke();
                Check(catalog.gameObject.activeSelf && !sport.gameObject.activeSelf && !directory.gameObject.activeSelf, "Catalogue routing failed.");
                catalog.backButton.onClick.Invoke();
                Check(sport.gameObject.activeSelf && !catalog.gameObject.activeSelf && sport.currentMode == (ODSportPage.Mode)i, "Catalogue back lost the previous state.");
            }
            for (int i = 0; i < catalog.entries.Length; i++)
            {
                sport.replayButton.onClick.Invoke();
                catalog.entries[i].openButton.onClick.Invoke();
                Check(sport.gameObject.activeSelf && !catalog.gameObject.activeSelf && sport.currentMode == ODSportPage.Mode.Replay,
                    "Replay selection did not open state four.");
                Check(sport.replayId == catalog.entries[i].id && sport.heading.text == catalog.entries[i].grandPrix && sport.status.text == "Replay",
                    "Selected replay metadata or badge mismatch.");
                Check(sport.subtitle.text.Contains(catalog.entries[i].season) && sport.modePanels.Count(p => p.activeSelf) == 1,
                    "Replay subtitle or exclusive panel failed.");
            }
            sport.replayButton.onClick.Invoke();
            catalog.OpenReplay(-1);
            catalog.OpenReplay(catalog.entries.Length);
            Check(catalog.gameObject.activeSelf, "Invalid replay selection changed the page.");
            nav.profileButton.onClick.Invoke();
            Check(!catalog.gameObject.activeSelf && !sport.gameObject.activeSelf, "Profile left the catalogue visible.");
            nav.categoriesButton.onClick.Invoke();
            directory.cards[0].openButton.onClick.Invoke();
            Check(sport.currentMode == ODSportPage.Mode.Weekend && sport.heading.text == sport.mockGrandPrix, "Returning to F1 retained replay presentation.");
            sport.backButton.onClick.Invoke();
            Check(directory.gameObject.activeSelf && !sport.gameObject.activeSelf, "Back button failed.");

            var cameraGO = new GameObject("CategoryVerificationCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraGO, preview);
            var camera = cameraGO.GetComponent<Camera>();
            camera.enabled = false;
            camera.scene = preview;
            camera.orthographic = true;
            camera.orthographicSize = .4664f;
            camera.transform.position = menu.transform.TransformPoint(((RectTransform)menu.transform).rect.center) - Vector3.forward * 2;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 10;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.20f, .22f, .25f);
            canvas.worldCamera = camera;
            var overflow = new List<string>();
            Capture(camera, canvasGO, "/private/tmp/overdrive-categories.png", overflow);
            directory.cards[0].openButton.onClick.Invoke();
            for (int i = 0; i < 4; i++)
            {
                sport.SetMode((ODSportPage.Mode)i);
                Check(sport.modePanels.Count(p => p.activeSelf) == 1, "Mode exclusivity failed.");
                Capture(camera, canvasGO, "/private/tmp/overdrive-f1-mode-" + i + ".png", overflow);
            }
            sport.replayButton.onClick.Invoke();
            LayoutRebuilder.ForceRebuildLayoutImmediate(catalog.scroll.content);
            Check(catalog.scroll.content.rect.height > catalog.scroll.viewport.rect.height, "Replay catalogue does not scroll.");
            catalog.scroll.verticalNormalizedPosition = 1;
            Capture(camera, canvasGO, "/private/tmp/overdrive-replay-catalog.png", overflow);
            catalog.scroll.verticalNormalizedPosition = 0;
            Check(catalog.scroll.content.anchoredPosition.y > 0, "Cannot reach the final replays.");
            Capture(camera, canvasGO, "/private/tmp/overdrive-replay-catalog-bottom.png", overflow);
            catalog.entries[0].openButton.onClick.Invoke();
            Capture(camera, canvasGO, "/private/tmp/overdrive-replay-selected.png", overflow);
            sport.replayButton.onClick.Invoke();
            nav.homepageButton.onClick.Invoke();
            Check(menu.homepagePanel.activeSelf && !directory.gameObject.activeSelf && !sport.gameObject.activeSelf && !catalog.gameObject.activeSelf, "Returning to Homepage failed.");
            Check(menu.homepagePanel.GetComponent<ODHomePage>() != null, "Homepage is not the runtime Home page.");
            Check(overflow.Count == 0, "Text overflow: " + string.Join(", ", overflow));
            return "PASS: wrist/category/profile/home navigation, favorites/search, four exclusive F1 modes, replay button in every mode, catalogue/back preserving state, all six replay selections and metadata, catalogue scrolling, F1 default restored, runtime Home page, text bounds. Eight isolated screenshots captured.";
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static void Capture(Camera camera, GameObject root, string path, List<string> overflow)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>())
        {
            label.ForceMeshUpdate(true);
            if (label.isTextOverflowing || label.preferredWidth > label.rectTransform.rect.width + 1)
                overflow.Add(label.name + ": " + label.text);
        }
        foreach (var graphic in root.GetComponentsInChildren<Graphic>()) graphic.Rebuild(CanvasUpdate.PreRender);
        var render = new RenderTexture(1200, 880, 24);
        var previous = RenderTexture.active;
        var pixels = new Texture2D(1200, 880, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = render;
            camera.Render();
            RenderTexture.active = render;
            pixels.ReadPixels(new Rect(0, 0, 1200, 880), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            render.Release();
            UnityEngine.Object.DestroyImmediate(render);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
