using System;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One-time saved-prefab integration, not an additional legacy Build All menu.</summary>
public static class VideoMultiviewSetup
{
    public const string VideoPath = "Assets/_Overdrive/UI/Prefabs/Screens/OverdriveCourseTV.prefab";
    public const string PilotPath = "Assets/_Overdrive/UI/Prefabs/Screens/OverdrivePilotVideo.prefab";
    public const string PickerPath = "Assets/_Overdrive/UI/Prefabs/Screens/OverdriveViewPicker.prefab";

    public static string Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/_Overdrive/Scenes/Overdrive 0.5.unity") throw new InvalidOperationException("Open the Overdrive scene first.");
        // Saved canvases keep their own SDK bridge: nested canvases are rejected by PointableCanvasModule.
        CreateAuxiliary(PilotPath, "OverdrivePilotVideo", new Vector2(704, 396), true);
        CreateAuxiliary(PickerPath, "OverdriveViewPicker", new Vector2(1120, 660), false);
        var root = PrefabUtility.LoadPrefabContents(VideoPath);
        try
        {
            var player = root.GetComponent<UnityEngine.Video.VideoPlayer>();
            player.Stop(); player.clip = null; player.source = UnityEngine.Video.VideoSource.Url; player.url = "";
            player.playOnAwake = false;
            var view = root.GetComponent<ODVideoPlayerView>() ?? root.AddComponent<ODVideoPlayerView>();
            view.pilotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PilotPath);
            view.pickerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickerPath);
            view.BuildMain(player.targetTexture);
            var bounds = root.GetComponent<BoundsClipper>();
            var rect = (RectTransform)root.transform;
            bounds.Position = new Vector3(rect.rect.center.x, rect.rect.center.y, 0);
            bounds.Size = new Vector3(1200, 675, .01f);
            var surface = root.GetComponent<ODHomepageSurface>();
            surface.glass = true; surface.cornerRadius = 36; surface.rimWidth = 1;
            surface.topLeft = surface.bottomRight = new Color(.08f, .085f, .10f, .98f);
            var outline = root.GetComponent<Outline>(); if (outline != null) outline.enabled = false;
            // Initial presentation only; dragging the TV remains completely free afterwards.
            root.GetComponent<ODCourseVideoDock>().localOffset = new Vector3(-1832, 337.5f, 0);
            root.GetComponent<ODCourseVideoDock>().initialYaw = -12;
            PrefabUtility.SaveAsPrefabAsset(root, VideoPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var course = UnityEngine.Object.FindAnyObjectByType<ODSportPage>(FindObjectsInactive.Include);
        if (course == null || course.television == null) throw new InvalidOperationException("Course controller missing.");
        var tv = course.television;
        var panel = tv.videoPanel;
        // Match every window's geometric handle to the selector belonging to that exact hand/controller ray.
        foreach (var ray in UnityEngine.Object.FindObjectsByType<RayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ray.gameObject.scene != scene) continue;
            var property = new SerializedObject(ray).FindProperty("_selector");
            var source = property != null ? property.objectReferenceValue : null;
            if (!(source is ISelector)) throw new InvalidOperationException("Ray selector missing: " + ray.name);
            var input = ray.GetComponent<ODWindowGrabInput>() ?? Undo.AddComponent<ODWindowGrabInput>(ray.gameObject);
            input.selector = source;
            EditorUtility.SetDirty(input); PrefabUtility.RecordPrefabInstancePropertyModifications(input);
        }
        // The old standalone scene instance had a size override. Updating the prefab alone cannot replace it.
        var sceneRect = (RectTransform)panel.transform;
        sceneRect.sizeDelta = new Vector2(1200, 675);
        PrefabUtility.RecordPrefabInstancePropertyModifications(sceneRect);
        var sceneBounds = panel.GetComponent<BoundsClipper>();
        sceneBounds.Position = new Vector3(600, -337.5f, 0);
        sceneBounds.Size = new Vector3(1200, 675, .01f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(sceneBounds);
        tv.videoPanel = panel;
        tv.videoPlayer = panel.GetComponent<UnityEngine.Video.VideoPlayer>();
        panel.GetComponent<ODVideoPlayerView>().Bind(tv);
        tv.scrubSlider = panel.transform.Find("Progress").GetComponent<Slider>();
        tv.timeLabel = panel.transform.Find("Time").GetComponent<TextMeshProUGUI>();
        EditorUtility.SetDirty(tv); PrefabUtility.RecordPrefabInstancePropertyModifications(tv);
        panel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        AssetDatabase.SaveAssets();
        return "Saved main TV and two independent ray/poke canvas prefabs; scene controller reconnected.";
    }

    private static void CreateAuxiliary(string path, string name, Vector2 size, bool pilot)
    {
        // Never silently overwrite a pre-existing new view prefab from another task.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        var canvas = ODPointableCanvas.Create(name, size);
        var go = canvas.gameObject;
        try
        {
            var rect = (RectTransform)go.transform;
            rect.pivot = new Vector2(0, 1);
            var bounds = go.GetComponent<BoundsClipper>();
            bounds.Position = new Vector3(rect.rect.center.x, rect.rect.center.y, 0);
            var surface = go.AddComponent<ODHomepageSurface>();
            surface.color = Color.white; surface.raycastTarget = true;
            if (pilot)
            {
                surface.glass = false; surface.topLeft = surface.bottomRight = new Color(.035f, .04f, .05f);
                surface.cornerRadius = 26;
                var handle = go.AddComponent<WindowHandle>(); handle.followTarget = rect;
                go.AddComponent<ODPilotVideoView>();
            }
            else ODChampionshipTheme.ApplyBackground(surface, ODChampionshipTheme.NeutralPalette);
            go.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
