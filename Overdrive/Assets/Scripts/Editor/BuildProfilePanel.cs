using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the "ProfilePanel" inside ContentArea with EXPLICIT fixed positions
/// (no layout groups for the main structure), so it can never collapse to
/// zero height. Dark + gold, rounded cards, Apple/visionOS look.
/// OverdriveMainMenu auto-detects it by name when the Profile nav is selected.
/// </summary>
public static class BuildProfilePanel
{
    // ── palette ──────────────────────────────────────────────────────────────
    static readonly Color Card     = new Color(0.20f, 0.18f, 0.15f, 1f);
    static readonly Color CardSoft = new Color(0.24f, 0.21f, 0.17f, 1f);
    static readonly Color Track    = new Color(0.30f, 0.27f, 0.22f, 1f);
    static readonly Color Gold     = new Color(0.85f, 0.70f, 0.20f, 1f);
    static readonly Color Txt      = new Color(0.92f, 0.90f, 0.86f, 1f);
    static readonly Color Sub      = new Color(0.62f, 0.60f, 0.56f, 1f);

    static readonly Color Ferrari  = new Color(0.90f, 0.05f, 0.05f, 1f);
    static readonly Color RedBull  = new Color(0.10f, 0.22f, 0.60f, 1f);
    static readonly Color Merc     = new Color(0.00f, 0.82f, 0.74f, 1f);
    static readonly Color McLaren  = new Color(1.00f, 0.50f, 0.00f, 1f);

    const string CONTENT = "OverdriveMenuCanvas/OverdriveMenuPanel/Body/ContentArea";

    // ContentArea is 1180 x 720. All maths are in those local units.
    const float W = 1180f, H = 720f;
    const float PADX = 28f, PADTOP = 24f, GAP = 18f;
    const float INNERW = W - PADX * 2f;     // 1124

    const float HEADER_H = 168f;
    const float STATS_H  = 118f;

    [MenuItem("Overdrive/Build Profile Panel")]
    public static void Build()
    {
        var contentArea = GameObject.Find(CONTENT);
        if (contentArea == null) { Debug.LogError("[Profile] ContentArea not found"); return; }

        var old = contentArea.transform.Find("ProfilePanel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // Root – fills ContentArea, transparent, NO layout group
        var panel = NewGO("ProfilePanel", contentArea.transform);
        Stretch(panel);

        float headerY = -PADTOP;
        float statsY  = headerY - HEADER_H - GAP;
        float bottomY = statsY - STATS_H - GAP;
        float bottomH = H - (-bottomY) - PADTOP;   // fill down to bottom padding

        BuildHeader(panel.transform, PADX, headerY, INNERW, HEADER_H);
        BuildStats (panel.transform, PADX, statsY,  INNERW, STATS_H);
        BuildBottom(panel.transform, PADX, bottomY, INNERW, bottomH);

        panel.SetActive(false); // SelectNav manages visibility at runtime
        EditorUtility.SetDirty(contentArea);
        Debug.Log("[Profile] Panel rebuilt with fixed layout!");
        Selection.activeGameObject = panel;
    }

    // ── HEADER ────────────────────────────────────────────────────────────────
    static void BuildHeader(Transform parent, float x, float y, float w, float h)
    {
        var card = TL(Rounded(NewGO("HeaderCard", parent), Card, 22f), x, y, w, h);

        // Avatar (vertically centred at left)
        var ring = Rounded(NewGO("AvatarRing", card.transform), Gold, 56f);
        VLeft(ring, 26f, 112f, 112f);
        var inner = Rounded(NewGO("AvatarInner", ring.transform), CardSoft, 50f);
        Center(inner, 100f, 100f);
        Label(inner.transform, "Initials", "A", 44f, FontStyles.Bold, Txt, TextAlignmentOptions.Center, full:true);

        float tx = 160f;
        TL(Label(card.transform, "Name",   "Anthony",        32f, FontStyles.Bold,   Txt, TextAlignmentOptions.TopLeft), tx, -20f, 460f, 42f);
        TL(Label(card.transform, "Handle", "@overdrive_fan", 16f, FontStyles.Normal, Sub, TextAlignmentOptions.TopLeft), tx, -60f, 460f, 26f);

        Chip(card.transform, "TeamChip",   tx,         -92f, 150f, 32f, Ferrari, "Ferrari");
        Chip(card.transform, "DriverChip", tx + 162f,  -92f, 104f, 32f, RedBull, "VER");

        TL(Label(card.transform, "Level", "Level 7 · Veteran", 14f, FontStyles.Bold, Gold, TextAlignmentOptions.TopLeft), tx, -132f, 220f, 22f);
        var track = TL(Rounded(NewGO("XPTrack", card.transform), Track, 6f), tx + 210f, -128f, 360f, 12f);
        var fill  = Rounded(NewGO("XPFill", track.transform), Gold, 6f);
        var fr = fill.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0, 0); fr.anchorMax = new Vector2(0.68f, 1);
        fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;

        // Edit button (top-right)
        var edit = Rounded(NewGO("EditButton", card.transform), CardSoft, 14f);
        var er = edit.GetComponent<RectTransform>();
        er.anchorMin = new Vector2(1, 1); er.anchorMax = new Vector2(1, 1); er.pivot = new Vector2(1, 1);
        er.anchoredPosition = new Vector2(-22f, -22f); er.sizeDelta = new Vector2(170f, 44f);
        var btn = edit.AddComponent<Button>(); btn.targetGraphic = edit.GetComponent<Graphic>();
        edit.GetComponent<Graphic>().raycastTarget = true;
        Label(edit.transform, "Label", "Edit Profile", 16f, FontStyles.Bold, Txt, TextAlignmentOptions.Center, full:true);
    }

    // ── STATS ───────────────────────────────────────────────────────────────
    static void BuildStats(Transform parent, float x, float y, float w, float h)
    {
        var row = TL(NewGO("StatsRow", parent), x, y, w, h);
        float cw = (w - 3 * 16f) / 4f;
        string[,] data = { { "47", "Races" }, { "128h", "Watched" }, { "12", "Day Streak" }, { "3", "Series" } };
        for (int i = 0; i < 4; i++)
        {
            var card = TL(Rounded(NewGO("Stat_" + data[i, 1], row.transform), Card, 18f), i * (cw + 16f), 0f, cw, h);
            TL(Label(card.transform, "Value", data[i, 0], 40f, FontStyles.Bold,   Gold, TextAlignmentOptions.Center), 0f, -24f, cw, 52f);
            TL(Label(card.transform, "Label", data[i, 1], 15f, FontStyles.Normal, Sub,  TextAlignmentOptions.Center), 0f, -80f, cw, 24f);
        }
    }

    // ── BOTTOM (Following + Continue Watching) ────────────────────────────────
    static void BuildBottom(Transform parent, float x, float y, float w, float h)
    {
        var row = TL(NewGO("BottomRow", parent), x, y, w, h);
        float cw = (w - 16f) / 2f;

        var follow = TL(Rounded(NewGO("Card_Following", row.transform), Card, 20f), 0f, 0f, cw, h);
        Label2(follow, "Following", cw);
        FollowRow(follow, cw, 0, RedBull, "VER", "Red Bull Racing");
        FollowRow(follow, cw, 1, Ferrari, "LEC", "Ferrari");
        FollowRow(follow, cw, 2, Merc,    "HAM", "Mercedes");
        FollowRow(follow, cw, 3, McLaren, "NOR", "McLaren");

        var watch = TL(Rounded(NewGO("Card_Watch", row.transform), Card, 20f), cw + 16f, 0f, cw, h);
        Label2(watch, "Continue Watching", cw);
        WatchRow(watch, cw, 0, Ferrari, "Monaco GP 2024", "32:10 remaining");
        WatchRow(watch, cw, 1, RedBull, "Japan GP",        "Lap 40 / 53");
        WatchRow(watch, cw, 2, McLaren, "Bahrain GP",      "Highlights · 8 min");
    }

    static void Label2(GameObject card, string title, float cw)
    {
        TL(Label(card.transform, "Title", title, 20f, FontStyles.Bold, Txt, TextAlignmentOptions.TopLeft), 22f, -18f, cw - 44f, 30f);
    }

    static void FollowRow(GameObject card, float cw, int i, Color team, string code, string name)
    {
        float y = -62f - i * 52f;
        var row = TL(NewGO("Follow_" + code, card.transform), 16f, y, cw - 32f, 44f);
        var dot = Rounded(NewGO("Dot", row.transform), team, 11f);
        VLeft(dot, 6f, 22f, 22f);
        TL(Label(row.transform, "Code", code, 17f, FontStyles.Bold,   Txt, TextAlignmentOptions.MidlineLeft), 40f, -10f, 60f, 24f);
        TL(Label(row.transform, "Team", name, 15f, FontStyles.Normal, Sub, TextAlignmentOptions.MidlineLeft), 104f, -10f, cw - 150f, 24f);
    }

    static void WatchRow(GameObject card, float cw, int i, Color accent, string title, string sub)
    {
        float y = -62f - i * 64f;
        var row = TL(NewGO("Watch_" + i, card.transform), 16f, y, cw - 32f, 56f);
        var thumb = Rounded(NewGO("Thumb", row.transform), accent, 8f);
        VLeft(thumb, 0f, 72f, 44f);
        TL(Label(row.transform, "Title", title, 17f, FontStyles.Bold,   Txt, TextAlignmentOptions.BottomLeft), 86f, -4f,  cw - 130f, 26f);
        TL(Label(row.transform, "Sub",   sub,   14f, FontStyles.Normal, Sub, TextAlignmentOptions.TopLeft),    86f, -30f, cw - 130f, 22f);
    }

    // ── chip ────────────────────────────────────────────────────────────────
    static void Chip(Transform parent, string name, float x, float y, float w, float h, Color dot, string label)
    {
        var chip = TL(Rounded(NewGO(name, parent), CardSoft, h * 0.5f), x, y, w, h);
        var d = Rounded(NewGO("Dot", chip.transform), dot, 8f);
        VLeft(d, 10f, 16f, 16f);
        TL(Label(chip.transform, "Label", label, 14f, FontStyles.Bold, Txt, TextAlignmentOptions.MidlineLeft), 34f, -(h-20f)*0.5f, w - 40f, 20f);
    }

    // ── primitives ─────────────────────────────────────────────────────────────
    static GameObject NewGO(string n, Transform p)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject Rounded(GameObject go, Color c, float r)
    {
        var img = go.AddComponent<RoundedImage>();
        img.color = c; img.cornerRadius = r; img.cornerSegments = 12;
        img.raycastTarget = false;
        return go;
    }

    static GameObject Label(Transform p, string name, string txt, float size,
        FontStyles style, Color color, TextAlignmentOptions align, bool full = false)
    {
        var go = NewGO(name, p);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = size; t.fontStyle = style;
        t.color = color; t.alignment = align; t.raycastTarget = false;
        t.enableWordWrapping = false; t.overflowMode = TextOverflowModes.Overflow;
        if (full) Stretch(go);
        return go;
    }

    // Top-left anchored rect at (x, y) with explicit size. y is negative downward.
    static GameObject TL(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        return go;
    }

    // Vertically-centred, left-anchored (for avatars / dots / thumbs)
    static GameObject VLeft(GameObject go, float x, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f); rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f); rt.sizeDelta = new Vector2(w, h);
        return go;
    }

    static GameObject Center(GameObject go, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(w, h);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
