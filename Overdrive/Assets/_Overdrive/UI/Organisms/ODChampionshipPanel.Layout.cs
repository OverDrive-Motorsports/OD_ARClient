/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODChampionshipPanel.Layout - Places the panel's builder elements on the
 ## design system grid (Canva pages 02.4 Hors direct, 02.5 Live week-end,
 ## 02.6 Live course): header with the caps line above the title and the
 ## status pill on the right, then each state's plateaux at the mockup's
 ## positions. Coordinates are design px (1920 x 1200 window), see ODDesign.
 ## Nothing is created twice: every method is idempotent.
 ##
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using D = ODDesign;

public partial class ODChampionshipPanel
{
    // Window grid (design px).
    const float GX = D.WindowPadSide, GTop = D.WindowPadTop, GRight = 1920f - D.WindowPadSide;
    const float HeaderBottom = 162f, ContentTop = 202f, ContentBottom = 1200f - D.WindowPadBottom;
    const float ContentH = ContentBottom - ContentTop;

    bool _headerLaidOut;
    TextMeshProUGUI _headerLap;

    // ── Header ──────────────────────────────────────────────────────────────

    void LayoutHeader()
    {
        if (_headerLaidOut) return;
        _headerLaidOut = true;

        var back = transform.Find("BackToCategories");
        if (back != null)
        {
            D.Place(back, GX, HeaderBottom - 68f, 68f, 68f);
            StyleIconButton(back, 68f);
        }

        float textX = GX + 68f + 24f;
        if (subtitle != null)
        {
            D.Place(subtitle.transform, textX, GTop, 1060f, 30f);
            D.Style(subtitle, D.PageCaps, D.Weight.SemiBold, D.Secondary, caps: true);
            subtitle.overflowMode = TextOverflowModes.Ellipsis;
        }
        if (heading != null)
        {
            D.Place(heading.transform, textX, GTop + 37f, 1060f, 72f);
            D.Style(heading, D.TitleXL, D.Weight.Bold, D.Primary);
            heading.enableAutoSizing = true;
            heading.fontSizeMax = D.Px(D.TitleXL);
            heading.fontSizeMin = D.Px(40f);
            heading.overflowMode = TextOverflowModes.Ellipsis;
        }

        // Right side, from the right edge: Replay capsule, status pill, then the demo-mode capsule.
        float right = GRight;
        var replay = transform.Find("ReplayCatalogButton");
        if (replay != null)
        {
            D.Place(replay, right - 156f, HeaderBottom - 68f, 156f, 68f);
            StyleCapsule(replay, D.ButtonKind.Secondary, 22f);
            right -= 156f + 16f;
        }
        if (statusPill != null)
        {
            // Right edge kept by ApplyStatus, which sizes the pill to its text.
            D.Place(statusPill.transform, right - 300f, HeaderBottom - 64f, 300f, 64f);
        }
        var demo = transform.Find("DemoModeButton");
        if (demo != null)
        {
            D.Place(demo, right - 300f - 20f - 330f, HeaderBottom - 56f, 330f, 52f);
            StyleCapsule(demo, D.ButtonKind.Secondary, 17f);
        }
        _headerLap = D.Text(transform, "HeaderLap", "", right - 300f - 20f - 260f, HeaderBottom - 64f, 260f, 64f, 30f, D.Weight.SemiBold,
            new Color(1f, 1f, 1f, .80f), TextAlignmentOptions.MidlineRight);
        _headerLap.gameObject.SetActive(false);
    }

    /// <summary>Keeps the demo capsule / live lap to the left of the (resized) status pill.</summary>
    void AfterStatus(string lap)
    {
        if (statusPill == null) return;
        var pill = statusPill.rectTransform;
        float pillLeft = pill.anchoredPosition.x / D.Scale;
        var demo = transform.Find("DemoModeButton") as RectTransform;
        bool showLap = !string.IsNullOrEmpty(lap);
        if (_headerLap != null)
        {
            _headerLap.gameObject.SetActive(showLap);
            _headerLap.text = lap;
            D.Place(_headerLap.transform, pillLeft - 20f - 260f, HeaderBottom - 64f, 260f, 64f);
        }
        if (demo != null)
        {
            float w = demo.rect.width / D.Scale;
            float x = pillLeft - 20f - w - (showLap ? 280f : 0f);
            D.Place(demo, x, HeaderBottom - 58f, w, 52f);
        }
    }

    // ── Hors direct ─────────────────────────────────────────────────────────

    const float NextEventH = 705f, CalendarCardY = ContentTop + NextEventH + 32f, CalendarCardH = ContentH - NextEventH - 32f;
    const float StandingsX = GX + 700f + 40f;

    void LayoutOffSeason(RectTransform nextEvent, RectTransform calendar, RectTransform standings)
    {
        if (nextEvent != null)
        {
            D.Place(nextEvent, GX, ContentTop, 700f, NextEventH);
            StyleCaps(nextEvent, "Label", 44f, 44f, 612f, "Prochaine manche");
            StyleLabel(nextEvent, "MockEvent", 44f, 82f, 612f, 64f, 52f, D.Weight.Bold, D.Primary);
            StyleLabel(nextEvent, "Sport", 44f, 150f, 612f, 34f, 23f, D.Weight.Regular, D.Muted);
            for (int i = 0; i < 3; i++)
            {
                float x = 44f + i * 186f;
                var value = StyleLabel(nextEvent, "CountdownValue" + i, x, 226f, 170f, 104f, 104f, D.Weight.SemiBold, D.Primary);
                if (value != null) { value.characterSpacing = -4f; value.overflowMode = TextOverflowModes.Overflow; }
                StyleCaps(nextEvent, "CountdownUnit" + i, x, 340f, 170f, null, 18f);
            }
            var view = nextEvent.Find("ViewEvent");
            if (view != null)
            {
                D.Place(view, 44f, NextEventH - 44f - 72f, 330f, 72f);
                StyleCapsule(view, D.ButtonKind.Primary, 24f);
            }
        }
        if (calendar != null)
        {
            D.Place(calendar, GX, CalendarCardY, 700f, CalendarCardH);
            StyleCaps(calendar, "Heading", 44f, 34f, 612f, "Ensuite au calendrier");
        }
        if (standings != null)
        {
            D.Place(standings, StandingsX, ContentTop, GRight - StandingsX, ContentH);
            var full = standings.Find("FullStandings");
            if (full != null) full.gameObject.SetActive(false);   // the table already lists everyone
        }
    }

    // ── Live · week-end ─────────────────────────────────────────────────────

    const float CircuitCardW = 540f, NextSessionH = 164f, CircuitCardH = ContentH - NextSessionH - 32f;
    const float ProgramX = GX + 540f + 36f, ProgramW = 560f, WeekendStandingsX = ProgramX + 560f + 36f;

    void LayoutWeekend(RectTransform circuit, RectTransform nextSession, RectTransform program, RectTransform standings)
    {
        if (circuit != null)
        {
            D.Place(circuit, GX, ContentTop, CircuitCardW, CircuitCardH);
            StyleCaps(circuit, "Heading", 36f, 36f, 468f, "Circuit · Météo");
            var divider = circuit.Find("Divider");
            if (divider != null) { D.Place(divider, 36f, CircuitCardH - 36f - 66f * 2f - 16f - 24f, 468f, 1.6f); divider.GetComponent<Graphic>().color = D.DividerStrong; }
            var hint = circuit.Find("MapHint");
            if (hint != null) MapHint(circuit, hint, 36f, 84f, 468f, CircuitCardH - 84f - 36f - 66f * 2f - 16f - 48f);
            for (int i = 0; i < 6; i++)
            {
                float x = 36f + (i % 3) * 156f, y = CircuitCardH - 36f - 66f * 2f - 16f + (i / 3) * (66f + 16f);
                StyleLabel(circuit, "MetricLabel" + i, x, y, 150f, 24f, 18f, D.Weight.Regular, D.Secondary);
                var value = StyleLabel(circuit, "MetricValue" + i, x, y + 28f, 150f, 40f, 30f, D.Weight.SemiBold, D.Primary);
                if (value != null) value.richText = true;
            }
        }
        if (nextSession != null)
        {
            D.Place(nextSession, GX, ContentTop + CircuitCardH + 32f, CircuitCardW, NextSessionH);
            StyleCaps(nextSession, "Heading", 36f, 32f, 300f, "Prochaine session", 19f, new Color(1f, 1f, 1f, .74f));
            var countdown = nextSession.Find("Countdown");
            if (countdown != null) countdown.gameObject.SetActive(false);
            if (nextSession.Find("SessionName") == null)
            {
                D.Text(nextSession, "SessionName", "", 36f, 66f, 300f, 42f, 30f, D.Weight.SemiBold, D.Primary);
                var number = D.Text(nextSession, "BigNumber", "", CircuitCardW - 36f - 200f, 24f, 200f, 84f, 80f, D.Weight.SemiBold, D.Primary, TextAlignmentOptions.MidlineRight);
                number.characterSpacing = -3f;
                D.Text(nextSession, "BigUnit", "", CircuitCardW - 36f - 200f, 108f, 200f, 26f, 18f, D.Weight.SemiBold, new Color(1f, 1f, 1f, .74f),
                    TextAlignmentOptions.MidlineRight, caps: true, trackingEm: .12f);
            }
        }
        if (program != null)
        {
            D.Place(program, ProgramX, ContentTop, ProgramW, ContentH);
            StyleLabel(program, "Heading", 36f, 30f, 480f, 50f, 36f, D.Weight.Bold, D.Primary);
            var follow = program.Find("FollowLive");
            if (follow != null)
            {
                D.Place(follow, 36f, ContentH - 36f - 72f, ProgramW - 72f, 72f);
                StyleCapsule(follow, D.ButtonKind.Primary, 24f);
            }
        }
        if (standings != null)
        {
            D.Place(standings, WeekendStandingsX, ContentTop, GRight - WeekendStandingsX, ContentH);
            StyleLabel(standings, "Heading", 36f, 30f, 300f, 50f, 36f, D.Weight.Bold, D.Primary);
        }
    }

    /// <summary>Orange "Prochaine session" plateau of the week-end state.</summary>
    void StyleNextSession(RectTransform card)
    {
        var surface = card != null ? card.GetComponent<ODHomepageSurface>() : null;
        if (surface == null) return;
        surface.glass = true;
        surface.useMiddle = false;
        surface.topLeft = D.WithAlpha(D.Orange, .30f);
        surface.bottomRight = D.WithAlpha(D.Hex("FF6A00"), .10f);
        surface.sheen = 0f;
        surface.shadowSize = 0f;
        surface.rimWidth = 1.5f;
        surface.rimTop = D.WithAlpha(D.Orange, .40f);
        surface.rimBottom = D.WithAlpha(D.Orange, .28f);
        surface.cornerRadius = D.Px(D.CardRadius);
        surface.SetVerticesDirty();
    }

    // ── Live · course (and replay, same cards) ──────────────────────────────

    const float LiveLeftW = 580f, PodiumH = 300f, LiveButtonsY = ContentTop + PodiumH + 28f, LiveCircuitY = LiveButtonsY + 72f + 28f;
    const float LiveCircuitH = ContentBottom - LiveCircuitY, CourseX = GX + 580f + 36f;

    void LayoutLive(RectTransform section)
    {
        if (section == null) return;
        var podium = Child(section, "Podium");
        if (podium != null)
        {
            D.Place(podium, GX, ContentTop, LiveLeftW, PodiumH);
            for (int i = 0; i < 3; i++)
            {
                StyleLabel(podium, "Position" + i, 36f, 12f + i * 92f, 50f, 92f, 26f, D.Weight.Bold, i == 0 ? D.GoldText : D.RankText);
                foreach (Transform child in podium)
                    if (child.name == "Divider") child.gameObject.SetActive(false);
            }
        }
        float bw = (LiveLeftW - 18f) * .5f;
        string[] buttons = { "TVLive", "TVReplay", "Telemetry" };
        int slot = 0;
        foreach (var name in buttons)
        {
            var b = section.Find(name);
            if (b == null) continue;
            D.Place(b, GX + slot * (bw + 18f), LiveButtonsY, bw, 72f);
            StyleCapsule(b, D.ButtonKind.Secondary, 22f);
            slot++;
        }
        var circuit = Child(section, "Circuit");
        if (circuit != null)
        {
            D.Place(circuit, GX, LiveCircuitY, LiveLeftW, LiveCircuitH);
            StyleCaps(circuit, "Heading", 36f, 30f, 300f, "Circuit");
            var hint = circuit.Find("MapHint");
            if (hint != null) MapHint(circuit, hint, 36f, 84f, LiveLeftW - 72f, LiveCircuitH - 84f - 30f - 10f - 18f - 34f - 18f);
            StyleLabel(circuit, "LapProgress", 36f, LiveCircuitH - 30f - 10f - 14f - 30f, 300f, 30f, 20f, D.Weight.Regular, D.Muted);
            var percent = StyleLabel(circuit, "LapPercent", LiveLeftW - 36f - 200f, LiveCircuitH - 30f - 10f - 14f - 30f, 200f, 30f, 20f, D.Weight.Regular, D.Muted);
            if (percent != null) percent.alignment = TextAlignmentOptions.MidlineRight;
            var track = circuit.Find("ProgressTrack");
            if (track != null)
            {
                D.Place(track, 36f, LiveCircuitH - 30f - 10f, LiveLeftW - 72f, 10f);
                var g = track.GetComponent<RoundedImage>();
                if (g != null) { g.color = new Color(0f, 0f, 0f, .28f); g.cornerRadius = D.Px(5f); }
                var fill = track.Find("Fill")?.GetComponent<RoundedImage>();
                if (fill != null) { fill.color = Color.white; fill.cornerRadius = D.Px(5f); }
            }
        }
    }

    void LayoutCourseStandings()
    {
        var card = transform.Find("CourseStandings") as RectTransform;
        if (card == null || card.Find("ODLaidOut") != null) return;
        new GameObject("ODLaidOut", typeof(RectTransform)).transform.SetParent(card, false);

        float w = GRight - CourseX;
        D.Place(card, CourseX, ContentTop, w, ContentH);
        StyleLabel(card, "Heading", 40f, 30f, 700f, 52f, 36f, D.Weight.Bold, D.Primary).text = "Classement en direct";
        var lap = StyleLabel(card, "LapText", w - 40f - 300f, 34f, 300f, 40f, 20f, D.Weight.Regular, D.Secondary);
        if (lap != null) lap.alignment = TextAlignmentOptions.MidlineRight;
        var notice = card.Find("DemoNotice");
        if (notice != null) notice.gameObject.SetActive(false);
        var hintLabel = card.Find("ScrollHint");
        if (hintLabel != null) hintLabel.gameObject.SetActive(false);
        var bar = card.Find("ScrollTrack");
        if (bar != null) bar.gameObject.SetActive(false);

        // Columns (design: Pos · avatar · Pilote · Pneu · Arr. · Écart · Dernier tour).
        float y = 104f;
        string[] names = { "Column0", "Column1", "Column2", "Column3", "Column4" };
        float[] xs = { 164f, 624f, 754f, 832f, 930f };
        float[] ws = { 300f, 112f, 60f, 120f, 150f };
        for (int i = 0; i < names.Length; i++)
        {
            var label = StyleLabel(card, names[i], 40f + xs[i] - 40f, y, ws[i], 30f, 16f, D.Weight.SemiBold, D.ColumnText);
            if (label == null) continue;
            label.fontStyle = FontStyles.UpperCase;
            label.characterSpacing = 12f;
            label.alignment = i == 2 ? TextAlignmentOptions.Midline : i >= 3 ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        }
        var pos = D.Text(card, "ColumnPos", "Pos", 40f, y, 40f, 30f, 16f, D.Weight.SemiBold, D.ColumnText, caps: true, trackingEm: .12f);
        pos.transform.SetSiblingIndex(card.Find("Column0").GetSiblingIndex());
        D.Line(card, 40f, y + 40f, w - 80f, D.DividerStrong);

        var scroll = card.Find("StandingsScroll");
        if (scroll != null)
        {
            D.Place(scroll, 40f, y + 42f, w - 80f, ContentH - (y + 42f) - 36f);
            var rect = scroll.GetComponent<ScrollRect>();
            if (rect != null) rect.verticalScrollbar = null;   // design: no scroll bar, the list scrolls with the ray
        }

        // Row template: transparent row, divider at the bottom, design columns.
        var manager = card.GetComponent<RaceRankingManager>();
        var template = card.Find("StandingsScroll/Viewport/Rows/RowTemplate") as RectTransform;
        float rowH = 92f;
        if (manager != null) { manager.rowHeight = D.Px(rowH); manager.rowSpacing = 0f; }
        if (template != null) StyleRaceRow(template, w - 80f, rowH);
        foreach (Transform row in card.Find("StandingsScroll/Viewport/Rows"))
            if (row != template && row.GetComponent<RaceStandingCard>() != null) StyleRaceRow((RectTransform)row, w - 80f, rowH);
    }

    static void StyleRaceRow(RectTransform row, float w, float h)
    {
        if (row.Find("RowDivider") != null) return;
        row.sizeDelta = new Vector2(D.Px(w), D.Px(h));
        var bg = row.GetComponent<Graphic>();
        if (bg != null) bg.color = new Color(1f, 1f, 1f, 0f);
        PlaceRow(row, "RankText", 0f, 40f, h, 25f, D.Weight.Bold, D.Primary, TextAlignmentOptions.MidlineLeft);
        var dot = row.Find("TeamDot");
        if (dot != null) { D.Place(dot, 58f, (h - 48f) * .5f, 48f, 48f); var r = dot.GetComponent<RoundedImage>(); if (r != null) r.cornerRadius = D.Px(24f); }
        PlaceRow(row, "CodeText", 124f, 420f, h, 25f, D.Weight.SemiBold, D.Primary, TextAlignmentOptions.MidlineLeft);
        PlaceRow(row, "Tyre", 584f, 112f, h, 19f, D.Weight.SemiBold, D.Muted, TextAlignmentOptions.MidlineLeft);
        PlaceRow(row, "Stops", 714f, 60f, h, 22f, D.Weight.Regular, D.Primary, TextAlignmentOptions.Midline);
        PlaceRow(row, "GapText", 792f, 120f, h, 24f, D.Weight.SemiBold, D.GoldText, TextAlignmentOptions.MidlineRight);
        PlaceRow(row, "LastLap", 930f, 150f, h, 23f, D.Weight.Regular, D.Primary, TextAlignmentOptions.MidlineRight);
        var divider = D.Line(row, 0f, h - 1.6f, w, new Color(1f, 1f, 1f, .07f));
        divider.name = "RowDivider";
    }

    static void PlaceRow(Transform row, string child, float x, float w, float h, float size, D.Weight weight, Color colour, TextAlignmentOptions align)
    {
        var t = row.Find(child);
        if (t == null) return;
        D.Place(t, x, 0f, w, h);
        var label = t.GetComponent<TextMeshProUGUI>();
        if (label != null) D.Style(label, size, weight, colour, align);
    }

    // ── Shared helpers ──────────────────────────────────────────────────────

    /// <summary>The 3D-circuit hint becomes a recessed map area with a capsule "Voir en 3D" in it.</summary>
    static void MapHint(Transform card, Transform hint, float x, float y, float w, float h)
    {
        if (card.Find("MapArea") == null)
        {
            var area = D.Fill(card, "MapArea", x, y, w, h, D.Recessed, 32f);
            area.transform.SetSiblingIndex(hint.GetSiblingIndex());
            var label = D.Text(area.transform, "Label", "Tracé du circuit", 0f, h * .5f - 60f, w, 34f, 22f, D.Weight.SemiBold, D.Secondary, TextAlignmentOptions.Center);
            label.overflowMode = TextOverflowModes.Overflow;
        }
        D.Place(hint, x + (w - 300f) * .5f, y + h * .5f - 8f, 300f, 56f);
        var text = hint.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            D.Style(text, 20f, D.Weight.SemiBold, Color.white, TextAlignmentOptions.Center);
            text.text = "Voir en 3D";
            if (hint.Find("Capsule") == null)
            {
                var capsule = D.Fill(hint, "Capsule", 0f, 0f, 300f, 56f, D.ButtonFill, 28f);
                capsule.transform.SetAsFirstSibling();
            }
        }
    }

    static TextMeshProUGUI StyleLabel(Transform card, string child, float x, float y, float w, float h, float size, D.Weight weight, Color colour)
    {
        var t = card != null ? card.Find(child) : null;
        if (t == null) return null;
        D.FitLine(ref y, ref h, size);
        D.Place(t, x, y, w, h);
        var label = t.GetComponent<TextMeshProUGUI>();
        if (label != null) { D.Style(label, size, weight, colour); label.overflowMode = TextOverflowModes.Ellipsis; }
        return label;
    }

    static TextMeshProUGUI StyleCaps(Transform card, string child, float x, float y, float w, string text = null, float size = D.CapsSize, Color? colour = null)
    {
        var label = StyleLabel(card, child, x, y, w, size * 1.4f, size, D.Weight.SemiBold, colour ?? D.Secondary);
        if (label == null) return null;
        label.fontStyle = FontStyles.UpperCase;
        label.characterSpacing = 14f;
        if (text != null) label.text = text;
        return label;
    }

    static void StyleCapsule(Transform t, D.ButtonKind kind, float fontPx)
    {
        var button = t.GetComponent<Button>();
        var graphic = t.GetComponent<RoundedImage>();
        if (graphic != null) graphic.cornerRadius = ((RectTransform)t).rect.height * .5f;
        D.ApplyButton(button, kind);
        var label = t.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null)
        {
            var r = label.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            D.Style(label, fontPx, D.Weight.SemiBold, kind == D.ButtonKind.Primary ? D.Ink : Color.white, TextAlignmentOptions.Center);
        }
    }

    static void StyleIconButton(Transform t, float size)
    {
        var graphic = t.GetComponent<RoundedImage>();
        if (graphic != null) graphic.cornerRadius = D.Px(size * .5f);
        D.ApplyButton(t.GetComponent<Button>(), D.ButtonKind.Icon);
        var icon = t.GetComponentInChildren<ODNavigationIcon>(true);
        if (icon != null) { D.Place(icon.transform, (size - 30f) * .5f, (size - 30f) * .5f, 30f, 30f); icon.color = Color.white; }
    }
}
