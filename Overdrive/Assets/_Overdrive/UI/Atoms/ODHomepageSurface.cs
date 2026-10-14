using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A rounded translucent surface with the homepage's subtle blue-grey tint.
/// Diagonal gradient from topLeft to bottomRight; with useMiddle, a third stop
/// at the centre gives the championship pages' "coloured corners, dark middle"
/// look (RoundedImage fans its triangles from a centre vertex, so the middle
/// colour lands exactly there).
///
/// With glass on, the surface is drawn visionOS-style in a single mesh:
/// soft drop shadow, translucent fill with a light sheen near the top, and a
/// thin rim that is brighter on top than at the bottom. The headset's
/// passthrough can't be blurred by an app, so this is the whole effect.
/// </summary>
public sealed class ODHomepageSurface : RoundedImage
{
    public Color topLeft = new Color(.34f, .39f, .51f, .90f);
    public Color bottomRight = new Color(.17f, .18f, .20f, .90f);
    public bool useMiddle;
    public Color middle = new Color(.05f, .06f, .08f, .94f);

    [Header("Glass (visionOS)")]
    public bool glass;
    [Tooltip("White added to the fill near the top edge (0 = none).")]
    public float sheen = .08f;
    public float rimWidth = 1.5f;
    public Color rimTop = new Color(1f, 1f, 1f, .32f);
    public Color rimBottom = new Color(1f, 1f, 1f, .05f);
    [Tooltip("Soft shadow spread around the surface, in canvas units (0 = no shadow).")]
    public float shadowSize;
    public float shadowAlpha = .30f;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        if (!glass)
        {
            base.OnPopulateMesh(mesh);
            Rect area = GetPixelAdjustedRect();
            UIVertex vertex = default;
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                vertex.color = Fill(area, vertex.position) * color;
                mesh.SetUIVertex(vertex, i);
            }
            return;
        }

        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float r = Mathf.Min(cornerRadius, rect.width * .5f, rect.height * .5f);

        // 1. Shadow: a feathered ring from the surface edge outwards, nudged down (light from above).
        if (shadowSize > 0f)
        {
            Vector2 drop = new Vector2(0f, -shadowSize * .35f);
            Rect inner = new Rect(rect.position + drop, rect.size);
            Rect outer = new Rect(inner.xMin - shadowSize, inner.yMin - shadowSize, inner.width + shadowSize * 2f, inner.height + shadowSize * 2f);
            Ring(mesh, inner, r, outer, r + shadowSize, _ => new Color(0f, 0f, 0f, shadowAlpha), _ => new Color(0f, 0f, 0f, 0f));
        }

        // 2. Fill: fan from the centre so the diagonal gradient's middle stop lands there.
        //    uv0.x = 1 marks glass fill for the OD/UI Glass shader (frosted passthrough behind it).
        int centre = mesh.currentVertCount;
        AddVertex(mesh, rect.center, Fill(rect, rect.center) * color, GlassFill);
        var edge = Perimeter(rect, r);
        foreach (var p in edge) AddVertex(mesh, p, Fill(rect, p) * color, GlassFill);
        for (int i = 0; i < edge.Count; i++)
            mesh.AddTriangle(centre, centre + 1 + i, centre + 1 + (i + 1) % edge.Count);

        // 3. Rim: thin ring just inside the edge, bright on top, faint at the bottom.
        if (rimWidth > 0f)
        {
            Rect inset = new Rect(rect.xMin + rimWidth, rect.yMin + rimWidth, rect.width - rimWidth * 2f, rect.height - rimWidth * 2f);
            System.Func<Vector2, Color> rim = p => Color.Lerp(rimBottom, rimTop, Mathf.InverseLerp(rect.yMin, rect.yMax, p.y)) * color.a;
            Ring(mesh, inset, Mathf.Max(0f, r - rimWidth), rect, r, rim, rim);
        }
    }

    Color Fill(Rect rect, Vector3 position)
    {
        float x = Mathf.InverseLerp(rect.xMin, rect.xMax, position.x);
        float y = Mathf.InverseLerp(rect.yMin, rect.yMax, position.y);
        float t = (x + 1f - y) * .5f;
        Color tint = !useMiddle ? Color.Lerp(topLeft, bottomRight, t)
                   : t < .5f    ? Color.Lerp(topLeft, middle, t * 2f)
                                : Color.Lerp(middle, bottomRight, (t - .5f) * 2f);
        if (glass && sheen > 0f)
        {
            float s = sheen * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.55f, 1f, y));
            tint = new Color(tint.r + s, tint.g + s, tint.b + s, tint.a + s * .5f);
        }
        return tint;
    }

    List<Vector2> Perimeter(Rect rect, float radius)
    {
        var points = new List<Vector2>();
        Vector2[] pivots =
        {
            new Vector2(rect.xMin + radius, rect.yMin + radius),
            new Vector2(rect.xMax - radius, rect.yMin + radius),
            new Vector2(rect.xMax - radius, rect.yMax - radius),
            new Vector2(rect.xMin + radius, rect.yMax - radius),
        };
        float step = 90f / cornerSegments;
        for (int corner = 0; corner < 4; corner++)
            for (int seg = 0; seg <= cornerSegments; seg++)
            {
                float angle = (180f + corner * 90f + seg * step) * Mathf.Deg2Rad;
                points.Add(pivots[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        return points;
    }

    /// <summary>Quad strip between two rounded rects sampled at the same angles.</summary>
    void Ring(VertexHelper mesh, Rect inner, float innerRadius, Rect outer, float outerRadius,
        System.Func<Vector2, Color> innerColor, System.Func<Vector2, Color> outerColor)
    {
        var a = Perimeter(inner, innerRadius);
        var b = Perimeter(outer, outerRadius);
        int start = mesh.currentVertCount;
        for (int i = 0; i < a.Count; i++)
        {
            AddVertex(mesh, a[i], innerColor(a[i]));
            AddVertex(mesh, b[i], outerColor(b[i]));
        }
        for (int i = 0; i < a.Count; i++)
        {
            int j = (i + 1) % a.Count;
            int ai = start + i * 2, bi = ai + 1, aj = start + j * 2, bj = aj + 1;
            mesh.AddTriangle(ai, bi, bj);
            mesh.AddTriangle(ai, bj, aj);
        }
    }

    static readonly Vector2 GlassFill = new Vector2(1f, 0f);

    static void AddVertex(VertexHelper mesh, Vector2 position, Color color, Vector2 uv = default) =>
        mesh.AddVert(new Vector3(position.x, position.y, 0f), color, uv);
}
