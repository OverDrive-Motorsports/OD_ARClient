using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drop-in replacement for Image that draws a rounded rectangle mesh.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class RoundedImage : Image
{
    [Range(0f, 300f)]
    public float cornerRadius = 24f;

    [Range(3, 32)]
    public int cornerSegments = 12;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float r = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
        Color32 c32 = color;

        // Center vertex
        vh.AddVert(new Vector3(rect.center.x, rect.center.y, 0f), c32, Vector2.zero);

        // 4 corner pivot points (inner corners of rounded rect)
        Vector2[] pivots = {
            new Vector2(rect.xMin + r, rect.yMin + r), // Bottom-Left
            new Vector2(rect.xMax - r, rect.yMin + r), // Bottom-Right
            new Vector2(rect.xMax - r, rect.yMax - r), // Top-Right
            new Vector2(rect.xMin + r, rect.yMax - r), // Top-Left
        };

        float step = 90f / cornerSegments;

        for (int corner = 0; corner < 4; corner++)
        {
            float startAngle = 180f + corner * 90f;
            for (int seg = 0; seg <= cornerSegments; seg++)
            {
                float angle = (startAngle + seg * step) * Mathf.Deg2Rad;
                float x = pivots[corner].x + r * Mathf.Cos(angle);
                float y = pivots[corner].y + r * Mathf.Sin(angle);
                vh.AddVert(new Vector3(x, y, 0f), c32, Vector2.zero);
            }
        }

        int totalEdge = 4 * (cornerSegments + 1);
        for (int i = 0; i < totalEdge; i++)
        {
            int curr = 1 + i;
            int next = 1 + (i + 1) % totalEdge;
            vh.AddTriangle(0, curr, next);
        }
    }
}
