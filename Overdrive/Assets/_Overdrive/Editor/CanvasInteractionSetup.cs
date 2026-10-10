using System;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One-time authoring repair; does not generate UI or run at startup.</summary>
public static class CanvasInteractionSetup
{
    public static string Install()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/_Overdrive/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Open SampleScene in Edit mode first.");
        const string backup = "Assets/_Overdrive/Scenes/SampleScene_BeforeTouchInteraction.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(backup) == null && !EditorSceneManager.SaveScene(scene, backup, true))
            throw new InvalidOperationException("Cannot save the interaction snapshot.");

        var manager = UnityEngine.Object.FindFirstObjectByType<OVRManager>(FindObjectsInactive.Include);
        if (manager == null) throw new InvalidOperationException("Tracking manager missing.");
        Undo.RecordObject(manager, "Enable hands and controllers together");
        manager.SimultaneousHandsAndControllersEnabled = true;
        manager.launchSimultaneousHandsControllersOnStartup = true;
        EditorUtility.SetDirty(manager);
        PrefabUtility.RecordPrefabInstancePropertyModifications(manager);

        var menu = UnityEngine.Object.FindFirstObjectByType<OverdriveMainMenu>(FindObjectsInactive.Include);
        var canvas = menu.GetComponentInParent<Canvas>();
        var pointable = canvas.GetComponent<PointableCanvas>();
        if (pointable == null) throw new InvalidOperationException("Main canvas bridge missing.");
        // The old child bridge and the new root bridge occupied the same plane.
        // Keep one pointer lifecycle per canvas; retain the old object for recovery.
        foreach (var duplicate in canvas.GetComponentsInChildren<PointableCanvas>(true))
        {
            if (duplicate == pointable || duplicate.Canvas != canvas) continue;
            Undo.RecordObject(duplicate.gameObject, "Disable duplicate canvas interaction");
            duplicate.gameObject.SetActive(false);
        }
        var surface = canvas.GetComponent<ClippedPlaneSurface>();
        var clipper = GetOrAdd<BoundsClipper>(menu.gameObject);
        Fit(clipper, (RectTransform)menu.transform);
        var driver = GetOrAdd<RectTransformBoundsClipperDriver>(menu.gameObject);
        BindDriver(driver, clipper);
        Undo.RecordObject(surface, "Limit interaction to the visible menu");
        surface.InjectClippers(new[] { clipper });
        EditorUtility.SetDirty(surface);
        AddPoke(pointable, surface);

        // Upgrade prefab assets before their scene instances to avoid adding the
        // same component once as a prefab component and again as an override.
        UpdatePrefab("Assets/_Overdrive/UI/Prefabs/Screens/FloatingNavigation.prefab");
        UpdatePrefab("Assets/_Overdrive/UI/Prefabs/Screens/CircuitPreview.prefab");
        foreach (var other in UnityEngine.Object.FindObjectsByType<PointableCanvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (other == pointable || other.Canvas == canvas || other.gameObject.scene != scene) continue;
            var otherSurface = other.GetComponent<ClippedPlaneSurface>();
            if (otherSurface == null) otherSurface = other.GetComponentInChildren<ClippedPlaneSurface>(true);
            if (otherSurface == null) throw new InvalidOperationException("Missing surface: " + other.name);
            AddPoke(other, otherSurface);
            other.Canvas.worldCamera = Camera.main;
            EditorUtility.SetDirty(other.Canvas);
        }
        AssetDatabase.SaveAssets();
        Canvas.ForceUpdateCanvases();
        var navigation = UnityEngine.Object.FindFirstObjectByType<FloatingNavigation>(FindObjectsInactive.Include);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)navigation.transform);
        foreach (var rect in navigation.GetComponentsInChildren<RectTransform>(true))
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        return "Saved one main-menu bridge and ray + direct touch on main menu, wrist, video player and circuit selector.";
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(go);
    }

    private static void Fit(BoundsClipper clipper, RectTransform rect)
    {
        Undo.RecordObject(clipper, "Fit canvas interaction bounds");
        clipper.Position = new Vector3(rect.rect.center.x, rect.rect.center.y, 0);
        clipper.Size = new Vector3(rect.rect.width, rect.rect.height, .01f);
        EditorUtility.SetDirty(clipper);
    }

    private static void BindDriver(RectTransformBoundsClipperDriver driver, BoundsClipper clipper)
    {
        var serialized = new SerializedObject(driver);
        serialized.FindProperty("_boundsClipper").objectReferenceValue = clipper;
        serialized.ApplyModifiedProperties();
    }

    private static void AddPoke(PointableCanvas pointable, ClippedPlaneSurface surface)
    {
        var poke = GetOrAdd<PokeInteractable>(pointable.gameObject);
        Undo.RecordObject(poke, "Enable direct canvas touch");
        poke.InjectAllPokeInteractable(surface);
        poke.InjectOptionalPointableElement(pointable);
        EditorUtility.SetDirty(poke);
        PrefabUtility.RecordPrefabInstancePropertyModifications(poke);
    }

    private static void UpdatePrefab(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var pointable = root.GetComponent<PointableCanvas>();
            var surface = root.GetComponent<ClippedPlaneSurface>();
            if (pointable == null || surface == null) throw new InvalidOperationException("Prefab bridge missing: " + path);
            AddPoke(pointable, surface);
            var clipper = root.GetComponent<BoundsClipper>();
            Fit(clipper, (RectTransform)root.transform);
            BindDriver(GetOrAdd<RectTransformBoundsClipperDriver>(root), clipper);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
