using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Isolated checks for course map access and freely movable television.</summary>
public static class CircuitAccessVerification
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static string RunAndCapture()
    {
        Check(!EditorApplication.isPlaying, "Exit Play mode first.");
        var source = UnityEngine.Object.FindAnyObjectByType<OverdriveMainMenu>(FindObjectsInactive.Include);
        var sourceSport = source.sportPanel.GetComponent<ODSportPage>();
        var preview = EditorSceneManager.NewPreviewScene();
        var randomState = UnityEngine.Random.state;
        try
        {
            var root = new GameObject("CircuitAccessPreview", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(root, preview);
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)root.transform).sizeDelta = new Vector2(1200, 880);
            root.transform.localScale = Vector3.one * .001f;
            var clone = UnityEngine.Object.Instantiate(source.gameObject, root.transform, false);
            var menu = clone.GetComponent<OverdriveMainMenu>();
            var sport = menu.sportPanel.GetComponent<ODSportPage>();
            var selector = UnityEngine.Object.Instantiate(sourceSport.circuitPreview);
            SceneManager.MoveGameObjectToScene(selector.gameObject, preview);
            var table = UnityEngine.Object.Instantiate(sourceSport.circuitPreview.circuitRoot.gameObject);
            SceneManager.MoveGameObjectToScene(table, preview);
            selector.modelMount = table.transform.Find("ModelMount");
            selector.circuitRoot = table.transform;
            // The label may belong to either independent window.
            var originalLabel = sourceSport.circuitPreview.circuitLabel;
            if (originalLabel != null && originalLabel.transform.IsChildOf(sourceSport.circuitPreview.circuitRoot))
                selector.circuitLabel = table.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == originalLabel.name);
            sport.circuitPreview = selector;
            var video = UnityEngine.Object.Instantiate(sourceSport.television.videoPanel);
            SceneManager.MoveGameObjectToScene(video, preview);
            var dock = video.GetComponent<ODCourseVideoDock>();
            dock.courseWindow = (RectTransform)menu.transform;
            CourseMediaSetup.ConnectPanel(sport.television, video);
            sport.Initialize();
            selector.Initialize();
            menu.ShowSport();
            Check(sport.circuitButtons.Length == 3 && selector.circuits.Length == 4, "Missing circuit cards/maps.");
            Check(selector.modelMount != null && selector.modelMount.IsChildOf(table.transform), "Model not independent of selector.");
            Check(selector.viewingAngles == Vector3.zero && table.GetComponent<WindowHandle>() != null && selector.GetComponent<WindowHandle>() != null,
                "Flat movable tabletop/selector not preserved.");
            foreach (var mode in new[] { ODSportPage.Mode.Weekend, ODSportPage.Mode.LiveSession, ODSportPage.Mode.Replay })
            {
                sport.SetMode(mode);
                var button = sport.circuitButtons.First(b => b.gameObject.activeInHierarchy);
                Check(button.targetGraphic.raycastTarget, "Circuit card cannot receive input.");
                button.onClick.Invoke();
                Check(selector.gameObject.activeSelf && table.activeSelf && selector.CurrentModel != null, "Circuit click failed.");
                var selectorCenter = selector.transform.TransformPoint(((RectTransform)selector.transform).rect.center);
                var courseCenter = menu.transform.TransformPoint(((RectTransform)menu.transform).rect.center);
                Check(Vector3.Dot(selectorCenter - courseCenter, menu.transform.right) > .9f, "Selector overlaps course/TV.");
                button.onClick.Invoke();
                Check(!selector.gameObject.activeSelf, "Circuit toggle did not close selector.");
                // Edit-mode previews do not invoke MonoBehaviour.OnDisable automatically.
                typeof(CircuitPreview).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(selector, null);
            }
            sport.circuitButtons.Last().onClick.Invoke();
            for (int i = 0; i < selector.circuits.Length; i++)
            {
                // Runtime Destroy is deferred; preview objects are cleaned up synchronously instead.
                if (selector.CurrentModel != null) UnityEngine.Object.DestroyImmediate(selector.CurrentModel);
                selector.circuits[i].button.onClick.Invoke();
                Check(selector.SelectedIndex == i && selector.CurrentModel != null, "Map button failed: " + selector.circuits[i].label);
                Bounds bounds;
                Check(CircuitPreview.TryGetMeshBounds(selector.CurrentModel.transform, selector.modelMount, out bounds), "Missing map mesh.");
                Check(bounds.size.x <= selector.modelSpace.x + .1f && bounds.size.y <= selector.modelSpace.y + .1f && bounds.size.z <= selector.modelSpace.z + .1f,
                    "Oversized map: " + selector.circuits[i].label);
                Check(Mathf.Abs(bounds.min.y) < .1f && selector.CurrentModel.transform.localRotation == Quaternion.identity, "Map not flat on tabletop.");
            }
            var tv = sport.television;
            tv.tvButtons.Last().onClick.Invoke();
            Check(tv.IsVisible && tv.standingsPanel.activeSelf, "Replay media/regression.");
            var handle = video.GetComponent<WindowHandle>();
            var videoRect = ((RectTransform)video.transform).rect;
            Check(handle != null && handle.customPillPosition && Mathf.Abs(handle.pillLocalPosition.x - videoRect.center.x) < .1f && handle.pillLocalPosition.y < videoRect.yMin,
                "TV grab bar is not below the window.");
            var position = video.transform.position + new Vector3(.13f, .09f, -.07f);
            var rotation = video.transform.rotation * Quaternion.Euler(0, 12, 0);
            video.transform.SetPositionAndRotation(position, rotation);
            for (int i = 0; i < 10; i++) dock.PlaceIfNeeded();
            Check(Vector3.Distance(video.transform.position, position) < .001f && Quaternion.Angle(video.transform.rotation, rotation) < .01f, "Dock fights dragging.");
            tv.tvButtons.Last().onClick.Invoke();
            tv.tvButtons.Last().onClick.Invoke();
            Check(Vector3.Distance(video.transform.position, position) < .001f, "TV reopening loses moved position.");
            Check(typeof(ODCourseVideoDock).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance) == null, "Continuous docking still active.");
            // Keep all three windows in their initial presentation pose for the capture.
            dock.Align();
            Capture(preview, menu.transform);
            typeof(ODSportPage).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(sport, null);
            typeof(CircuitPreview).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(selector, null);
            Check(!selector.gameObject.activeSelf && !table.activeSelf, "Course exit leaves maps visible.");
            return "PASS: three clickable circuit cards, all four map buttons, flat independent tabletop and size bounds, separate selector/TV, TV grab bar placement, free translation/rotation and pose retained on reopening, course exit cleanup. Capture: /private/tmp/overdrive-course-circuits.png";
        }
        finally
        {
            UnityEngine.Random.state = randomState;
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static void Capture(Scene scene, Transform menu)
    {
        var go = new GameObject("CircuitAccessCamera", typeof(Camera));
        SceneManager.MoveGameObjectToScene(go, scene);
        var camera = go.GetComponent<Camera>();
        camera.enabled = false;
        camera.scene = scene;
        camera.orthographic = true;
        camera.orthographicSize = .75f;
        camera.nearClipPlane = .01f;
        camera.farClipPlane = 10;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.20f, .22f, .25f);
        camera.transform.SetPositionAndRotation(menu.TransformPoint(new Vector3(-160, -100, -2000)), menu.rotation);
        var roots = scene.GetRootGameObjects();
        foreach (var canvas in roots.SelectMany(x => x.GetComponentsInChildren<Canvas>(true))) canvas.worldCamera = camera;
        Canvas.ForceUpdateCanvases();
        foreach (var label in roots.SelectMany(x => x.GetComponentsInChildren<TextMeshProUGUI>())) label.ForceMeshUpdate(true);
        foreach (var graphic in roots.SelectMany(x => x.GetComponentsInChildren<Graphic>())) graphic.Rebuild(CanvasUpdate.PreRender);
        Canvas.ForceUpdateCanvases();
        foreach (var mask in roots.SelectMany(x => x.GetComponentsInChildren<RectMask2D>())) mask.PerformClipping();
        var render = new RenderTexture(2800, 1400, 24);
        var previous = RenderTexture.active;
        var pixels = new Texture2D(2800, 1400, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = render;
            camera.Render();
            RenderTexture.active = render;
            pixels.ReadPixels(new Rect(0, 0, 2800, 1400), 0, 0);
            pixels.Apply();
            File.WriteAllBytes("/private/tmp/overdrive-course-circuits.png", pixels.EncodeToPNG());
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
