using UnityEngine;
using UnityEngine.UI;

/// <summary>Video controls drawn as geometry: no font-dependent glyphs.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ODVideoIcon : MaskableGraphic
{
    public enum Kind { Play, Pause, Volume, Mute, Add, Swap, Close, Expand, More, Check, Map, Radio, BackTen, ForwardTen }
    public Kind kind;
    public void Set(Kind value) { if (kind == value) return; kind = value; SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper v)
    {
        v.Clear();
        switch (kind)
        {
            case Kind.Pause: Box(v, .28f, .17f, .13f, .66f); Box(v, .59f, .17f, .13f, .66f); break;
            case Kind.Play: Triangle(v, new Vector2(.3f, .16f), new Vector2(.3f, .84f), new Vector2(.83f, .5f)); break;
            case Kind.Add: Line(v, .5f, .16f, .5f, .84f); Line(v, .16f, .5f, .84f, .5f); break;
            case Kind.Close: Line(v, .23f, .23f, .77f, .77f); Line(v, .23f, .77f, .77f, .23f); break;
            case Kind.Check: Line(v, .15f, .5f, .4f, .24f); Line(v, .4f, .24f, .86f, .78f); break;
            case Kind.More:
                for (int i = 0; i < 3; i++) Box(v, .19f + .25f * i, .45f, .10f, .10f);
                break;
            case Kind.Volume:
            case Kind.Mute:
                Line(v, .13f, .38f, .13f, .62f); Line(v, .13f, .62f, .32f, .62f);
                Line(v, .32f, .62f, .52f, .81f); Line(v, .52f, .81f, .52f, .19f);
                Line(v, .52f, .19f, .32f, .38f); Line(v, .32f, .38f, .13f, .38f);
                if (kind == Kind.Mute) { Line(v, .68f, .35f, .92f, .65f); Line(v, .68f, .65f, .92f, .35f); }
                else { Arc(v, new Vector2(.48f, .5f), .28f, -55, 55); Arc(v, new Vector2(.48f, .5f), .43f, -55, 55); }
                break;
            case Kind.Swap:
                Line(v, .15f, .67f, .85f, .67f); Line(v, .15f, .67f, .36f, .88f); Line(v, .15f, .67f, .36f, .46f);
                Line(v, .85f, .3f, .15f, .3f); Line(v, .85f, .3f, .64f, .51f); Line(v, .85f, .3f, .64f, .09f); break;
            case Kind.Expand:
                for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++)
                {
                    float a = x == 0 ? .17f : .83f, b = y == 0 ? .17f : .83f;
                    Line(v, a, b, a + (x == 0 ? .22f : -.22f), b); Line(v, a, b, a, b + (y == 0 ? .22f : -.22f));
                }
                break;
            case Kind.Map:
                Line(v, .12f, .2f, .12f, .74f); Line(v, .12f, .74f, .37f, .88f); Line(v, .37f, .88f, .63f, .74f);
                Line(v, .63f, .74f, .88f, .88f); Line(v, .88f, .88f, .88f, .3f); Line(v, .88f, .3f, .63f, .16f);
                Line(v, .63f, .16f, .37f, .3f); Line(v, .37f, .3f, .12f, .2f); Line(v, .37f, .3f, .37f, .88f); Line(v, .63f, .16f, .63f, .74f); break;
            case Kind.Radio:
                for (int i = 0; i < 5; i++) { float h = i == 2 ? .7f : i % 2 == 0 ? .24f : .46f; Line(v, .18f + .16f * i, .5f - h / 2, .18f + .16f * i, .5f + h / 2); }
                break;
            case Kind.BackTen:
            case Kind.ForwardTen:
                Arc(v, new Vector2(.5f, .5f), .34f, 35, 320);
                if (kind == Kind.BackTen) { Line(v, .28f, .83f, .2f, .58f); Line(v, .2f, .58f, .46f, .62f); }
                else { Line(v, .72f, .83f, .8f, .58f); Line(v, .8f, .58f, .54f, .62f); }
                break;
        }
    }

    private Vector2 Point(Vector2 p) => rectTransform.rect.min + Vector2.Scale(p, rectTransform.rect.size);
    private void Triangle(VertexHelper v, Vector2 a, Vector2 b, Vector2 c)
    { int i = v.currentVertCount; v.AddVert(Point(a), color, Vector2.zero); v.AddVert(Point(b), color, Vector2.zero); v.AddVert(Point(c), color, Vector2.zero); v.AddTriangle(i, i + 1, i + 2); }
    private void Box(VertexHelper v, float x, float y, float w, float h)
    { Triangle(v, new Vector2(x, y), new Vector2(x, y + h), new Vector2(x + w, y + h)); Triangle(v, new Vector2(x, y), new Vector2(x + w, y + h), new Vector2(x + w, y)); }
    private void Line(VertexHelper v, float x, float y, float xx, float yy)
    {
        Vector2 a = Point(new Vector2(x, y)), b = Point(new Vector2(xx, yy));
        Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .035f;
        int i = v.currentVertCount;
        foreach (var p in new[] { a - n, a + n, b + n, b - n }) v.AddVert(p, color, Vector2.zero);
        v.AddTriangle(i, i + 1, i + 2); v.AddTriangle(i, i + 2, i + 3);
    }
    private void Arc(VertexHelper v, Vector2 center, float radius, float begin, float end)
    {
        for (int i = 0; i < 24; i++)
        {
            float a = Mathf.Lerp(begin, end, i / 24f) * Mathf.Deg2Rad, b = Mathf.Lerp(begin, end, (i + 1) / 24f) * Mathf.Deg2Rad;
            Line(v, center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, center.x + Mathf.Cos(b) * radius, center.y + Mathf.Sin(b) * radius);
        }
    }
}
