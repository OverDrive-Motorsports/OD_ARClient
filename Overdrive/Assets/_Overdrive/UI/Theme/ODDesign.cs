/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODDesign - The OverDrive VR v2 design system (Canva "01 · Fondations" and
 ## "01 · Composants"), as tokens and runtime builders. Every size is given
 ## in design pixels — the mockups draw a window at 1920 x 1200 — and turned
 ## into window units with Px() (window = 1200 x 750 units).
 ##   • Colours: gold + blue identity accents, status colours always with a
 ##     label, one colour per championship. Text 96 / 62 / 50 % white.
 ##   • Materials: glass window, white 7 % plateaux, black 22 % recessed
 ##     lists and fields, ornaments in front.
 ##   • Shapes: window 64, card 44, plateau 40, tile 28, capsules; 8-px grid.
 ##   • Type: DM Sans — 64 Bold, 44 / 34 / 27 SemiBold, 24 Regular, 19 caps.
 ##   • Controls: 64–72 px capsules; hover lights the target (+ soft ring),
 ##     press dims and shrinks to 96 %, nothing around it moves.
 ##
 */

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ODDesign
{
    // ── Scale ───────────────────────────────────────────────────────────────

    /// <summary>Design pixels → window units (1920-px mockup → 1200-unit window).</summary>
    public const float Scale = .625f;
    public static float Px(float designPx) => designPx * Scale;

    /// <summary>Window size of every page drawn from the mockups (16:10).</summary>
    public static readonly Vector2 WindowSize = new Vector2(1200f, 750f);

    // ── Colours ─────────────────────────────────────────────────────────────

    public static readonly Color Gold = Hex("C9A84C");
    public static readonly Color GoldText = Hex("E3C97A");
    public static readonly Color GoldLight = Hex("F0D68A");
    public static readonly Color Blue = Hex("378ADD");
    public static readonly Color BlueText = Hex("7DB6F0");
    public static readonly Color BlueSoft = Hex("9CCBF5");
    public static readonly Color Night = Hex("15171C");
    public static readonly Color Ink = Hex("0B0C10");   // text on white

    public static readonly Color Live = Hex("FF453A");   // direct, erreur
    public static readonly Color Green = Hex("30D158");   // connecté, piste libre
    public static readonly Color GreenText = Hex("6BE08C");
    public static readonly Color Orange = Hex("FF9F0A");   // week-end en cours, attention
    public static readonly Color OrangeText = Hex("FFB340");
    public static readonly Color Purple = Hex("BF5AF2");   // meilleur tour
    public static readonly Color PurpleText = Hex("D59BF6");
    public static readonly Color Destructive = Hex("FF6B61");
    public static readonly Color TyreSoft = Hex("FF453A");
    public static readonly Color TyreMedium = Hex("FFD60A");
    public static readonly Color TyreHard = Hex("F2F2F7");

    public static readonly Color Primary = new Color(1f, 1f, 1f, .96f);
    public static readonly Color Secondary = new Color(1f, 1f, 1f, .62f);
    public static readonly Color Muted = new Color(1f, 1f, 1f, .66f);
    public static readonly Color Tertiary = new Color(1f, 1f, 1f, .50f);
    public static readonly Color RankText = new Color(1f, 1f, 1f, .70f);
    public static readonly Color ColumnText = new Color(1f, 1f, 1f, .58f);

    public static readonly Color Plateau = new Color(1f, 1f, 1f, .07f);
    public static readonly Color Recessed = new Color(0f, 0f, 0f, .22f);
    public static readonly Color Divider = new Color(1f, 1f, 1f, .08f);
    public static readonly Color DividerStrong = new Color(1f, 1f, 1f, .10f);
    public static readonly Color Avatar = new Color(1f, 1f, 1f, .10f);

    public static readonly Color ButtonFill = new Color(1f, 1f, 1f, .14f);
    public static readonly Color ButtonHover = new Color(1f, 1f, 1f, .26f);
    public static readonly Color ButtonPressed = new Color(1f, 1f, 1f, .34f);
    public static readonly Color ButtonDisabled = new Color(1f, 1f, 1f, .07f);
    public static readonly Color IconButtonFill = new Color(1f, 1f, 1f, .12f);

    public static readonly Color SegmentTrack = new Color(0f, 0f, 0f, .22f);
    public static readonly Color SegmentOn = new Color(1f, 1f, 1f, .24f);
    public static readonly Color SwitchOff = new Color(1f, 1f, 1f, .16f);

    // ── Shapes & type (design px) ───────────────────────────────────────────

    public const float WindowRadius = 64f, CardRadius = 44f, PlateauRadius = 40f, TileRadius = 28f;
    public const float WindowPadTop = 60f, WindowPadSide = 72f, WindowPadBottom = 64f;

    public const float TitleXL = 64f, Title1 = 44f, Title2 = 34f, RowTitle = 27f, Body = 24f, CapsSize = 19f, PageCaps = 21f;

    public enum Weight { Regular, SemiBold, Bold }

    // ── Championships ───────────────────────────────────────────────────────

    /// <summary>Discipline colour, and the dark end of its poster gradient.</summary>
    public static void Championship(string id, out Color colour, out Color dark)
    {
        switch (Normalise(id))
        {
            case "f1": colour = Hex("E10600"); dark = Hex("4A0200"); return;
            case "motogp": colour = Hex("FF6A00"); dark = Hex("5A2400"); return;
            case "wec": colour = Hex("4C86FF"); dark = Hex("0E2766"); return;
            case "fe": colour = Hex("22B8AC"); dark = Hex("0A3C39"); return;
            case "indycar": colour = Hex("8A5CF6"); dark = Hex("2A1661"); return;
            case "wrc": colour = Hex("2FBF71"); dark = Hex("0A3320"); return;
            default: colour = Blue; dark = Night; return;
        }
    }

    public static Color ChampionshipColour(string id) { Championship(id, out Color c, out _); return c; }

    /// <summary>Backend codes ("f1"), mock ids ("formula_1") and names ("World Endurance").</summary>
    public static string Normalise(string id)
    {
        string s = (id ?? "").ToLowerInvariant().Replace("_", "").Replace("-", "").Replace(" ", "");
        if (s == "f1" || s.StartsWith("formula1") || s.StartsWith("formule1")) return "f1";
        if (s == "wec" || s.Contains("endurance")) return "wec";
        if (s == "motogp" || s == "mgp") return "motogp";
        if (s == "indycar" || s == "ic") return "indycar";
        if (s == "formulae" || s == "fe") return "fe";
        if (s == "wrc" || s.Contains("rally")) return "wrc";
        return s;
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    public static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    // ── Builders (positions and sizes in design px, top-left of the parent) ─

    public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h) =>
        ODRuntimeUI.Rect(parent, name, Px(x), Px(y), Px(w), Px(h));

    /// <summary>Rounded fill; radius in design px (a capsule when it's half the height).</summary>
    public static RoundedImage Fill(Transform parent, string name, float x, float y, float w, float h, Color colour, float radius)
    {
        var image = ODRuntimeUI.Fill(parent, name, Px(x), Px(y), Px(w), Px(h), colour, Px(radius));
        image.cornerSegments = 20;
        return image;
    }

    public static RoundedImage Line(Transform parent, float x, float y, float w, Color? colour = null) =>
        Fill(parent, "Divider", x, y, w, 1.6f, colour ?? Divider, 0f);

    /// <summary>DM Sans label. Caps labels get the design's wide tracking; big titles a tight one.</summary>
    public static TextMeshProUGUI Text(Transform parent, string name, string text, float x, float y, float w, float h,
        float sizePx, Weight weight = Weight.Regular, Color? colour = null, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
        bool caps = false, float trackingEm = 0f)
    {
        FitLine(ref y, ref h, sizePx);
        var label = ODRuntimeUI.Rect(parent, name, Px(x), Px(y), Px(w), Px(h)).gameObject.AddComponent<TextMeshProUGUI>();
        label.gameObject.AddComponent<ODTypeScaled>();
        Style(label, sizePx, weight, colour ?? Primary, align, caps, trackingEm);
        label.text = text;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    /// <summary>
    /// A TMP line taller than its rect is dropped by the ellipsis overflow: grow the
    /// rect to the line height (1.32 x size), keeping it centred where it was.
    /// </summary>
    public static void FitLine(ref float y, ref float h, float sizePx)
    {
        float need = sizePx * 1.32f;
        if (h >= need) return;
        y -= (need - h) * .5f;
        h = need;
    }

    /// <summary>Applies the type scale to an existing label (builder labels re-styled in place).</summary>
    public static void Style(TextMeshProUGUI label, float sizePx, Weight weight, Color colour,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool caps = false, float trackingEm = 0f)
    {
        if (label == null) return;
        if (label.GetComponent<ODTypeScaled>() == null) label.gameObject.AddComponent<ODTypeScaled>();
        label.enableAutoSizing = false;
        label.fontSize = Px(sizePx);
        label.font = weight == Weight.Bold ? ODChampionshipTheme.Bold : weight == Weight.SemiBold ? ODChampionshipTheme.SemiBold : ODChampionshipTheme.Regular;
        label.fontStyle = caps ? FontStyles.UpperCase : FontStyles.Normal;
        label.color = colour;
        label.alignment = align;
        float tracking = trackingEm != 0f ? trackingEm : caps ? .14f : sizePx >= 52f ? -.025f : sizePx >= 40f ? -.01f : 0f;
        label.characterSpacing = tracking * 100f;
    }

    /// <summary>Caps section label: 19 px, SemiBold, +0.14 em, 62 % white.</summary>
    public static TextMeshProUGUI Caps(Transform parent, string name, string text, float x, float y, float w, float sizePx = CapsSize, Color? colour = null) =>
        Text(parent, name, text, x, y, w, sizePx * 1.4f, sizePx, Weight.SemiBold, colour ?? Secondary, caps: true);

    /// <summary>Moves / resizes an existing rect, in design px from the parent's top-left.</summary>
    public static void Place(Transform t, float x, float y, float w, float h)
    {
        var rect = t as RectTransform;
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(Px(x), -Px(y));
        rect.sizeDelta = new Vector2(Px(w), Px(h));
    }

    // ── Buttons ─────────────────────────────────────────────────────────────

    public enum ButtonKind { Primary, Secondary, Destructive, Icon, Gold, Ghost }

    /// <summary>Capsule button (height in design px, 64–72). The whole state set lives in the ColorBlock + ODHoverState.</summary>
    public static Button Button(Transform parent, string name, string text, float x, float y, float w, float h, ButtonKind kind, float fontPx = Body)
    {
        var pill = Fill(parent, name, x, y, w, h, Color.white, h * .5f);
        pill.raycastTarget = true;
        var button = pill.gameObject.AddComponent<Button>();
        button.targetGraphic = pill;
        ApplyButton(button, kind);
        if (!string.IsNullOrEmpty(text))
        {
            var label = Text(pill.transform, "Label", text, 0f, 0f, w, h, fontPx, Weight.SemiBold,
                kind == ButtonKind.Primary ? Ink : kind == ButtonKind.Destructive ? Destructive : kind == ButtonKind.Gold ? GoldLight : Color.white,
                TextAlignmentOptions.Center);
            label.overflowMode = TextOverflowModes.Overflow;
        }
        return button;
    }

    /// <summary>States of the design: rest / gaze hover (lighter + 4-px ring) / pressed (lighter, 96 %) / disabled.</summary>
    public static void ApplyButton(Button button, ButtonKind kind)
    {
        if (button == null) return;
        if (button.targetGraphic != null) button.targetGraphic.color = Color.white;
        var colors = button.colors;
        switch (kind)
        {
            case ButtonKind.Primary:
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(.93f, .93f, .95f, 1f);
                colors.pressedColor = new Color(.84f, .84f, .87f, 1f);
                break;
            case ButtonKind.Gold:
                colors.normalColor = WithAlpha(Gold, .24f);
                colors.highlightedColor = WithAlpha(Gold, .36f);
                colors.pressedColor = WithAlpha(Gold, .46f);
                break;
            case ButtonKind.Ghost:
                colors.normalColor = new Color(1f, 1f, 1f, 0f);
                colors.highlightedColor = new Color(1f, 1f, 1f, .12f);
                colors.pressedColor = new Color(1f, 1f, 1f, .20f);
                break;
            case ButtonKind.Icon:
                colors.normalColor = IconButtonFill;
                colors.highlightedColor = ButtonHover;
                colors.pressedColor = ButtonPressed;
                break;
            default:
                colors.normalColor = ButtonFill;
                colors.highlightedColor = ButtonHover;
                colors.pressedColor = ButtonPressed;
                break;
        }
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = ButtonDisabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = .12f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var outline = button.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
        if (button.GetComponent<ODHoverState>() == null) button.gameObject.AddComponent<ODHoverState>();
        button.targetGraphic?.CrossFadeColor(colors.normalColor, 0f, true, true);
    }

    /// <summary>Round icon button (68 px) with one of the app's vector symbols.</summary>
    public static Button IconButton(Transform parent, string name, float x, float y, float size, ODNavigationIcon.Symbol symbol,
        ButtonKind kind = ButtonKind.Icon, float iconPx = 30f)
    {
        var button = Button(parent, name, "", x, y, size, size, kind);
        var icon = Rect(button.transform, "Icon", (size - iconPx) * .5f, (size - iconPx) * .5f, iconPx, iconPx).gameObject.AddComponent<ODNavigationIcon>();
        icon.symbol = symbol;
        icon.color = kind == ButtonKind.Gold ? GoldLight : Color.white;
        icon.raycastTarget = false;
        return button;
    }

    /// <summary>"Plus d'options": round button with three dots.</summary>
    public static Button MoreButton(Transform parent, string name, float x, float y, float size = 68f)
    {
        var button = Button(parent, name, "", x, y, size, size, ButtonKind.Icon);
        for (int i = -1; i <= 1; i++)
            Fill(button.transform, "Dot" + (i + 1), size * .5f + i * 10.5f - 3.5f, size * .5f - 3.5f, 7f, 7f, Color.white, 3.5f);
        return button;
    }

    // ── Status pill ─────────────────────────────────────────────────────────

    /// <summary>Capsule with a glowing dot and a caps label — never colour alone.</summary>
    public static void StatusColours(ODStatusStyle style, out Color fill, out Color dot, out bool glow)
    {
        switch (style)
        {
            case ODStatusStyle.Weekend: fill = WithAlpha(Orange, .24f); dot = Orange; glow = true; return;
            case ODStatusStyle.Live: fill = WithAlpha(Live, .28f); dot = Live; glow = true; return;
            case ODStatusStyle.Replay: fill = WithAlpha(Blue, .26f); dot = BlueSoft; glow = false; return;
            default: fill = new Color(1f, 1f, 1f, .12f); dot = new Color(1f, 1f, 1f, .60f); glow = false; return;
        }
    }

    public static RoundedImage Dot(Transform parent, float x, float y, float size, Color colour, bool glow)
    {
        if (glow)
        {
            var halo = Fill(parent, "Glow", x - size * .6f, y - size * .6f, size * 2.2f, size * 2.2f, WithAlpha(colour, .28f), size * 1.1f);
            halo.raycastTarget = false;
        }
        return Fill(parent, "Dot", x, y, size, size, colour, size * .5f);
    }
}

/// <summary>
/// Gaze feedback of the design: the hovered control gets a soft 4-px ring
/// (+10 % white) and lifts; a press shrinks it to 96 %. The surroundings never move.
/// </summary>
public class ODHoverState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool Hovered { get; private set; }
    public bool Pressed { get; private set; }

    RoundedImage _ring;
    float _hover, _press;
    Vector3 _baseScale = Vector3.one;
    bool _init;

    public void OnPointerEnter(PointerEventData e) => Hovered = true;
    public void OnPointerExit(PointerEventData e) { Hovered = false; Pressed = false; }
    public void OnPointerDown(PointerEventData e) => Pressed = true;
    public void OnPointerUp(PointerEventData e) => Pressed = false;

    void OnDisable() { Hovered = Pressed = false; _hover = _press = 0f; Apply(); }

    void Update()
    {
        float h = Hovered ? 1f : 0f, p = Pressed ? 1f : 0f;
        if (Mathf.Abs(_hover - h) < .001f && Mathf.Abs(_press - p) < .001f) return;
        float k = 1f - Mathf.Exp(-ODTime.Delta * 18f);
        _hover = Mathf.Lerp(_hover, h, k);
        _press = Mathf.Lerp(_press, p, k);
        Apply();
    }

    void Apply()
    {
        var rect = transform as RectTransform;
        if (rect == null) return;
        if (!_init) { _init = true; _baseScale = rect.localScale; }
        if (_ring == null && _hover > .01f)
        {
            var graphic = GetComponent<RoundedImage>();
            float ring = ODDesign.Px(4f);
            var go = new GameObject("HoverRing", typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(transform, false);
            r.SetAsFirstSibling();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(-ring, -ring); r.offsetMax = new Vector2(ring, ring);
            _ring = go.AddComponent<RoundedImage>();
            _ring.cornerRadius = (graphic != null ? graphic.cornerRadius : rect.rect.height * .5f) + ring;
            _ring.cornerSegments = 20;
            _ring.raycastTarget = false;
        }
        if (_ring != null) _ring.color = new Color(1f, 1f, 1f, .10f * _hover);
        rect.localScale = _baseScale * (1f - .04f * _press);
    }
}
