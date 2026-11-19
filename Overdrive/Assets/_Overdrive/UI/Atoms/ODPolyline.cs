/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODPolyline - Atom drawing a thick open line through points given in its
 ## rect's local units, relative to its pivot (telemetry traces). Each
 ## segment is a quad stretched by half the thickness at both ends, so the
 ## joins have no gaps (draw it opaque: overlaps would show when translucent).
 ##
 */

using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ODPolyline : MaskableGraphic
{
    [Tooltip("Line width, in local units.")]
    public float thickness = 3f;

    Vector2[] _points = Array.Empty<Vector2>();
    int _count;

    /// <summary>Draws the first <paramref name="count"/> points (the array is kept, not copied: refill it and call again).</summary>
    public void SetPoints(Vector2[] points, int count)
    {
        _points = points ?? Array.Empty<Vector2>();
        _count = Mathf.Clamp(count, 0, _points.Length);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var vertex = UIVertex.simpleVert;
        vertex.color = color;
        float half = thickness * .5f;
        for (int i = 0; i + 1 < _count; i++)
        {
            Vector2 a = _points[i], b = _points[i + 1], d = b - a;
            float length = d.magnitude;
            if (length < 1e-5f) continue;
            d /= length;
            Vector2 normal = new Vector2(-d.y, d.x) * half;
            a -= d * half;
            b += d * half;

            int k = vh.currentVertCount;
            vertex.position = a + normal; vh.AddVert(vertex);
            vertex.position = a - normal; vh.AddVert(vertex);
            vertex.position = b - normal; vh.AddVert(vertex);
            vertex.position = b + normal; vh.AddVert(vertex);
            vh.AddTriangle(k, k + 1, k + 2);
            vh.AddTriangle(k, k + 2, k + 3);
        }
    }
}
