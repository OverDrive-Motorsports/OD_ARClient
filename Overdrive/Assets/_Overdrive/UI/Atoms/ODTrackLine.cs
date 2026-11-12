/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTrackLine - Atom drawing a thick closed line through points given in
 ## its rect's local units, relative to its pivot (the replay map's circuit).
 ##
 */

using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ODTrackLine : MaskableGraphic
{
    [Tooltip("Line width, in local units.")]
    public float thickness = 4f;

    Vector2[] _points = Array.Empty<Vector2>();

    public void SetPoints(Vector2[] points)
    {
        _points = points ?? Array.Empty<Vector2>();
        SetVerticesDirty();
    }

    /// <summary>Two vertices per point, offset along the normal of the line through its neighbours.</summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        int n = _points.Length;
        if (n < 3) return;

        var vertex = UIVertex.simpleVert;
        vertex.color = color;
        float half = thickness * .5f;
        for (int i = 0; i < n; i++)
        {
            Vector2 direction = (_points[(i + 1) % n] - _points[(i - 1 + n) % n]).normalized;
            if (direction == Vector2.zero) direction = Vector2.right;
            Vector2 normal = new Vector2(-direction.y, direction.x) * half;
            vertex.position = _points[i] + normal;
            vh.AddVert(vertex);
            vertex.position = _points[i] - normal;
            vh.AddVert(vertex);
        }
        for (int i = 0; i < n; i++)
        {
            int a = i * 2, b = (i + 1) % n * 2;
            vh.AddTriangle(a, a + 1, b);
            vh.AddTriangle(b, a + 1, b + 1);
        }
    }
}
