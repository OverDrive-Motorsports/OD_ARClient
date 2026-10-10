using UnityEngine;
using UnityEngine.UI;

/// <summary>A rounded translucent surface with the homepage's subtle blue-grey tint.</summary>
public sealed class ODHomepageSurface : RoundedImage
{
    public Color topLeft = new Color(.34f, .39f, .51f, .90f);
    public Color bottomRight = new Color(.17f, .18f, .20f, .90f);

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        base.OnPopulateMesh(mesh);
        Rect rect = GetPixelAdjustedRect();
        UIVertex vertex = default;
        for (int i = 0; i < mesh.currentVertCount; i++)
        {
            mesh.PopulateUIVertex(ref vertex, i);
            float x = Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x);
            float y = Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y);
            vertex.color = Color.Lerp(topLeft, bottomRight, (x + 1f - y) * .5f) * color;
            mesh.SetUIVertex(vertex, i);
        }
    }
}
