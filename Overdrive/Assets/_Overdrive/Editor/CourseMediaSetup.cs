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
using UnityEngine.Video;
using static CategoryPagesSetup;

/// <summary>One-time migration into a saved course prefab, never a runtime UI builder.</summary>
public static class CourseMediaSetup
{
    public const string VideoPath = "Assets/_Overdrive/UI/Prefabs/Screens/OverdriveCourseTV.prefab";

    public static void ConnectPanel(VideoPlayerController television, GameObject panel)
    {
        television.videoPanel = panel;
        television.videoPlayer = panel.GetComponent<VideoPlayer>();
        television.titleLabel = panel.transform.Find("Title").GetComponent<TextMeshProUGUI>();
        television.playPauseButton = panel.transform.Find("PlayPause").GetComponent<Button>();
        television.playPauseLabel = panel.transform.Find("PlayPause/Label").GetComponent<TextMeshProUGUI>();
        television.playbackStatus = panel.transform.Find("PlaybackStatus").GetComponent<TextMeshProUGUI>();
        television.volumeSlider = panel.transform.Find("Volume").GetComponent<Slider>();
        television.volumeLabel = panel.transform.Find("VolumeValue").GetComponent<TextMeshProUGUI>();
    }

    public static string SeparateInputCanvas()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/_Overdrive/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Open SampleScene in Edit mode.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(VideoPath) != null)
            throw new InvalidOperationException("Course TV asset already exists.");
        var menu = UnityEngine.Object.FindAnyObjectByType<OverdriveMainMenu>(FindObjectsInactive.Include);
        var root = PrefabUtility.LoadPrefabContents(SportPath);
        try
        {
            var television = root.GetComponent<ODSportPage>().television;
            var panel = television.videoPanel;
            if (panel == null || panel.transform.parent != root.transform)
                throw new InvalidOperationException("Expected nested course panel.");
            var standalone = UnityEngine.Object.Instantiate(panel);
            SceneManager.MoveGameObjectToScene(standalone, root.scene);
            try
            {
                standalone.name = "OverdriveCourseTV";
                var rect = (RectTransform)standalone.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.anchoredPosition3D = Vector3.zero;
                rect.localScale = Vector3.one * .001f;
                standalone.AddComponent<ODCourseVideoDock>();
                PrefabUtility.SaveAsPrefabAsset(standalone, VideoPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(standalone); }
            UnityEngine.Object.DestroyImmediate(panel);
            PrefabUtility.SaveAsPrefabAsset(root, SportPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VideoPath), scene);
        Undo.RegisterCreatedObjectUndo(instance, "Dock course TV with independent SDK canvas");
        var dock = instance.GetComponent<ODCourseVideoDock>();
        dock.courseWindow = (RectTransform)menu.transform;
        dock.Align();
        var controller = menu.sportPanel.GetComponent<ODSportPage>().television;
        ConnectPanel(controller, instance);
        foreach (var component in new Component[] { dock, controller, instance.transform })
        {
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        instance.SetActive(false);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Save failed.");
        return "Course-owned TV dock now has its own root canvas, as required by Meta PointableCanvasModule.";
    }
    public static string Install()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/_Overdrive/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Open SampleScene in Edit mode.");
        var menu = UnityEngine.Object.FindAnyObjectByType<OverdriveMainMenu>(FindObjectsInactive.Include);
        var oldRanking = UnityEngine.Object.FindObjectsByType<RaceRankingManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .SingleOrDefault(x => x.name == "RankingWidget" && x.gameObject.scene == scene);
        var oldVideo = scene.GetRootGameObjects().SingleOrDefault(x => x.name == "VideoPlayerCanvas");
        if (menu == null || oldRanking == null || oldVideo == null || menu.sportPanel.GetComponent<ODSportPage>().television != null)
            throw new InvalidOperationException("Legacy source missing or migration already installed.");
        const string snapshot = "Assets/_Overdrive/Scenes/SampleScene_BeforeCourseMedia.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(snapshot) == null && !EditorSceneManager.SaveScene(scene, snapshot, true))
            throw new InvalidOperationException("Snapshot failed.");
        var mediaSource = oldVideo.GetComponent<VideoPlayer>();
        if (mediaSource == null || mediaSource.clip == null || mediaSource.targetTexture == null)
            throw new InvalidOperationException("Mock clip or render texture missing.");
        var root = PrefabUtility.LoadPrefabContents(SportPath);
        try
        {
            var sport = root.GetComponent<ODSportPage>();
            var television = root.AddComponent<VideoPlayerController>();
            sport.television = television;
            television.course = sport;
            television.tvButtons = new[] {
                root.transform.Find("LiveSession/TVLive").GetComponent<Button>(),
                root.transform.Find("Replay/TVReplay").GetComponent<Button>() };
            foreach (var button in television.tvButtons)
                button.transform.Find("Label").GetComponent<TextMeshProUGUI>().color = Color.white;
            foreach (var path in new[] { "LiveSession/SessionStandings", "Replay/SessionStandings", "Replay/DemoMediaNotice" })
            {
                var child = root.transform.Find(path);
                if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            AddStandings(root.transform, television, oldRanking);
            AddVideo(root.transform, television, mediaSource);
            television.videoPanel.SetActive(false);
            television.standingsPanel.SetActive(false);
            // Do not initialize runtime button listeners while authoring the asset.
            PrefabUtility.SaveAsPrefabAsset(root, SportPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        var migrated = menu.sportPanel.GetComponent<ODSportPage>();
        if (migrated.television == null || migrated.television.videoPanel == null)
            throw new InvalidOperationException("Saved migration did not reach the scene instance.");
        Undo.DestroyObjectImmediate(oldVideo);
        Undo.DestroyObjectImmediate(oldRanking.gameObject);
        var oldGrid = menu.transform.Find("Body/ContentArea/GridScrollView");
        if (oldGrid != null) Undo.DestroyObjectImmediate(oldGrid.gameObject);
        // Homepage, profile, categories, replay catalogue and XR rig are left intact.
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        SeparateInputCanvas();
        return "Saved course-owned TV with play/pause/volume and one animated standings table. Removed legacy player, ranking window and thumbnail grid; recovery snapshot retained.";
    }

    private static void AddStandings(Transform parent, VideoPlayerController television, RaceRankingManager source)
    {
        var panel = Card(parent, "CourseStandings", 460, 174, 708, 666);
        television.standingsPanel = panel.gameObject;
        television.standingsHeading = Text(panel, "Heading", "Classement de la session", 24, 20, 480, 44, 30, true);
        var lap = Text(panel, "LapText", "Tour 1/54", 522, 26, 162, 32, 22, true);
        lap.alignment = TextAlignmentOptions.MidlineRight;
        Text(panel, "DemoNotice", "Classement animé de démonstration", 24, 70, 630, 24, 17, secondary: true);
        string[] labels = { "PILOTE", "PNEU", "ARR.", "ÉCART", "DERN. TOUR" };
        float[] positions = { 88, 262, 346, 408, 520 };
        float[] widths = { 150, 64, 48, 90, 144 };
        for (int i = 0; i < labels.Length; i++) Text(panel, "Column" + i, labels[i], positions[i], 114, widths[i], 26, 16, secondary: true);
        var scroll = Rect(panel, "StandingsScroll", 24, 150, 640, 464);
        var viewport = Rect(scroll, "Viewport", 0, 0, 640, 464);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, .001f);
        var rows = Rect(viewport, "Rows", 0, 0, 640, 1074);
        television.standingsScroll = scroll.gameObject.AddComponent<ScrollRect>();
        television.standingsScroll.content = rows;
        television.standingsScroll.viewport = viewport;
        television.standingsScroll.horizontal = false;
        television.standingsScroll.vertical = true;
        television.standingsScroll.movementType = ScrollRect.MovementType.Clamped;
        television.standingsScroll.scrollSensitivity = 35;
        var track = Fill(panel, "ScrollTrack", 674, 152, 10, 460, new Color(1, 1, 1, .12f), 5);
        track.raycastTarget = true;
        var handle = Fill(track.transform, "Handle", 0, 0, 10, 200, new Color(1, 1, 1, .65f), 5);
        Stretch(handle.rectTransform);
        handle.raycastTarget = true;
        var scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
        television.standingsScroll.verticalScrollbar = scrollbar;
        Text(panel, "ScrollHint", "20 pilotes · Faites défiler pour voir la suite", 24, 630, 650, 24, 17, secondary: true);

        var row = Fill(rows, "RowTemplate", 0, 0, 640, 48, new Color(1, 1, 1, .055f), 15);
        var card = row.gameObject.AddComponent<RaceStandingCard>();
        card.rankText = Text(row.transform, "RankText", "1", 8, 0, 40, 48, 22, true, true);
        card.teamDot = Fill(row.transform, "TeamDot", 58, 15, 18, 18, Color.white, 9);
        card.codeText = Text(row.transform, "CodeText", "DRV", 86, 0, 120, 48, 24, true);
        Text(row.transform, "Tyre", "--", 238, 0, 64, 48, 20, secondary: true);
        Text(row.transform, "Stops", "--", 322, 0, 48, 48, 20, secondary: true);
        card.gapText = Text(row.transform, "GapText", "+0.000", 384, 0, 94, 48, 21, true);
        Text(row.transform, "LastLap", "--:--.---", 496, 0, 138, 48, 20, secondary: true);
        row.gameObject.SetActive(false);
        var ranking = panel.gameObject.AddComponent<RaceRankingManager>();
        EditorUtility.CopySerialized(source, ranking);
        ranking.cardTemplate = row.gameObject;
        ranking.container = rows;
        ranking.lapText = lap;
        ranking.rowHeight = 48;
        ranking.rowSpacing = 6;
    }

    private static void AddVideo(Transform parent, VideoPlayerController television, VideoPlayer source)
    {
        var panel = Card(parent, "CourseTV", -924, 174, 892, 642);
        television.videoPanel = panel.gameObject;
        var canvas = panel.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        panel.gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10;
        panel.gameObject.AddComponent<GraphicRaycaster>();
        television.titleLabel = Text(panel, "Title", "TV Live · GP de Singapour", 24, 18, 844, 48, 27, true);
        var surface = Rect(panel, "VideoSurface", 24, 84, 844, 474.75f);
        var raw = surface.gameObject.AddComponent<RawImage>();
        raw.texture = source.targetTexture;
        raw.raycastTarget = false;
        television.videoPlayer = panel.gameObject.AddComponent<VideoPlayer>();
        var player = television.videoPlayer;
        player.clip = source.clip;
        player.targetTexture = source.targetTexture;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.audioOutputMode = VideoAudioOutputMode.Direct;
        player.controlledAudioTrackCount = (ushort)Mathf.Min((int)source.clip.audioTrackCount, 1);
        player.playOnAwake = false;
        player.waitForFirstFrame = true;
        player.isLooping = false;
        television.playPauseButton = Button(panel, "PlayPause", "Lecture", 24, 578, 180, 44);
        television.playPauseLabel = television.playPauseButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        Text(panel, "VolumeTitle", "Volume", 230, 580, 104, 40, 23);
        var sliderRect = Rect(panel, "Volume", 348, 582, 264, 36);
        var sliderHit = sliderRect.gameObject.AddComponent<Image>();
        sliderHit.color = new Color(1, 1, 1, .001f);
        var track = Fill(sliderRect, "Track", 0, 14, 264, 8, new Color(1, 1, 1, .16f), 4);
        var fillArea = Rect(sliderRect, "FillArea", 0, 14, 264, 8);
        var fill = Fill(fillArea, "Fill", 0, 0, 264, 8, new Color(.66f, .83f, 1), 4);
        Stretch(fill.rectTransform);
        var handleArea = Rect(sliderRect, "HandleArea", 0, 0, 264, 36);
        var handle = Fill(handleArea, "Handle", 0, 4, 28, 28, Color.white, 14);
        handle.rectTransform.pivot = new Vector2(.5f, .5f);
        handle.rectTransform.anchoredPosition = Vector2.zero;
        // Slider drives vertical anchors to 0..1; keep a 28-high handle in a 36-high track.
        handle.rectTransform.sizeDelta = new Vector2(28, -8);
        handle.raycastTarget = true;
        television.volumeSlider = sliderRect.gameObject.AddComponent<Slider>();
        television.volumeSlider.minValue = 0;
        television.volumeSlider.maxValue = 1;
        television.volumeSlider.value = .7f;
        television.volumeSlider.fillRect = fill.rectTransform;
        television.volumeSlider.handleRect = handle.rectTransform;
        television.volumeSlider.targetGraphic = handle;
        television.volumeSlider.navigation = new Navigation { mode = Navigation.Mode.None };
        television.volumeLabel = Text(panel, "VolumeValue", "70%", 628, 580, 80, 40, 23, true);
        // Put the mock notice above the controls, away from the image.
        television.playbackStatus = Text(panel, "PlaybackStatus", "Vidéo mock", 24, 62, 844, 20, 15, secondary: true);

        var pointable = panel.gameObject.AddComponent<PointableCanvas>();
        pointable.InjectCanvas(canvas);
        var plane = panel.gameObject.AddComponent<PlaneSurface>();
        var bounds = panel.gameObject.AddComponent<BoundsClipper>();
        bounds.Position = new Vector3(panel.rect.center.x, panel.rect.center.y, 0);
        bounds.Size = new Vector3(892, 642, .01f);
        var driver = panel.gameObject.AddComponent<RectTransformBoundsClipperDriver>();
        var serialized = new SerializedObject(driver);
        serialized.FindProperty("_boundsClipper").objectReferenceValue = bounds;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var clipped = panel.gameObject.AddComponent<ClippedPlaneSurface>();
        clipped.InjectAllClippedPlaneSurface(plane, new[] { bounds });
        var ray = panel.gameObject.AddComponent<RayInteractable>();
        ray.InjectSurface(clipped);
        ray.InjectOptionalPointableElement(pointable);
        var poke = panel.gameObject.AddComponent<PokeInteractable>();
        poke.InjectAllPokeInteractable(clipped);
        poke.InjectOptionalPointableElement(pointable);
    }
}
