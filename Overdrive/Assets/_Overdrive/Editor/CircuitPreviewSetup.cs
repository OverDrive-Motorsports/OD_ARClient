using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;

/// <summary>
/// One-time editor import utility. Produces ordinary saved prefabs and a scene
/// reference; there is no build button or runtime dependency on this utility.
/// </summary>
public static class CircuitPreviewSetup
{
    private const string Art = "Assets/_Overdrive/Art/Circuits";
    private const string PreviewPath = "Assets/_Overdrive/UI/Prefabs/Screens/CircuitPreview.prefab";
    private static readonly string[] Keys = { "Baku", "Jeddah", "YasMarina" };
    private static readonly string[] Labels = { "Baku", "Jeddah", "Yas Marina" };

    [Serializable] private class Materials { public MaterialData[] materials; }
    [Serializable] private class MaterialData
    {
        public string name;
        public float[] color;
        public float roughness;
        public float metallic;
    }

    public static string Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/_Overdrive/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Open SampleScene first.");
        var player = UnityEngine.Object.FindFirstObjectByType<VideoPlayerController>(FindObjectsInactive.Include);
        if (player == null) throw new InvalidOperationException("Video player not found.");
        if (UnityEngine.Object.FindFirstObjectByType<CircuitPreview>(FindObjectsInactive.Include) != null)
            throw new InvalidOperationException("Circuit preview already installed; preserve the existing instance.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPath) != null)
            throw new InvalidOperationException("Preview prefab already exists; do not overwrite it.");

        Directory.CreateDirectory(Art + "/Materials");
        Directory.CreateDirectory(Art + "/Prefabs");
        AssetDatabase.Refresh();
        var models = Keys.Select(CreateModelPrefab).ToArray();
        var root = new GameObject("CircuitPreviewCanvas", typeof(RectTransform));
        root.SetActive(false);
        try
        {
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(780, 620);
            rect.localScale = Vector3.one * .001f;
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10;
            root.AddComponent<GraphicRaycaster>();
            var handle = root.AddComponent<WindowHandle>();
            handle.idleColor = Color.white;
            handle.hoverColor = new Color(.80f, .91f, 1f, 1f);
            handle.grabColor = new Color(.65f, .84f, 1f, 1f);
            var preview = root.AddComponent<CircuitPreview>();
            preview.circuits = new CircuitPreview.Circuit[3];
            Panel(root.transform, "Glass", Vector2.zero, new Vector2(780, 620),
                new Color(.42f, .48f, .57f, .88f), 28);
            Text(root.transform, "Heading", "CIRCUITS", new Vector2(0, 275), new Vector2(700, 32), 24);
            for (int i = 0; i < Keys.Length; i++)
            {
                var graphic = Panel(root.transform, Keys[i] + "Button", new Vector2((i - 1) * 242, 216),
                    new Vector2(226, 58), Color.white, 18);
                graphic.raycastTarget = true;
                var button = graphic.gameObject.AddComponent<Button>();
                button.targetGraphic = graphic;
                var navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
                var colors = button.colors;
                colors.normalColor = new Color(.22f, .28f, .36f, 1);
                colors.highlightedColor = new Color(.56f, .67f, .79f, 1);
                colors.pressedColor = new Color(.34f, .46f, .58f, 1);
                colors.selectedColor = colors.normalColor;
                button.colors = colors;
                Text(graphic.transform, "Label", Labels[i], Vector2.zero, new Vector2(218, 50), 24);
                preview.circuits[i] = new CircuitPreview.Circuit { label = Labels[i], prefab = models[i], button = button };
            }
            var mount = new GameObject("ModelMount").transform;
            mount.SetParent(root.transform, false);
            mount.localPosition = new Vector3(0, -32, -160);
            preview.modelMount = mount;
            preview.circuitLabel = Text(root.transform, "CircuitName", "Baku", new Vector2(0, -263), new Vector2(700, 32), 24);
            Text(root.transform, "GrabHint", "Déplacer avec la barre blanche", new Vector2(0, -293), new Vector2(700, 22), 17);

            var pointable = root.AddComponent<PointableCanvas>();
            pointable.InjectCanvas(canvas);
            var plane = root.AddComponent<PlaneSurface>();
            var clipper = root.AddComponent<BoundsClipper>();
            clipper.Size = new Vector3(780, 620, 1);
            var clipped = root.AddComponent<ClippedPlaneSurface>();
            clipped.InjectAllClippedPlaneSurface(plane, new[] { clipper });
            var interactable = root.AddComponent<RayInteractable>();
            interactable.InjectSurface(clipped);
            interactable.InjectOptionalPointableElement(pointable);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PreviewPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Add circuit preview");
            float offset = ((RectTransform)player.transform).rect.width * player.transform.lossyScale.x * .5f + .39f + .1f;
            instance.transform.SetPositionAndRotation(player.transform.position - player.transform.right * offset, player.transform.rotation);
            Undo.RecordObject(player, "Wire circuit preview");
            player.circuitPreview = instance.GetComponent<CircuitPreview>();
            EditorUtility.SetDirty(player);
            if (PrefabUtility.IsPartOfPrefabInstance(player)) PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            return "Installed three circuit prefabs and independently movable preview beside the video.";
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject CreateModelPrefab(string key)
    {
        string path = Art + "/" + key + ".fbx";
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Missing imported FBX: " + key);
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.isReadable = false;
        importer.addCollider = false;
        importer.SaveAndReimport();
        var metadata = JsonUtility.FromJson<Materials>(File.ReadAllText(Art + "/" + key + ".materials.json"));
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP Lit shader missing.");
        var materials = new Dictionary<string, Material>();
        foreach (var data in metadata.materials)
        {
            string materialPath = Art + "/Materials/" + key + "_" + data.name.Replace('/', '_') + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = key + "_" + data.name };
                // Blender base colors are linear; Unity material color properties are sRGB.
                var color = new Color(data.color[0], data.color[1], data.color[2], 1).gamma;
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Metallic", data.metallic);
                material.SetFloat("_Smoothness", 1 - data.roughness);
                material.SetFloat("_Cull", 0); // Thin barriers/markings remain visible from either side.
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(data.color[0], data.color[1], data.color[2]) * .10f);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            materials[data.name] = material;
        }
        var root = new GameObject(key);
        try
        {
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
            model.name = "Geometry";
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                {
                    if (material == null || !materials.TryGetValue(material.name, out var mapped))
                        throw new InvalidOperationException("Unmapped material on " + key + "/" + renderer.name + ": " + material?.name);
                    return mapped;
                }).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
            }
            if (!CircuitPreview.TryGetMeshBounds(model.transform, root.transform, out var bounds))
                throw new InvalidOperationException("Empty circuit: " + key);
            float scale = 1f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            model.transform.localScale *= scale;
            model.transform.localPosition = -bounds.center * scale;
            return PrefabUtility.SaveAsPrefabAsset(root, Art + "/Prefabs/" + key + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static RoundedImage Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color, float radius)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var image = go.AddComponent<RoundedImage>();
        image.color = color;
        image.cornerRadius = radius;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }
}
