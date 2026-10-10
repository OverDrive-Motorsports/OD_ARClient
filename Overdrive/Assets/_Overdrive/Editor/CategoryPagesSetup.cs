using System;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One-time authoring helper. Saves ordinary editable prefabs, with no menu/build button.</summary>
public static class CategoryPagesSetup
{
    public const string DirectoryPath = "Assets/_Overdrive/UI/Prefabs/Screens/OverdriveCategories.prefab";
    public const string SportPath = "Assets/_Overdrive/UI/Prefabs/Screens/OverdriveF1Session.prefab";
    private static readonly Color Secondary = new Color(1, 1, 1, .65f);
    private static readonly Color LineColor = new Color(1, 1, 1, .12f);
    private static readonly Color ButtonColor = new Color(.42f, .46f, .54f, .60f);
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

    public static string Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Overdrive/Scenes/SampleScene.unity") throw new InvalidOperationException("Open SampleScene first.");
        var menu = UnityEngine.Object.FindFirstObjectByType<OverdriveMainMenu>(FindObjectsInactive.Include);
        if (menu == null || menu.homepagePanel == null) throw new InvalidOperationException("Existing homepage missing.");
        if (menu.categoriesPanel != null || menu.sportPanel != null) throw new InvalidOperationException("Pages already installed; preserve them.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(DirectoryPath) != null || AssetDatabase.LoadAssetAtPath<GameObject>(SportPath) != null)
            throw new InvalidOperationException("Saved pages already exist; do not overwrite them.");
        const string backup = "Assets/_Overdrive/Scenes/SampleScene_BeforeCategories.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(backup) == null && !EditorSceneManager.SaveScene(scene, backup, true))
            throw new InvalidOperationException("Could not save scene snapshot.");

        SaveDirectory();
        SaveSport();
        var directoryGO = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DirectoryPath), scene);
        var sportGO = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SportPath), scene);
        Undo.RegisterCreatedObjectUndo(directoryGO, "Add Categories page");
        Undo.RegisterCreatedObjectUndo(sportGO, "Add F1 demo page");
        directoryGO.transform.SetParent(menu.transform, false);
        sportGO.transform.SetParent(menu.transform, false);
        Stretch((RectTransform)directoryGO.transform);
        Stretch((RectTransform)sportGO.transform);
        directoryGO.SetActive(false);
        sportGO.SetActive(false);
        var directory = directoryGO.GetComponent<ODCategoriesPage>();
        var sport = sportGO.GetComponent<ODSportPage>();
        Undo.RecordObjects(new UnityEngine.Object[] { menu, directory, sport }, "Wire category navigation");
        menu.categoriesPanel = directoryGO;
        menu.sportPanel = sportGO;
        directory.menu = menu;
        directory.sport = sport;
        sport.menu = menu;
        foreach (var component in new Component[] { directory, sport })
        {
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        EditorUtility.SetDirty(menu);

        // The content canvas previously lacked the ISDK bridge used by the wrist menu.
        // This lets the existing controller/hand rays interact with the saved pages.
        var canvas = menu.GetComponentInParent<Canvas>();
        if (canvas.GetComponentsInChildren<PointableCanvas>(true).All(p => p.Canvas != canvas))
        {
            var pointable = Undo.AddComponent<PointableCanvas>(canvas.gameObject);
            pointable.InjectCanvas(canvas);
            var plane = Undo.AddComponent<PlaneSurface>(canvas.gameObject);
            var bounds = Undo.AddComponent<BoundsClipper>(canvas.gameObject);
            var size = ((RectTransform)canvas.transform).rect.size;
            bounds.Size = new Vector3(size.x, size.y, 1);
            var surface = Undo.AddComponent<ClippedPlaneSurface>(canvas.gameObject);
            surface.InjectAllClippedPlaneSurface(plane, new[] { bounds });
            var ray = Undo.AddComponent<RayInteractable>(canvas.gameObject);
            ray.InjectSurface(surface);
            ray.InjectOptionalPointableElement(pointable);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        return "Saved Categories, F1's three demo modes and controller/hand UI interaction; Homepage unchanged.";
    }

    private static GameObject Root(string name, Scene scene)
    {
        var root = new GameObject(name, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(root, scene);
        root.SetActive(false);
        ((RectTransform)root.transform).sizeDelta = new Vector2(1200, 880);
        var glass = root.AddComponent<ODHomepageSurface>();
        glass.cornerRadius = 52;
        glass.cornerSegments = 24;
        glass.color = Color.white;
        glass.topLeft = new Color(.025f, .04f, .075f, .94f);
        glass.bottomRight = new Color(.01f, .015f, .028f, .93f);
        glass.raycastTarget = true;
        Border(root, .14f);
        return root;
    }

    private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(.5f, .5f);
    }

    private static void Border(GameObject go, float alpha)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1, 1, 1, alpha);
        outline.effectDistance = new Vector2(1, -1);
        outline.useGraphicAlpha = true;
    }

    private static RectTransform Card(Transform parent, string name, float x, float y, float w, float h)
    {
        var rect = Rect(parent, name, x, y, w, h);
        var image = rect.gameObject.AddComponent<ODHomepageSurface>();
        image.color = Color.white;
        image.cornerRadius = 32;
        image.cornerSegments = 24;
        image.topLeft = new Color(.10f, .12f, .17f, .75f);
        image.bottomRight = new Color(.065f, .08f, .12f, .68f);
        image.raycastTarget = false;
        Border(rect.gameObject, .15f);
        return rect;
    }

    private static RoundedImage Fill(Transform parent, string name, float x, float y, float w, float h, Color color, float radius = 20)
    {
        var rect = Rect(parent, name, x, y, w, h);
        var image = rect.gameObject.AddComponent<RoundedImage>();
        image.color = color;
        image.cornerRadius = radius;
        image.cornerSegments = 16;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string text, float x, float y, float w, float h,
        float size = 26, bool bold = false, bool centered = false, bool secondary = false)
    {
        var rect = Rect(parent, name, x, y, w, h);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Font;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = centered ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
        label.color = secondary ? Secondary : Color.white;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private static ODNavigationIcon Icon(Transform parent, string name, ODNavigationIcon.Symbol symbol, float x, float y, float size)
    {
        var rect = Rect(parent, name, x, y, size, size);
        var icon = rect.gameObject.AddComponent<ODNavigationIcon>();
        icon.symbol = symbol;
        icon.color = Color.white;
        icon.raycastTarget = false;
        return icon;
    }

    private static Button Button(Transform parent, string name, string label, float x, float y, float w, float h, bool primary = false, float size = 23)
    {
        var graphic = Fill(parent, name, x, y, w, h, Color.white, h * .5f);
        graphic.raycastTarget = true;
        Border(graphic.gameObject, .15f);
        var button = graphic.gameObject.AddComponent<Button>();
        button.targetGraphic = graphic;
        var colors = button.colors;
        colors.normalColor = primary ? Color.white : ButtonColor;
        colors.highlightedColor = primary ? new Color(.83f, .89f, .97f) : new Color(.57f, .65f, .76f, .9f);
        colors.pressedColor = new Color(.62f, .73f, .90f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(.45f, .48f, .54f, .55f);
        colors.fadeDuration = .1f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        if (!string.IsNullOrEmpty(label))
        {
            var text = Text(graphic.transform, "Label", label, 10, 0, w - 20, h, size, true, true);
            if (primary) text.color = new Color(.08f, .10f, .15f);
        }
        return button;
    }

    private static void Rule(Transform parent, float y, float width, float x = 32)
        => Fill(parent, "Divider", x, y, width, 1, LineColor, 0);

    private static void SaveDirectory()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = Root("CategoriesPanel", scene);
            var directory = root.AddComponent<ODCategoriesPage>();
            Text(root.transform, "Brand", "Overdrive", 32, 26, 300, 70, 44, true);
            Text(root.transform, "Heading", "Catégories", 32, 114, 400, 38, 25, true);
            directory.profileButton = Button(root.transform, "ProfileButton", "", 988, 34, 180, 64);
            Icon(directory.profileButton.transform, "ProfileIcon", ODNavigationIcon.Symbol.Profile, 20, 17, 30);
            Text(directory.profileButton.transform, "Label", "Profil", 62, 0, 106, 64, 24, true);

            var searchGraphic = Fill(root.transform, "CategorySearch", 360, 34, 596, 64, new Color(.43f, .48f, .59f, .43f), 32);
            searchGraphic.raycastTarget = true;
            Border(searchGraphic.gameObject, .12f);
            Icon(searchGraphic.transform, "Magnifier", ODNavigationIcon.Symbol.Search, 19, 16, 32);
            var area = Rect(searchGraphic.transform, "TextArea", 68, 10, 508, 44);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = Text(area, "Placeholder", "Rechercher une catégorie", 0, 0, 508, 44, 24, secondary: true);
            var value = Text(area, "Text", "", 0, 0, 508, 44, 24);
            directory.search = searchGraphic.gameObject.AddComponent<TMP_InputField>();
            directory.search.targetGraphic = searchGraphic;
            directory.search.textViewport = area;
            directory.search.textComponent = value;
            directory.search.placeholder = placeholder;
            directory.search.fontAsset = Font;
            directory.search.pointSize = 24;
            directory.search.lineType = TMP_InputField.LineType.SingleLine;
            directory.search.characterLimit = 80;
            directory.search.customCaretColor = true;
            directory.search.caretColor = Color.white;
            directory.search.selectionColor = new Color(.5f, .7f, 1, .4f);

            var scroller = Rect(root.transform, "CategoryScroll", 32, 170, 1100, 670);
            var viewport = Rect(scroller, "Viewport", 0, 0, 1100, 670);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, .001f);
            hit.raycastTarget = true;
            var content = Rect(viewport, "Content", 0, 0, 1100, 984);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.sizeDelta = new Vector2(0, 984);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(538, 312);
            grid.spacing = new Vector2(24, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperLeft;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            directory.scroll = scroller.gameObject.AddComponent<ScrollRect>();
            directory.scroll.content = content;
            directory.scroll.viewport = viewport;
            directory.scroll.horizontal = false;
            directory.scroll.vertical = true;
            directory.scroll.movementType = ScrollRect.MovementType.Clamped;
            directory.scroll.scrollSensitivity = 35;
            var track = Fill(root.transform, "ScrollTrack", 1148, 176, 16, 656, new Color(1, 1, 1, .12f), 8);
            track.raycastTarget = true;
            var handle = Fill(track.transform, "Handle", 0, 0, 16, 240, new Color(1, 1, 1, .72f), 8);
            Stretch(handle.rectTransform);
            handle.raycastTarget = true;
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            directory.scroll.verticalScrollbar = scrollbar;
            directory.scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            directory.cards = new ODCategoriesPage.Card[6];
            for (int i = 0; i < 6; i++)
            {
                var card = Card(content, i == 0 ? "F1Card" : "ComingSoonCard" + i, (i % 2) * 562, (i / 2) * 336, 538, 312);
                var graphic = card.GetComponent<Graphic>();
                graphic.raycastTarget = true;
                var open = card.gameObject.AddComponent<Button>();
                open.targetGraphic = graphic;
                open.interactable = i == 0;
                open.navigation = new Navigation { mode = Navigation.Mode.None };
                var colors = open.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(.78f, .87f, 1);
                colors.pressedColor = new Color(.56f, .70f, .95f);
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(1, 1, 1, .75f);
                open.colors = colors;
                if (i == 0)
                {
                    Text(card, "SportName", "F1", 48, 66, 442, 102, 80, true, true);
                    Rule(card, 186, 442, 48);
                    Text(card, "MockGrandPrix", "GP de Singapour", 48, 207, 442, 54, 29, true, true);
                }
                else Text(card, "ComingSoon", "Bientôt disponible", 38, 124, 462, 64, 32, true, true, true);
                var favorite = Button(card, "FavoriteButton", "", 464, 20, 54, 54);
                var star = Icon(favorite.transform, "Star", ODNavigationIcon.Symbol.Star, 11, 11, 32);
                star.color = Secondary;
                directory.cards[i] = new ODCategoriesPage.Card
                {
                    id = i == 0 ? "f1" : "coming-" + i,
                    searchTerms = i == 0 ? "F1 Formule 1 Formula 1 GP de Singapour" : "Bientôt disponible prochainement coming soon",
                    available = i == 0, root = card.gameObject, openButton = open, favoriteButton = favorite, favoriteIcon = star
                };
            }
            directory.noResults = Text(root.transform, "NoResults", "Aucune catégorie trouvée", 32, 360, 1100, 60, 28, centered: true, secondary: true).gameObject;
            directory.noResults.SetActive(false);
            root.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            PrefabUtility.SaveAsPrefabAsset(root, DirectoryPath);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void SaveSport()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = Root("F1SessionPanel", scene);
            var sport = root.AddComponent<ODSportPage>();
            sport.background = root.GetComponent<ODHomepageSurface>();
            sport.backButton = Button(root.transform, "BackToCategories", "", 32, 32, 52, 52);
            Icon(sport.backButton.transform, "BackArrow", ODNavigationIcon.Symbol.Back, 11, 11, 30);
            sport.heading = Text(root.transform, "Heading", "GP de Singapour", 104, 22, 764, 66, 44, true);
            sport.subtitle = Text(root.transform, "Subtitle", "Formule 1 · GP de Singapour", 104, 89, 744, 36, 23, secondary: true);
            sport.statusSurface = Fill(root.transform, "StatusBadge", 898, 32, 270, 52, new Color(1, .73f, .3f, .2f), 26);
            Border(sport.statusSurface.gameObject, .20f);
            sport.status = Text(sport.statusSurface.transform, "Label", "Week-end en cours", 12, 0, 246, 52, 22, true, true);
            sport.demoButton = Button(root.transform, "DemoModeButton", "Mode démo · Week-end", 898, 98, 270, 44, size: 17);
            sport.demoLabel = sport.demoButton.GetComponentInChildren<TextMeshProUGUI>();
            sport.modePanels = new[]
            {
                Rect(root.transform, "OffWeekend", 0, 0, 1200, 880).gameObject,
                Rect(root.transform, "Weekend", 0, 0, 1200, 880).gameObject,
                Rect(root.transform, "LiveSession", 0, 0, 1200, 880).gameObject
            };
            OffWeekend(sport.modePanels[0].transform);
            Weekend(sport.modePanels[1].transform);
            Live(sport.modePanels[2].transform);
            sport.SetMode(ODSportPage.Mode.Weekend);
            root.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, SportPath);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void OffWeekend(Transform parent)
    {
        var next = Card(parent, "NextEvent", 32, 174, 414, 420);
        Text(next, "Label", "PROCHAIN ÉVÉNEMENT", 32, 28, 350, 40, 20, secondary: true);
        Text(next, "MockEvent", "GP de Singapour", 32, 82, 350, 52, 33, true);
        Text(next, "Sport", "Formule 1", 32, 137, 350, 36, 24, secondary: true);
        for (int i = 0; i < 3; i++)
        {
            Text(next, "CountdownValue" + i, "--", 32 + i * 116, 207, 106, 70, 54, true, true);
            Text(next, "CountdownUnit" + i, new[] { "JOURS", "HEURES", "MIN" }[i], 32 + i * 116, 279, 106, 32, 18, centered: true, secondary: true);
        }
        Button(next, "ViewEvent", "Voir l'événement", 32, 338, 310, 56, true);
        var calendar = Card(parent, "Calendar", 32, 618, 414, 222);
        Text(calendar, "Heading", "Calendrier", 32, 26, 350, 48, 32, true);
        var standings = Card(parent, "ChampionshipStandings", 470, 174, 698, 666);
        Text(standings, "Heading", "Classement", 32, 28, 300, 52, 34, true);
        Button(standings, "Drivers", "Pilotes", 370, 30, 136, 46, size: 22);
        Button(standings, "Teams", "Équipes", 518, 30, 148, 46, size: 22);
        Text(standings, "NameColumn", "NOM", 32, 109, 400, 36, 21, secondary: true);
        Text(standings, "PointsColumn", "PTS", 586, 109, 80, 36, 21, secondary: true);
        for (int i = 0; i < 6; i++) Rule(standings, 153 + i * 70, 634);
        Button(standings, "FullStandings", "Classement complet", 32, 578, 316, 56);
    }

    private static void Weekend(Transform parent)
    {
        var circuit = Card(parent, "CircuitWeather", 32, 174, 344, 478);
        Text(circuit, "Heading", "CIRCUIT · MÉTÉO", 28, 24, 288, 40, 20, secondary: true);
        Rule(circuit, 254, 288, 28);
        string[] labels = { "Longueur", "Tours", "Pluie", "Air", "Piste", "Vent" };
        for (int i = 0; i < 6; i++)
        {
            float x = 28 + (i % 3) * 100;
            float y = 279 + (i / 3) * 96;
            Text(circuit, "MetricLabel" + i, labels[i], x, y, 98, 30, 19, secondary: true);
            Text(circuit, "MetricValue" + i, "--", x, y + 34, 98, 42, 30, true);
        }
        var next = Card(parent, "NextSession", 32, 676, 344, 164);
        Text(next, "Heading", "PROCHAINE SESSION", 28, 22, 288, 36, 20, secondary: true);
        Text(next, "Countdown", "-- min", 28, 71, 288, 58, 40, true);
        var program = Card(parent, "Program", 400, 174, 384, 666);
        Text(program, "Heading", "Programme", 32, 28, 320, 52, 34, true);
        string[] sessions = { "EL1", "EL2", "EL3", "Qualifications", "Course" };
        for (int i = 0; i < 5; i++)
        {
            Text(program, "Session" + i, sessions[i], 32, 105 + i * 86, 320, 38, 27, true);
            Text(program, "Time" + i, "--", 32, 144 + i * 86, 320, 30, 22, secondary: true);
            if (i < 4) Rule(program, 182 + i * 86, 320);
        }
        Button(program, "FollowLive", "Suivre le direct", 32, 578, 320, 56, true);
        var standings = Card(parent, "Standings", 808, 174, 360, 666);
        Text(standings, "Heading", "Classement", 32, 28, 296, 52, 34, true);
        Text(standings, "NameColumn", "NOM", 32, 109, 220, 36, 21, secondary: true);
        Text(standings, "PointsColumn", "PTS", 272, 109, 56, 36, 21, secondary: true);
        for (int i = 0; i < 6; i++) Rule(standings, 153 + i * 80, 296);
    }

    private static void Live(Transform parent)
    {
        var podium = Card(parent, "Podium", 32, 174, 404, 236);
        for (int i = 0; i < 3; i++)
        {
            Text(podium, "Position" + i, "P" + (i + 1), 32, 21 + i * 70, 64, 48, 27, true);
            if (i < 2) Rule(podium, 85 + i * 70, 340);
        }
        Button(parent, "TVLive", "TV Live", 32, 434, 190, 58, true);
        Button(parent, "Telemetry", "Télémétrie", 246, 434, 190, 58);
        var circuit = Card(parent, "Circuit", 32, 516, 404, 324);
        Text(circuit, "Heading", "CIRCUIT", 32, 24, 340, 40, 20, secondary: true);
        Text(circuit, "LapProgress", "-- / -- tours", 32, 239, 340, 30, 22, secondary: true);
        Fill(circuit, "ProgressTrack", 32, 284, 340, 7, new Color(1, 1, 1, .15f), 4);
        var standings = Card(parent, "SessionStandings", 460, 174, 708, 666);
        Text(standings, "Heading", "Classement de la session", 32, 28, 644, 52, 32, true);
        string[] headers = { "PILOTE", "PNEU", "ARR.", "ÉCART", "DERN. TOUR" };
        float[] positions = { 32, 242, 326, 406, 510 };
        float[] widths = { 196, 72, 64, 96, 164 };
        for (int i = 0; i < headers.Length; i++) Text(standings, "Column" + i, headers[i], positions[i], 109, widths[i], 36, 18, secondary: true);
        for (int i = 0; i < 9; i++) Rule(standings, 155 + i * 56, 644);
    }
}
