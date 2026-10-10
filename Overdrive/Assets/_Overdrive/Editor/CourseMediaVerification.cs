using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Isolated course integration tests, without modifying the user's scene.</summary>
public static class CourseMediaVerification
{
    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    public static string RunAndCapture()
    {
        Check(!EditorApplication.isPlaying, "Exit Play mode first.");
        var source = UnityEngine.Object.FindAnyObjectByType<OverdriveMainMenu>(FindObjectsInactive.Include);
        var preview = EditorSceneManager.NewPreviewScene();
        var randomState = UnityEngine.Random.state;
        try
        {
            var root = new GameObject("CourseIntegrationPreview", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(root, preview);
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)root.transform).sizeDelta = new Vector2(1200, 880);
            root.transform.localScale = Vector3.one * .001f;
            var clone = UnityEngine.Object.Instantiate(source.gameObject, root.transform, false);
            var menu = clone.GetComponent<OverdriveMainMenu>();
            var sport = menu.sportPanel.GetComponent<ODSportPage>();
            // External scene windows must never be changed by this isolated preview.
            sport.circuitPreview = null;
            var tv = sport.television;
            var panelClone = UnityEngine.Object.Instantiate(source.sportPanel.GetComponent<ODSportPage>().television.videoPanel);
            SceneManager.MoveGameObjectToScene(panelClone, preview);
            panelClone.GetComponent<ODCourseVideoDock>().courseWindow = (RectTransform)menu.transform;
            panelClone.GetComponent<ODCourseVideoDock>().Align();
            CourseMediaSetup.ConnectPanel(tv, panelClone);
            var ranking = tv.standingsPanel.GetComponent<RaceRankingManager>();
            var catalog = menu.replayCatalogPanel.GetComponent<ODReplayCatalog>();
            catalog.Initialize();
            sport.Initialize();
            tv.Initialize();
            menu.ShowSport();
            sport.SetMode(ODSportPage.Mode.LiveSession);
            UnityEngine.Random.InitState(31);
            ranking.Initialize();
            Check(ranking.DriverCount == 20 && ranking.CurrentLap == 1, "Missing dynamic rows.");
            Check(!tv.IsVisible && tv.standingsPanel.activeInHierarchy, "Ranking must exist before opening TV.");
            var initialCards = ranking.container.GetComponentsInChildren<RaceStandingCard>();
            var before = initialCards.Select(x => x.rankText.text).ToArray();
            ranking.SimulateOvertake();
            ranking.AdvanceLap();
            Check(ranking.CurrentLap == 2 && !before.SequenceEqual(initialCards.Select(x => x.rankText.text)), "Hidden-video ranking did not advance/reorder.");
            tv.tvButtons[0].onClick.Invoke();
            Check(tv.IsVisible && tv.WantsPlayback && sport.gameObject.activeSelf && menu.GetComponentInParent<Canvas>().gameObject.activeSelf, "TV replaced the course.");
            tv.playPauseButton.onClick.Invoke();
            Check(!tv.WantsPlayback && tv.playPauseLabel.text == "Lecture", "Pause failed.");
            tv.playPauseButton.onClick.Invoke();
            Check(tv.WantsPlayback && tv.playPauseLabel.text == "Pause", "Play failed.");
            tv.volumeSlider.value = .23f;
            Check(Mathf.Abs(tv.volume - .23f) < .001f && tv.volumeLabel.text == "23%", "Volume UI failed.");
            tv.SetVolume(2);
            Check(tv.volume == 1 && tv.volumeSlider.value == 1, "Volume clamp failed.");
            tv.SetVolume(.7f);
            tv.tvButtons[0].onClick.Invoke();
            Check(!tv.IsVisible && !tv.WantsPlayback && tv.standingsPanel.activeInHierarchy && ranking.CurrentLap == 2, "Hiding TV hid/reset ranking.");
            Check(initialCards.SequenceEqual(ranking.container.GetComponentsInChildren<RaceStandingCard>()), "TV duplicated standings.");
            Canvas.ForceUpdateCanvases();
            tv.standingsScroll.verticalNormalizedPosition = 0;
            Canvas.ForceUpdateCanvases();
            Check(ranking.container.anchoredPosition.y > 0, "Cannot reach last drivers.");

            var cameraGO = new GameObject("CourseCaptureCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraGO, preview);
            var camera = cameraGO.GetComponent<Camera>();
            camera.enabled = false;
            camera.scene = preview;
            camera.orthographic = true;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 10;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.20f, .22f, .25f);
            Capture(camera, root, menu.transform, false, "/private/tmp/overdrive-course-standings-bottom.png");
            tv.standingsScroll.verticalNormalizedPosition = 1;
            Capture(camera, root, menu.transform, false, "/private/tmp/overdrive-course-tv-hidden.png");
            tv.tvButtons[0].onClick.Invoke();
            tv.playbackStatus.text = "Vidéo mock · Pas de flux connecté";
            Capture(camera, root, menu.transform, true, "/private/tmp/overdrive-course-tv-live.png");
            sport.SetMode(ODSportPage.Mode.Replay);
            Check(!tv.IsVisible && tv.standingsPanel.activeInHierarchy && ranking.DriverCount == 20, "Mode switch left live TV open or lost ranking.");
            tv.tvButtons[1].onClick.Invoke();
            Check(tv.IsVisible && tv.titleLabel.text.StartsWith("TV Replay"), "Replay TV failed.");
            tv.playbackStatus.text = "Vidéo mock · Pas de flux connecté";
            Capture(camera, root, menu.transform, true, "/private/tmp/overdrive-course-tv-replay.png");
            tv.tvButtons[1].onClick.Invoke();
            Check(!tv.IsVisible && tv.standingsPanel.activeInHierarchy, "Replay TV toggle failed.");
            tv.tvButtons[1].onClick.Invoke();
            sport.replayButton.onClick.Invoke();
            // Match runtime lifecycle explicitly in this Edit-mode preview.
            typeof(VideoPlayerController).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(tv, null);
            Check(catalog.gameObject.activeSelf && !tv.IsVisible, "Catalogue left TV open.");
            catalog.entries[2].openButton.onClick.Invoke();
            Check(sport.currentMode == ODSportPage.Mode.Replay && !tv.IsVisible && tv.titleLabel.text.Contains("Italie"), "Replay selection did not update media context.");
            tv.tvButtons[1].onClick.Invoke();
            menu.ShowHomepage();
            // OnDisable is not automatically called for normal MonoBehaviours in Edit mode.
            typeof(VideoPlayerController).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(tv, null);
            Check(!tv.IsVisible && menu.homepagePanel.activeSelf, "Leaving the course left TV open.");
            menu.ShowSport();
            foreach (var mode in new[] { ODSportPage.Mode.OffWeekend, ODSportPage.Mode.Weekend })
            {
                sport.SetMode(mode);
                tv.ToggleVideo();
                Check(!tv.IsVisible && !tv.standingsPanel.activeSelf, "TV/race ranking leaked into a non-session state.");
            }
            Check(clone.GetComponentsInChildren<RaceRankingManager>(true).Length == 1 && clone.GetComponentsInChildren<VideoPlayerController>(true).Length == 1, "Duplicate media/ranking managers.");
            return "PASS: 20 animated drivers with TV hidden, overtaking/lap updates, persistent shared ranking, live/replay TV toggles, play/pause/volume, scroll to final drivers, session/catalogue/home exit cleanup, non-session isolation. Four visual captures saved.";
        }
        finally
        {
            UnityEngine.Random.state = randomState;
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static void Capture(Camera camera, GameObject root, Transform menu, bool wide, string path)
    {
        int width = wide ? 2200 : 1200;
        int height = wide ? 930 : 880;
        camera.orthographicSize = wide ? .465f : .4664f;
        camera.transform.SetPositionAndRotation(menu.TransformPoint(new Vector3(wide ? -462 : 0, 0, -2000)), menu.rotation);
        var sceneRoots = root.scene.GetRootGameObjects();
        foreach (var canvas in sceneRoots.SelectMany(x => x.GetComponentsInChildren<Canvas>(true))) canvas.worldCamera = camera;
        Canvas.ForceUpdateCanvases();
        foreach (var label in sceneRoots.SelectMany(x => x.GetComponentsInChildren<TextMeshProUGUI>()))
        {
            label.ForceMeshUpdate(true);
            Check(!label.isTextOverflowing && label.preferredWidth <= label.rectTransform.rect.width + 1,
                "Text overflow: " + label.name + " = " + label.text);
        }
        foreach (var graphic in sceneRoots.SelectMany(x => x.GetComponentsInChildren<Graphic>())) graphic.Rebuild(CanvasUpdate.PreRender);
        Canvas.ForceUpdateCanvases();
        foreach (var mask in sceneRoots.SelectMany(x => x.GetComponentsInChildren<RectMask2D>())) mask.PerformClipping();
        var render = new RenderTexture(width, height, 24);
        var previous = RenderTexture.active;
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = render;
            camera.Render();
            RenderTexture.active = render;
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
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
