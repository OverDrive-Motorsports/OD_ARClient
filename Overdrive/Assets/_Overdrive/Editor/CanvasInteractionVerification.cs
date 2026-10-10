using System;
using System.Collections.Generic;
using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Play-mode smoke test with synthetic poses, never hardware or saved scene changes.</summary>
public static class CanvasInteractionVerification
{
    private sealed class TestController : IController
    {
        public bool pressed;
        public Handedness Handedness => Handedness.Right;
        public float Scale => 1;
        public bool IsConnected => true;
        public bool IsPoseValid => true;
        public ControllerInput ControllerInput => default(ControllerInput);
        public bool TryGetPose(out Pose pose) { pose = Pose.identity; return true; }
        public bool TryGetPointerPose(out Pose pose) { pose = Pose.identity; return true; }
        public bool IsButtonUsageAnyActive(ControllerButtonUsage usage) => pressed && (usage & ControllerButtonUsage.TriggerButton) != 0;
        public bool IsButtonUsageAllActive(ControllerButtonUsage usage) => IsButtonUsageAnyActive(usage);
        public event Action WhenUpdated { add { } remove { } }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Lifecycle(Component c, string method)
    {
        c.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, null);
    }

    private static void PrepareGraphics(Canvas canvas, Vector3 center)
    {
        // A newly created Graphic has depth -1 until Unity renders the canvas.
        // Render once so this synchronous test can use real GraphicRaycaster hits.
        var cameraGO = new GameObject("TemporaryCanvasTestCamera", typeof(Camera));
        var render = new RenderTexture(64, 64, 16);
        var previous = canvas.worldCamera;
        try
        {
            var camera = cameraGO.GetComponent<Camera>();
            camera.enabled = false;
            camera.transform.SetPositionAndRotation(center - canvas.transform.forward, canvas.transform.rotation);
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 1.1f;
            camera.targetTexture = render;
            canvas.worldCamera = camera;
            camera.Render();
        }
        finally
        {
            canvas.worldCamera = previous;
            UnityEngine.Object.DestroyImmediate(cameraGO);
            render.Release();
            UnityEngine.Object.DestroyImmediate(render);
        }
    }

    public static string Run()
    {
        Check(EditorApplication.isPlaying, "Run only in Play mode.");
        var module = UnityEngine.Object.FindFirstObjectByType<PointableCanvasModule>();
        Check(module != null, "Canvas input module missing.");
        var results = new List<string>();
        foreach (var name in new[] { "OverdriveMenuCanvas", "OverdriveNavigationCanvas", "VideoPlayerCanvas", "CircuitPreview" })
        {
            var pointable = UnityEngine.Object.FindObjectsByType<PointableCanvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var target = Array.Find(pointable, p => p.Canvas != null && p.Canvas.name == name && p.gameObject.name != "ISDK_RayCanvasInteraction" && name != "VideoPlayerCanvas");
            if (name == "VideoPlayerCanvas") target = Array.Find(pointable, p => p.Canvas != null && p.Canvas.name == name);
            Check(target != null, "Missing bridge: " + name);
            Check(target.GetComponents<PokeInteractable>().Length == 1, "Duplicate poke: " + name);
            var canvas = target.Canvas;
            bool wasActive = canvas.gameObject.activeSelf;
            var oldPosition = canvas.transform.position;
            var group = canvas.GetComponent<CanvasGroup>();
            float oldAlpha = group != null ? group.alpha : 1;
            var fixture = new GameObject("TemporaryInteractionTest");
            var buttonGO = new GameObject("TemporaryTestButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGO.GetComponent<LayoutElement>().ignoreLayout = true;
            int clicks = 0;
            var trace = new List<string>();
            Action<PointableCanvasModule.Pointer> watch = pointer => pointer.WhenUpdated += data => trace.Add("hit=" + (data.pointerCurrentRaycast.gameObject != null ? data.pointerCurrentRaycast.gameObject.name : "none"));
            PointableCanvasModule.WhenPointerStarted += watch;
            try
            {
                canvas.gameObject.SetActive(true);
                // Isolate each window spatially: the edit-time wrist placeholder
                // overlaps the menu until a real tracked wrist pose is available.
                canvas.transform.position = new Vector3(20, 1, 2);
                if (group != null) group.alpha = 1;
                canvas.worldCamera = Camera.main;
                Lifecycle(target, "Start");
                Lifecycle(target.GetComponent<RayInteractable>(), "Start");
                Lifecycle(target.GetComponent<PokeInteractable>(), "Start");
                buttonGO.transform.SetParent(canvas.transform, false);
                var rect = (RectTransform)buttonGO.transform;
                rect.sizeDelta = new Vector2(80, 40);
                rect.anchoredPosition = Vector2.zero;
                buttonGO.transform.SetAsLastSibling();
                buttonGO.GetComponent<Button>().onClick.AddListener(() => clicks++);
                Canvas.ForceUpdateCanvases();
                buttonGO.GetComponent<Image>().Rebuild(CanvasUpdate.PreRender);
                var center = rect.TransformPoint(rect.rect.center);
                PrepareGraphics(canvas, center);

                var selector = fixture.AddComponent<ControllerSelector>();
                var controller = new TestController();
                selector.InjectController(controller);
                selector.ControllerButtonUsage = ControllerButtonUsage.TriggerButton;
                Lifecycle(selector, "Start");
                var ray = fixture.AddComponent<RayInteractor>();
                ray.InjectAllRayInteractor(selector, fixture.transform);
                ray.IsRootDriver = false;
                fixture.transform.SetPositionAndRotation(center - canvas.transform.forward * .1f, canvas.transform.rotation);
                Lifecycle(ray, "Start");
                for (int i = 0; i < 3; i++) { ray.Drive(); module.Process(); }
                Check(ray.Interactable == target.GetComponent<RayInteractable>(), "Ray hit another surface: " + name + " actual=" + (ray.Interactable != null ? ray.Interactable.name : "none"));
                controller.pressed = true;
                Lifecycle(selector, "Update");
                ray.Drive(); module.Process();
                trace.Add("press=" + ray.State);
                controller.pressed = false;
                Lifecycle(selector, "Update");
                ray.Drive(); module.Process();
                Check(clicks == 1, "Controller trigger click failed: " + name + " clicks=" + clicks + " depth=" + buttonGO.GetComponent<Image>().depth + " cull=" + buttonGO.GetComponent<Image>().canvasRenderer.cull + " " + string.Join(",", trace));
                ray.Disable(); module.Process();
                UnityEngine.Object.DestroyImmediate(ray);

                var poke = fixture.AddComponent<PokeInteractor>();
                poke.InjectAllPokeInteractor(fixture.transform);
                poke.IsRootDriver = false;
                float time = 0;
                poke.SetTimeProvider(() => time);
                Lifecycle(poke, "Start");
                foreach (float distance in new[] { .1f, .05f, .02f, .008f, .002f, -.008f, -.008f, .01f, .04f, .15f, .25f })
                {
                    time += 1f / 60f;
                    fixture.transform.position = center - canvas.transform.forward * distance;
                    poke.Drive(); module.Process();
                }
                Check(clicks == 2, "Finger contact/release click failed: " + name + " clicks=" + clicks);
                poke.Disable(); module.Process();
                results.Add(name + ": trigger ray + fingertip touch each produced one click");
            }
            finally
            {
                PointableCanvasModule.WhenPointerStarted -= watch;
                UnityEngine.Object.DestroyImmediate(fixture);
                module.Process();
                UnityEngine.Object.DestroyImmediate(buttonGO);
                canvas.gameObject.SetActive(wasActive);
                canvas.transform.position = oldPosition;
                if (group != null) group.alpha = oldAlpha;
            }
        }
        return "PASS: " + string.Join("; ", results);
    }
}
