/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODPointableCanvas - Creates a world-space canvas at runtime that the ISDK
 ## rays can click, with the same component set as the scene windows
 ## (PointableCanvas, clipped PlaneSurface, RayInteractable). Everything is
 ## injected before the object is activated, so the ISDK Start checks pass.
 ##
 */

using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;
using UnityEngine.UI;

public static class ODPointableCanvas
{
    /// <summary>
    /// New active canvas of <paramref name="size"/> canvas units, <paramref name="metresPerUnit"/>
    /// world metres per unit (0.001 = 1000 units per metre, like the other windows).
    /// </summary>
    public static Canvas Create(string name, Vector2 size, float metresPerUnit = .001f)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.SetActive(false);   // inject before OnEnable / Start run

        var rect = (RectTransform)go.transform;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one * metresPerUnit;

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;   // same text sharpness as the scene windows
        go.AddComponent<GraphicRaycaster>();

        var pointable = go.AddComponent<PointableCanvas>();
        pointable.InjectAllPointableCanvas(canvas);

        var plane = go.AddComponent<PlaneSurface>();
        plane.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Backward, false);

        var clipper = go.AddComponent<BoundsClipper>();
        clipper.Position = Vector3.zero;
        clipper.Size = new Vector3(size.x, size.y, .01f);

        var surface = go.AddComponent<ClippedPlaneSurface>();
        surface.InjectAllClippedPlaneSurface(plane, new IBoundsClipper[] { clipper });

        var ray = go.AddComponent<RayInteractable>();
        ray.InjectAllRayInteractable(surface);
        ray.InjectOptionalPointableElement(pointable);

        go.SetActive(true);
        return canvas;
    }
}
