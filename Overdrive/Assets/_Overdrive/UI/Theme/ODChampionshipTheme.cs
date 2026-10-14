/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODChampionshipTheme - Colour rules shared by every championship page
 ## (championship panel in all its states, replay gallery, categories page).
 ##   • The page background gradient depends on the championship shown
 ##     (F1 red, WEC blue, MotoGP red→orange), never on the page state.
 ##   • The status pill depends on the state (hors direct grey, week-end gold,
 ##     live red, replay blue).
 ##   • visionOS look: grey "liquid glass" window (rim, sheen, soft shadow),
 ##     lighter glass platters with concentric corners, capsule controls, and
 ##     Inter typography (Regular body, SemiBold emphasis, Bold titles).
 ##
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ODStatusStyle
{
    Loading,
    OffSeason,   // HORS DIRECT
    Weekend,     // LIVE · WEEK-END
    Live,        // LIVE · COURSE
    Replay
}

public static class ODChampionshipTheme
{
    public struct Palette
    {
        public Color accent;        // badges, dots, highlights
        public Color topLeft;       // gradient corner
        public Color middle;        // dark centre
        public Color bottomRight;   // gradient corner (glow)
    }

    // ── Championship palettes (ODDesign: one colour per discipline) ─────────

    static Palette Make(string id)
    {
        ODDesign.Championship(id, out Color colour, out Color dark);
        return new Palette { accent = colour, topLeft = colour, middle = ODDesign.Night, bottomRight = dark };
    }

    /// <summary>Unknown championship / loading, and the catalogue: identity blue + gold glows.</summary>
    static readonly Palette Neutral = new Palette
    {
        accent = ODDesign.Blue, topLeft = ODDesign.Blue, middle = ODDesign.Night, bottomRight = ODDesign.Gold,
    };

    /// <summary>Accepts backend codes ("f1") and mock ids ("formula_1", "wec", "motogp").</summary>
    public static Palette NeutralPalette => Neutral;

    public static Palette For(string championshipId)
    {
        string id = ODDesign.Normalise(championshipId);
        return id == "f1" || id == "wec" || id == "motogp" || id == "indycar" || id == "fe" || id == "wrc" ? Make(id) : Neutral;
    }

    // ── Surfaces (visionOS glass) ───────────────────────────────────────────
    //
    // Window: translucent grey glass tinted by the championship, soft shadow,
    // bright top rim. Cards ("platters"): lighter glass, concentric corners
    // (window radius − padding). Passthrough can't be blurred by an app, so
    // opacity stays high enough to keep text readable over a busy room.

    public static readonly float WindowRadius  = ODDesign.Px(ODDesign.WindowRadius);
    public static readonly float PlatterRadius = ODDesign.Px(ODDesign.CardRadius);

    /// <summary>Warm neutral grey of visionOS glass; the championship colour is only a glow in the corners.</summary>
    static readonly Color GlassGrey = new Color(.145f, .14f, .14f);

    static Color Glass(Color tint, float amount, float alpha)
    {
        Color c = Color.Lerp(GlassGrey, tint, amount);
        c.a = alpha;
        return c;
    }

    static void Rim(ODHomepageSurface surface, float width, float top, float bottom)
    {
        surface.rimWidth  = width;
        surface.rimTop    = new Color(1f, 1f, 1f, top);
        surface.rimBottom = new Color(1f, 1f, 1f, bottom);
        // The rim replaces the builders' Outline (a shifted copy that looks doubled on glass).
        var outline = surface.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    static Material _glassMaterial;

    /// <summary>
    /// OD/UI Glass: shows the blurred passthrough room behind the glass fill when
    /// ODPassthroughBlur is running, plain translucent glass otherwise.
    /// </summary>
    public static Material GlassMaterial
    {
        get
        {
            if (_glassMaterial == null)
            {
                var shader = Resources.Load<Shader>("Shaders/ODGlass");
                if (shader != null) _glassMaterial = new Material(shader) { name = "OD UI Glass (runtime)" };
            }
            if (_glassMaterial != null)
            {
                // Design glass: the room behind at 62 % luminance (sRGB) — about 0.4 in linear space.
                _glassMaterial.SetFloat("_Brightness", .40f);
                _glassMaterial.SetFloat("_Desaturate", 0f);
            }
            return _glassMaterial;
        }
    }

    /// <summary>
    /// Page window (design "Fenêtre"): glass over the dimmed room, white 13 % → 5 %
    /// from top to bottom, the championship colour glowing in the top-left corner
    /// and its dark tone in the bottom-right one; 1.5-px light rim, radius 64.
    /// The shader mixes the fill at 42 % over the blurred room, hence the alphas.
    /// </summary>
    public static void ApplyBackground(ODHomepageSurface surface, Palette palette)
    {
        if (surface == null) return;
        if (GlassMaterial != null) surface.material = GlassMaterial;
        surface.glass        = true;
        surface.useMiddle    = true;
        surface.topLeft      = WithAlpha(Color.Lerp(palette.topLeft, Color.white, .18f), .78f);
        surface.middle       = new Color(1f, 1f, 1f, .20f);
        surface.bottomRight  = WithAlpha(Color.Lerp(palette.bottomRight, ODDesign.Night, .35f), .55f);
        surface.sheen        = .04f;
        surface.shadowSize   = 46f;
        surface.shadowAlpha  = .22f;
        surface.cornerRadius = WindowRadius;
        Rim(surface, 1.5f, .24f, .10f);
        surface.SetVerticesDirty();
    }

    static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    /// <summary>Navigation bar / ornaments: darker, denser glass so they read over the windows.</summary>
    public static void ApplyBar(ODHomepageSurface surface)
    {
        if (surface == null) return;
        if (GlassMaterial != null) surface.material = GlassMaterial;
        surface.glass        = true;
        surface.useMiddle    = false;
        // Design "Ornement": brighter glass (white 18 % → 8 %) floating in front, shadow below.
        surface.topLeft      = new Color(1f, 1f, 1f, .44f);
        surface.bottomRight  = new Color(1f, 1f, 1f, .20f);
        surface.sheen        = .05f;
        surface.shadowSize   = 30f;
        surface.shadowAlpha  = .30f;
        Rim(surface, 1.5f, .22f, .12f);
        surface.SetVerticesDirty();
    }

    /// <summary>
    /// A single card tinted with its championship (e.g. its tile on the categories
    /// page). <paramref name="muted"/> keeps a hint of the colour for championships
    /// that aren't available yet.
    /// </summary>
    public static void ApplyTile(ODHomepageSurface surface, Palette palette, bool muted = false)
    {
        if (surface == null) return;
        surface.glass        = true;
        surface.useMiddle    = false;
        surface.topLeft      = Glass(palette.topLeft, muted ? .40f : .85f, muted ? .62f : .78f);
        surface.bottomRight  = Glass(palette.middle, .30f, muted ? .52f : .62f);
        surface.sheen        = .06f;
        surface.shadowSize   = 0f;
        surface.cornerRadius = PlatterRadius;
        Rim(surface, 1.25f, .14f, .02f);
        surface.SetVerticesDirty();
    }

    /// <summary>Platter: lighter glass over the window, faint rim, no shadow.</summary>
    public static void ApplyGlass(ODHomepageSurface surface)
    {
        if (surface == null) return;
        surface.glass        = true;
        surface.useMiddle    = false;
        // Design "Plateau": white 7 % on the glass, a 1-px light line on top, no shadow.
        surface.topLeft      = ODDesign.Plateau;
        surface.bottomRight  = ODDesign.Plateau;
        surface.sheen        = 0f;
        surface.shadowSize   = 0f;
        surface.cornerRadius = PlatterRadius;
        Rim(surface, 1f, .08f, 0f);
        surface.SetVerticesDirty();
    }

    /// <summary>Every card under <paramref name="page"/> becomes a glass platter, except the page surface itself and popups.</summary>
    public static void ApplyGlassCards(Transform page)
    {
        if (page == null) return;
        var root = page.GetComponent<ODHomepageSurface>();
        foreach (var surface in page.GetComponentsInChildren<ODHomepageSurface>(true))
            if (surface != root && surface.GetComponentInParent<ODBackendErrorPopup>(true) == null)
                ApplyGlass(surface);
    }

    // ── Typography (DM Sans, the design system's typeface) ──────────────────
    //
    // Body text Regular, emphasis SemiBold, titles Bold with slightly tight
    // tracking. Real weights replace TextMesh Pro's faux bold.

    static TMP_FontAsset _regular, _semiBold, _bold;
    static bool _fontsLoaded;

    static void LoadFonts()
    {
        if (_fontsLoaded) return;
        _fontsLoaded = true;
        _regular  = Resources.Load<TMP_FontAsset>("Fonts/DMSans-Regular SDF");
        _semiBold = Resources.Load<TMP_FontAsset>("Fonts/DMSans-SemiBold SDF");
        _bold     = Resources.Load<TMP_FontAsset>("Fonts/DMSans-Bold SDF");
        if (_regular == null) Debug.LogWarning("[ODChampionshipTheme] DM Sans font assets missing (UI/Resources/Fonts) — keeping the current font.");
    }

    public static TMP_FontAsset Regular  { get { LoadFonts(); return _regular; } }
    public static TMP_FontAsset SemiBold { get { LoadFonts(); return _semiBold != null ? _semiBold : _regular; } }
    public static TMP_FontAsset Bold     { get { LoadFonts(); return _bold != null ? _bold : SemiBold; } }

    /// <summary>Titles start at this size; smaller bold text is emphasis (SemiBold).</summary>
    const float TitleSize = 28f;

    /// <summary>
    /// visionOS density: windows are drawn larger (OverdriveMainMenu.WindowScale) and
    /// every page label is scaled by this factor, so text is lighter in a roomier window.
    /// </summary>
    public const float TypeScale = .74f;

    /// <summary>Scales every label under <paramref name="page"/> by <see cref="TypeScale"/>, once per label.</summary>
    public static void ScaleType(Transform page)
    {
        if (page == null) return;
        foreach (var label in page.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label.GetComponent<ODTypeScaled>() != null) continue;
            label.gameObject.AddComponent<ODTypeScaled>();
            label.fontSize    *= TypeScale;
            label.fontSizeMin *= TypeScale;
            label.fontSizeMax *= TypeScale;
            if (label.characterSpacing < 0f) label.characterSpacing *= TypeScale;
        }
    }

    /// <summary>
    /// Switches every label under <paramref name="page"/> to Inter. Labels already
    /// on Inter are skipped, so it's safe to call after every refresh.
    /// </summary>
    public static void ApplyTypography(Transform page)
    {
        if (page == null || Regular == null) return;
        foreach (var label in page.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (label.font != _regular && label.font != _semiBold && label.font != _bold)
                SetFont(label, (label.fontStyle & FontStyles.Bold) != 0);
    }

    /// <summary>Weight from intent: bold + large = title (Bold), bold = emphasis (SemiBold), else Regular.</summary>
    public static void SetFont(TextMeshProUGUI label, bool bold)
    {
        if (label == null || Regular == null) return;

        bool title = bold && label.fontSize >= TitleSize;
        label.font      = title ? Bold : bold ? SemiBold : Regular;
        label.fontStyle &= ~FontStyles.Bold;
        if (title) label.characterSpacing = -1.5f;
    }

    /// <summary>The builders' blue-grey ghost button tint (CategoryPagesSetup.ButtonColor).</summary>
    static readonly Color BuilderGhost = new Color(.42f, .46f, .54f, .60f);

    static bool Near(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < .02f && Mathf.Abs(a.g - b.g) < .02f && Mathf.Abs(a.b - b.b) < .02f && Mathf.Abs(a.a - b.a) < .02f;

    /// <summary>
    /// Builder buttons follow the design: blue-grey ghost pills become the white
    /// 14 % secondary capsule, white ones the primary capsule (one per view), and
    /// clickable cards only brighten on hover.
    /// </summary>
    public static void ApplyNeutralButtons(Transform page)
    {
        if (page == null) return;
        foreach (var button in page.GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponentInParent<ODBackendErrorPopup>(true) != null) continue;
            if (button.GetComponent<ODHoverState>() != null) continue;   // already a design control
            var colors = button.colors;
            if (Near(colors.normalColor, BuilderGhost))
                ODDesign.ApplyButton(button, ODDesign.ButtonKind.Secondary);
            else if (button.targetGraphic is ODHomepageSurface)
            {
                colors.highlightedColor = new Color(.90f, .90f, .92f);
                colors.pressedColor     = new Color(.75f, .75f, .78f);
                button.colors = colors;
                var outline = button.GetComponent<Outline>();
                if (outline != null) outline.enabled = false;
            }
        }
    }

    // ── Status pill ─────────────────────────────────────────────────────────

    /// <summary>
    /// Design status pill: 64-px capsule, glowing dot + caps label (19 px Bold,
    /// +0.08 em) — the colour never speaks alone. The pill is resized to its text,
    /// keeping its right edge (the header lays it out from the right).
    /// </summary>
    public static void ApplyStatus(Graphic pill, TextMeshProUGUI label, string text, ODStatusStyle style)
    {
        ODDesign.StatusColours(style, out Color fill, out Color dotColour, out bool glow);
        if (label == null) return;
        ODDesign.Style(label, ODDesign.CapsSize, ODDesign.Weight.Bold, Color.white, TextAlignmentOptions.MidlineLeft, caps: true, trackingEm: .08f);
        label.text = text;
        label.overflowMode = TextOverflowModes.Overflow;
        if (pill == null) return;

        pill.color = fill;
        var outline = pill.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
        if (pill is RoundedImage rounded) rounded.cornerRadius = ODDesign.Px(32f);

        var pillRect = pill.rectTransform;
        float textW = label.GetPreferredValues(text).x;
        float pad = ODDesign.Px(26f), dot = ODDesign.Px(12f), gap = ODDesign.Px(12f), h = ODDesign.Px(64f);
        float right = pillRect.anchoredPosition.x + pillRect.rect.width;   // top-left anchored by the header layout
        pillRect.sizeDelta = new Vector2(pad + dot + gap + textW + pad, h);
        pillRect.anchoredPosition = new Vector2(right - pillRect.sizeDelta.x, pillRect.anchoredPosition.y);

        foreach (var name in new[] { "Dot", "Glow" })
        {
            var old = pill.transform.Find(name);
            if (old != null) Object.Destroy(old.gameObject);
        }
        var dotImage = ODDesign.Dot(pill.transform, 26f, 26f, 12f, dotColour, glow);
        dotImage.transform.SetAsLastSibling();

        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot     = new Vector2(0f, .5f);
        labelRect.offsetMin = new Vector2(pad + dot + gap, 0f);
        labelRect.offsetMax = new Vector2(0f, 0f);
        label.transform.SetAsLastSibling();
    }
}
