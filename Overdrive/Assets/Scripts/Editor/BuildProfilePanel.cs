using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the "ProfilePanel" inside ContentArea, styled to match the
/// Overdrive menu (dark + gold, rounded cards, Apple/visionOS look).
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

    [MenuItem("Overdrive/Build Profile Panel")]
    public static void Build()
    {
        var contentArea = GameObject.Find(CONTENT);
        if (contentArea == null) { Debug.LogError("[Profile] ContentArea not found"); return; }

        // Remove old panel if present
        var old = contentArea.transform.Find("ProfilePanel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // ── Root panel (transparent, vertical stack) ─────────────────────────
        var panel = NewRect("ProfilePanel", contentArea.transform);
        Stretch(panel);
        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(28, 28, 24, 24);
        vlg.spacing = 18;
        vlg.childControlWidth = true;  vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true; vlg.childForceExpandHeight = false;

        BuildHeader(panel.transform);
        BuildStatsRow(panel.transform);
        BuildBottomRow(panel.transform);

        panel.SetActive(false); // SelectNav manages visibility at runtime
        EditorUtility.SetDirty(contentArea);
        Debug.Log("[Profile] Panel built successfully!");
        Selection.activeGameObject = panel;
    }

    // ── HEADER ────────────────────────────────────────────────────────────────
    static void BuildHeader(Transform parent)
    {
        var card = Card_(NewRect("HeaderCard", parent), Card, 22f);
        Fixed(card, height: 168f);

        // Avatar – gold ring + dark inner circle
        var ring = Circle("AvatarRing", card.transform, Gold, 118f);
        AnchorLeft(ring, x: 26f);
        var inner = Circle("AvatarInner", ring.transform, CardSoft, 104f);
        Center(inner);
        var initials = Text("Initials", inner.transform, "A", 44f, FontStyles.Bold, Txt, TextAlignmentOptions.Center);
        Stretch(initials);

        // Name + handle (top-left of text area)
        float tx = 168f;
        var name = Text("Name", card.transform, "Anthony", 32f, FontStyles.Bold, Txt, TextAlignmentOptions.TopLeft);
        TopLeft(name, tx, -22f, 420f, 44f);
        var handle = Text("Handle", card.transform, "@overdrive_fan", 16f, FontStyles.Normal, Sub, TextAlignmentOptions.TopLeft);
        TopLeft(handle, tx, -64f, 420f, 28f);

        // Favourite chips row
        var teamChip = Chip("TeamChip", card.transform, Ferrari, "Ferrari");
        TopLeft(teamChip, tx, -98f, 150f, 34f);
        var drvChip = Chip("DriverChip", card.transform, RedBull, "VER");
        TopLeft(drvChip, tx + 164f, -98f, 110f, 34f);

        // Level + XP bar (bottom of card)
        var lvl = Text("Level", card.transform, "Level 7 · Veteran", 15f, FontStyles.Bold, Gold, TextAlignmentOptions.TopLeft);
        TopLeft(lvl, tx, -140f, 300f, 24f);

        var track = Card_(NewRect("XPTrack", card.transform), Track, 6f);
        TopLeft(track, tx + 230f, -140f, 360f, 12f);
        var fill = Card_(NewRect("XPFill", track.transform), Gold, 6f);
        var fr = fill.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0, 0); fr.anchorMax = new Vector2(0.68f, 1);
        fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;

        // Edit button (top-right)
        var edit = Card_(NewRect("EditButton", card.transform), CardSoft, 14f);
        var er = edit.GetComponent<RectTransform>();
        er.anchorMin = new Vector2(1, 1); er.anchorMax = new Vector2(1, 1); er.pivot = new Vector2(1, 1);
        er.anchoredPosition = new Vector2(-22f, -22f); er.sizeDelta = new Vector2(170f, 44f);
        var btn = edit.AddComponent<Button>(); btn.targetGraphic = edit.GetComponent<Graphic>();
        var et = Text("Label", edit.transform, "Edit Profile", 16f, FontStyles.Bold, Txt, TextAlignmentOptions.Center);
        Stretch(et);
    }

    // ── STATS ROW ───────────────────────────────────────────────────────────
    static void BuildStatsRow(Transform parent)
    {
        var row = NewRect("StatsRow", parent);
        Fixed(row, height: 118f);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childControlWidth = true;  hlg.childForceExpandWidth = true;
        hlg.childControlHeight = true; hlg.childForceExpandHeight = true;

        StatCard(row.transform, "47",   "Races");
        StatCard(row.transform, "128h", "Watched");
        StatCard(row.transform, "12",   "Day Streak");
        StatCard(row.transform, "3",    "Series");
    }

    static void StatCard(Transform parent, string value, string label)
    {
        var card = Card_(NewRect("Stat_" + label, parent), Card, 18f);
        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 18, 16);
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;  vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false; vlg.childForceExpandHeight = false;

        var v = Text("Value", card.transform, value, 40f, FontStyles.Bold, Gold, TextAlignmentOptions.Center);
        v.AddComponent<LayoutElement>().preferredHeight = 50f;
        var l = Text("Label", card.transform, label, 15f, FontStyles.Normal, Sub, TextAlignmentOptions.Center);
        l.AddComponent<LayoutElement>().preferredHeight = 22f;
    }

    // ── BOTTOM ROW (Following + Continue Watching) ────────────────────────────
    static void BuildBottomRow(Transform parent)
    {
        var row = NewRect("BottomRow", parent);
        var le = row.AddComponent<LayoutElement>(); le.flexibleHeight = 1f;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childControlWidth = true;  hlg.childForceExpandWidth = true;
        hlg.childControlHeight = true; hlg.childForceExpandHeight = true;

        // Following
        var follow = ListCard(row.transform, "Following");
        FollowRow(follow, RedBull, "VER", "Red Bull Racing");
        FollowRow(follow, Ferrari, "LEC", "Ferrari");
        FollowRow(follow, Merc,    "HAM", "Mercedes");
        FollowRow(follow, McLaren, "NOR", "McLaren");

        // Continue watching
        var watch = ListCard(row.transform, "Continue Watching");
        WatchRow(watch, Ferrari, "Monaco GP 2024",  "32:10 remaining");
        WatchRow(watch, RedBull, "Japan GP",         "Lap 40 / 53");
        WatchRow(watch, McLaren, "Bahrain GP",       "Highlights · 8 min");
    }

    static Transform ListCard(Transform parent, string title)
    {
        var card = Card_(NewRect("Card_" + title, parent), Card, 20f);
        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(22, 22, 20, 18);
        vlg.spacing = 12;
        vlg.childControlWidth = true;  vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false; vlg.childForceExpandHeight = false;

        var t = Text("Title", card.transform, title, 20f, FontStyles.Bold, Txt, TextAlignmentOptions.TopLeft);
        t.AddComponent<LayoutElement>().preferredHeight = 30f;
        return card.transform;
    }

    static void FollowRow(Transform parent, Color teamColor, string code, string team)
    {
        var row = NewRect("Follow_" + code, parent);
        row.AddComponent<LayoutElement>().preferredHeight = 44f;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12; hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
        hlg.childControlHeight = true; hlg.childForceExpandHeight = true;

        var dot = Circle("Dot", row.transform, teamColor, 22f);
        FixedWH(dot, 26f);

        var code2 = Text("Code", row.transform, code, 17f, FontStyles.Bold, Txt, TextAlignmentOptions.MidlineLeft);
        FixedW(code2, 56f);

        var team2 = Text("Team", row.transform, team, 15f, FontStyles.Normal, Sub, TextAlignmentOptions.MidlineLeft);
        team2.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    static void WatchRow(Transform parent, Color accent, string title, string sub)
    {
        var row = NewRect("Watch_" + title, parent);
        row.AddComponent<LayoutElement>().preferredHeight = 56f;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12; hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
        hlg.childControlHeight = true; hlg.childForceExpandHeight = true;

        var thumb = Card_(NewRect("Thumb", row.transform), accent, 8f);
        FixedW(thumb, 72f);

        // text block (title + sub) as a vertical group, flexible width
        var block = NewRect("TextBlock", row.transform);
        block.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var bvlg = block.AddComponent<VerticalLayoutGroup>();
        bvlg.spacing = 2; bvlg.childAlignment = TextAnchor.MiddleLeft;
        bvlg.childControlWidth = true; bvlg.childForceExpandWidth = true;
        bvlg.childControlHeight = false; bvlg.childForceExpandHeight = false;

        var t = Text("Title", block.transform, title, 17f, FontStyles.Bold, Txt, TextAlignmentOptions.MidlineLeft);
        t.AddComponent<LayoutElement>().preferredHeight = 24f;
        var s = Text("Sub", block.transform, sub, 14f, FontStyles.Normal, Sub, TextAlignmentOptions.MidlineLeft);
        s.AddComponent<LayoutElement>().preferredHeight = 20f;
    }

    // ── chip (small rounded label) ────────────────────────────────────────────
    static GameObject Chip(string name, Transform parent, Color dotColor, string label)
    {
        var chip = Card_(NewRect(name, parent), CardSoft, 16f);
        var hlg = chip.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 12, 0, 0); hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
        hlg.childControlHeight = true; hlg.childForceExpandHeight = true;

        var dot = Circle("Dot", chip.transform, dotColor, 16f);
        FixedWH(dot, 18f);
        var t = Text("Label", chip.transform, label, 14f, FontStyles.Bold, Txt, TextAlignmentOptions.MidlineLeft);
        t.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return chip;
    }

    // ── primitives / helpers ─────────────────────────────────────────────────
    static GameObject NewRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject Card_(GameObject go, Color c, float radius)
    {
        var img = go.AddComponent<RoundedImage>();
        img.color = c; img.cornerRadius = radius; img.cornerSegments = 12;
        img.raycastTarget = false;
        return go;
    }

    static GameObject Circle(string name, Transform parent, Color c, float diameter)
    {
        var go = NewRect(name, parent);
        var img = go.AddComponent<RoundedImage>();
        img.color = c; img.cornerRadius = diameter * 0.5f; img.cornerSegments = 16;
        img.raycastTarget = false;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(diameter, diameter);
        return go;
    }

    static GameObject Text(string name, Transform parent, string txt, float size,
        FontStyles style, Color color, TextAlignmentOptions align)
    {
        var go = NewRect(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = size; t.fontStyle = style;
        t.color = color; t.alignment = align;
        t.raycastTarget = false;
        return go;
    }

    // ── layout setters ────────────────────────────────────────────────────────
    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Center(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }

    static void Fixed(GameObject go, float height)
    {
        go.AddComponent<LayoutElement>().preferredHeight = height;
    }

    static void FixedW(GameObject go, float w)
    {
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = w; le.preferredWidth = w; le.flexibleWidth = 0f;
    }

    static void FixedWH(GameObject go, float s)
    {
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = s; le.preferredWidth = s; le.flexibleWidth = 0f;
        le.minHeight = s; le.preferredHeight = s; le.flexibleHeight = 0f;
    }

    static void AnchorLeft(GameObject go, float x)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
    }

    static void TopLeft(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
