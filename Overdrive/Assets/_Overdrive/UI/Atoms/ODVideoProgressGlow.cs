using UnityEngine;
using UnityEngine.UI;

/// <summary>Soft red halo and a short fading trail behind the scrubber. Geometry only; no texture or shader asset.</summary>
[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class ODVideoProgressGlow : MaskableGraphic
{
    public Slider slider;
    private float _lastDistance = -1;
    private void LateUpdate()
    {
        float distance = AvailableTrail();
        // Playback updates the slider without notifications, so observe the value rather than onValueChanged.
        if (Mathf.Abs(distance - _lastDistance) > .05f) { _lastDistance = distance; SetVerticesDirty(); }
    }
    private float AvailableTrail() => slider == null ? 0 :
        Mathf.Min(56, slider.normalizedValue * ((RectTransform)slider.transform).rect.width);

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var centre = rectTransform.rect.center;
        float trail = AvailableTrail();
        if (trail > 12)
        {
            Quad(mesh, centre + new Vector2(-trail, -5), centre + new Vector2(-12, 5), 0, .10f);
            Quad(mesh, centre + new Vector2(-trail, -2), centre + new Vector2(-12, 2), 0, .65f);
        }
        const int segments = 48;
        for (int i = 0; i < segments; i++)
        {
            float a = 2 * Mathf.PI * i / segments, b = 2 * Mathf.PI * (i + 1) / segments;
            var p = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            int n = mesh.currentVertCount;
            mesh.AddVert(centre + p * 12, Tint(.35f), Vector2.zero);
            mesh.AddVert(centre + q * 12, Tint(.35f), Vector2.zero);
            mesh.AddVert(centre + q * 21, Tint(0), Vector2.zero);
            mesh.AddVert(centre + p * 21, Tint(0), Vector2.zero);
            mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
        }
    }
    private Color Tint(float alpha) => new Color(color.r, color.g, color.b, color.a * alpha);
    private void Quad(VertexHelper mesh, Vector2 min, Vector2 max, float left, float right)
    {
        int n = mesh.currentVertCount;
        mesh.AddVert(new Vector2(min.x, min.y), Tint(left), Vector2.zero);
        mesh.AddVert(new Vector2(min.x, max.y), Tint(left), Vector2.zero);
        mesh.AddVert(new Vector2(max.x, max.y), Tint(right), Vector2.zero);
        mesh.AddVert(new Vector2(max.x, min.y), Tint(right), Vector2.zero);
        mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
    }
}
