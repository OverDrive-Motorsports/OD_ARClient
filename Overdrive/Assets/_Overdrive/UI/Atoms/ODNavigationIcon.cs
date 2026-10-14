using UnityEngine;
using UnityEngine.UI;

/// <summary>Font-independent line icons for the primary navigation.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ODNavigationIcon : MaskableGraphic
{
    // New symbols go at the end: prefabs serialize the enum as an index.
    public enum Symbol { Races, Rankings, Profile, Settings, Homepage, Categories, Star, Search, Back, Calendar, Trophy }
    public Symbol symbol;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        switch (symbol)
        {
            case Symbol.Calendar:
                Outline(mesh, .14f, .14f, .72f, .64f);
                Line(mesh, .14f, .62f, .86f, .62f);
                Line(mesh, .34f, .70f, .34f, .88f);
                Line(mesh, .66f, .70f, .66f, .88f);
                for (int x = 0; x < 3; x++)
                    for (int y = 0; y < 2; y++)
                        Box(mesh, .27f + x * .18f, .24f + y * .17f, .09f, .08f);
                break;
            case Symbol.Trophy:
                Line(mesh, .28f, .86f, .72f, .86f);
                Ring(mesh, new Vector2(.5f, .62f), .22f, 180, 360);
                Line(mesh, .28f, .86f, .28f, .62f);
                Line(mesh, .72f, .86f, .72f, .62f);
                Ring(mesh, new Vector2(.24f, .70f), .09f, 90, 270);
                Ring(mesh, new Vector2(.76f, .70f), .09f, -90, 90);
                Line(mesh, .5f, .40f, .5f, .24f);
                Line(mesh, .32f, .14f, .68f, .14f);
                Line(mesh, .38f, .24f, .62f, .24f);
                Line(mesh, .38f, .24f, .32f, .14f);
                Line(mesh, .62f, .24f, .68f, .14f);
                break;
            case Symbol.Categories:
                Outline(mesh, .13f, .55f, .30f, .30f);
                Outline(mesh, .57f, .55f, .30f, .30f);
                Outline(mesh, .13f, .11f, .30f, .30f);
                Outline(mesh, .57f, .11f, .30f, .30f);
                break;
            case Symbol.Star:
                for (int i = 0; i < 10; i++)
                {
                    float a = (90f + i * 36f) * Mathf.Deg2Rad;
                    float b = (90f + (i + 1) * 36f) * Mathf.Deg2Rad;
                    float ra = i % 2 == 0 ? .40f : .19f;
                    float rb = (i + 1) % 2 == 0 ? .40f : .19f;
                    Line(mesh, .5f + Mathf.Cos(a) * ra, .5f + Mathf.Sin(a) * ra,
                        .5f + Mathf.Cos(b) * rb, .5f + Mathf.Sin(b) * rb);
                }
                break;
            case Symbol.Search:
                Ring(mesh, new Vector2(.43f, .57f), .27f, 0, 360);
                Line(mesh, .63f, .37f, .87f, .13f);
                break;
            case Symbol.Back:
                Line(mesh, .18f, .5f, .82f, .5f);
                Line(mesh, .18f, .5f, .48f, .8f);
                Line(mesh, .18f, .5f, .48f, .2f);
                break;
            case Symbol.Homepage:
                Line(mesh, .10f, .52f, .50f, .88f);
                Line(mesh, .50f, .88f, .90f, .52f);
                Line(mesh, .23f, .56f, .23f, .15f);
                Line(mesh, .23f, .15f, .77f, .15f);
                Line(mesh, .77f, .15f, .77f, .56f);
                Line(mesh, .42f, .15f, .42f, .40f);
                Line(mesh, .42f, .40f, .58f, .40f);
                Line(mesh, .58f, .40f, .58f, .15f);
                break;
            case Symbol.Races:
                Line(mesh, .23f, .12f, .23f, .88f);
                Line(mesh, .23f, .83f, .81f, .83f);
                Line(mesh, .81f, .83f, .81f, .43f);
                Line(mesh, .81f, .43f, .23f, .43f);
                for (int x = 0; x < 4; x++)
                    for (int y = 0; y < 3; y++)
                        if ((x + y) % 2 == 0)
                            Box(mesh, .24f + x * .14f, .44f + y * .125f, .14f, .125f);
                break;
            case Symbol.Rankings:
                Outline(mesh, .12f, .18f, .24f, .38f);
                Outline(mesh, .38f, .18f, .24f, .66f);
                Outline(mesh, .64f, .18f, .24f, .24f);
                break;
            case Symbol.Profile:
                Ring(mesh, new Vector2(.5f, .70f), .17f, 0, 360);
                Ring(mesh, new Vector2(.5f, .16f), .31f, 0, 180);
                Line(mesh, .19f, .16f, .81f, .16f);
                break;
            case Symbol.Settings:
                for (int i = 0; i < 3; i++)
                {
                    float y = .25f + i * .25f;
                    float x = i == 1 ? .65f : .35f;
                    Line(mesh, .14f, y, x - .10f, y);
                    Line(mesh, x + .10f, y, .86f, y);
                    Ring(mesh, new Vector2(x, y), .10f, 0, 360);
                }
                break;
        }
    }

    private Vector2 Point(Vector2 p)
    {
        Rect r = GetPixelAdjustedRect();
        float size = Mathf.Min(r.width, r.height);
        return r.center + (p - Vector2.one * .5f) * size;
    }

    private void Quad(VertexHelper m, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        int start = m.currentVertCount;
        m.AddVert(Point(a), color, Vector2.zero);
        m.AddVert(Point(b), color, Vector2.zero);
        m.AddVert(Point(c), color, Vector2.zero);
        m.AddVert(Point(d), color, Vector2.zero);
        m.AddTriangle(start, start + 1, start + 2);
        m.AddTriangle(start + 2, start + 3, start);
    }

    private void Line(VertexHelper m, float x1, float y1, float x2, float y2)
    {
        Vector2 a = new Vector2(x1, y1), b = new Vector2(x2, y2);
        Vector2 v = (b - a).normalized;
        Vector2 n = new Vector2(-v.y, v.x) * .027f;
        Quad(m, a - n, a + n, b + n, b - n);
    }

    private void Box(VertexHelper m, float x, float y, float w, float h)
    {
        Quad(m, new Vector2(x, y), new Vector2(x, y+h), new Vector2(x+w, y+h), new Vector2(x+w, y));
    }

    private void Outline(VertexHelper m, float x, float y, float w, float h)
    {
        Line(m, x, y, x, y+h); Line(m, x, y+h, x+w, y+h);
        Line(m, x+w, y+h, x+w, y); Line(m, x+w, y, x, y);
    }

    private void Ring(VertexHelper m, Vector2 c, float r, float from, float to)
    {
        int steps = Mathf.CeilToInt((to - from) / 12f);
        for (int i = 0; i < steps; i++)
        {
            float a = Mathf.Lerp(from, to, (float)i / steps) * Mathf.Deg2Rad;
            float b = Mathf.Lerp(from, to, (float)(i+1) / steps) * Mathf.Deg2Rad;
            Line(m, c.x + Mathf.Cos(a)*r, c.y + Mathf.Sin(a)*r, c.x + Mathf.Cos(b)*r, c.y + Mathf.Sin(b)*r);
        }
    }
}
