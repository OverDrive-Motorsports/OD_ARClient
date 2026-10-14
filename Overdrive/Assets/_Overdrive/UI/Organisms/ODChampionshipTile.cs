/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODChampionshipTile - Championship poster of the catalogue (design system
 ## "Plateaux & affiches"): 150° gradient in the discipline colour, giant code
 ## watermark, code chip, live pill and favourite chip on top, then the name,
 ## the subtitle and the season line (round, next race, progress bar).
 ## Quiet motion, no overshoot: staggered fade-in, gaze hover that only
 ## brightens the poster and its rim, slight press. Dresses an existing card
 ## (keeps its Button and favourite button).
 ##
 */

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using D = ODDesign;

public class ODChampionshipTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public struct Info
    {
        public string id, code, name, subtitle, round, next;
        public float  progress;    // season progress, 0..1
        public bool   live, available;
    }

    /// <summary>Poster size in design px (3 columns x 2 rows in the 1920 x 1200 window).</summary>
    public const float W = 570.67f, H = 451f;
    const float Pad = 36f;

    // Exponential easing time constants (about 95 % of the way after 3x): calm, never bounces.
    const float HoverTime = .06f, PressTime = .04f;
    const float FadeDuration = .24f, StaggerDelay = .04f;
    const float HoverBrighten = .06f, PressScale = .985f;
    static readonly Color RimRest = new Color(1f, 1f, 1f, .14f), RimHover = new Color(1f, 1f, 1f, .38f);

    Info              _info;
    ODHomepageSurface _surface;
    CanvasGroup       _group;
    Color             _topLeft, _bottomRight;

    float _hover, _press, _fade, _introTime, _introDelay;
    bool  _hovered, _pressed;

    public Info Data => _info;

    /// <summary>Builds the poster on <paramref name="card"/>; <paramref name="order"/> staggers the entrance.</summary>
    public static ODChampionshipTile Dress(GameObject card, Info info, int order)
    {
        var tile = card.GetComponent<ODChampionshipTile>();
        if (tile == null) tile = card.AddComponent<ODChampionshipTile>();
        tile.Build(info, order);
        return tile;
    }

    void Build(Info info, int order)
    {
        _info = info;
        _introDelay = order * StaggerDelay;
        D.Championship(info.id, out Color colour, out Color dark);

        var rect = (RectTransform)transform;
        rect.pivot = new Vector2(.5f, .5f);   // grows and squashes from the centre

        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

        // Poster: opaque 150° gradient, 1-px light rim, radius 44.
        _surface = GetComponent<ODHomepageSurface>();
        _surface.material = null;     // default UI material: the gradient is opaque, no frosted room behind
        _surface.glass = true;        // glass path = rim + shadow geometry
        _surface.useMiddle = false;
        _surface.topLeft = _topLeft = colour;
        _surface.bottomRight = _bottomRight = dark;
        _surface.sheen = 0f;
        _surface.shadowSize = 0f;
        _surface.shadowAlpha = .40f;
        _surface.cornerRadius = D.Px(D.CardRadius);
        _surface.rimWidth = 1f;
        _surface.rimTop = RimRest;
        _surface.rimBottom = RimRest;
        _surface.color = Color.white;
        var outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
        _surface.SetVerticesDirty();

        var button = GetComponent<Button>();
        if (button != null) button.transition = Selectable.Transition.None;   // feedback = springs below

        foreach (Transform child in transform)
            if (child.name != "FavoriteButton" && child.name != "Face") child.gameObject.SetActive(false);
        var existing = transform.Find("Face");
        if (existing != null) Destroy(existing.gameObject);
        var face = ODRuntimeUI.Stretch(transform, "Face");
        face.SetSiblingIndex(0);
        face.gameObject.AddComponent<RectMask2D>().softness = Vector2Int.zero;   // watermark stays inside the poster

        // Giant code, top-right, white 10 %.
        var watermark = D.Text(face, "Watermark", info.code, W - 520f + 20f, -46f, 520f, 260f, 250f, D.Weight.Bold,
            new Color(1f, 1f, 1f, .07f), TextAlignmentOptions.TopRight, trackingEm: -.06f);
        watermark.overflowMode = TextOverflowModes.Overflow;

        // Top row: code chip, then live pill + favourite chip on the right.
        var chipLabel = D.Text(face, "MeasureCode", info.code, 0f, 0f, 300f, 48f, 19f, D.Weight.Bold, trackingEm: .06f);
        float chipW = chipLabel.GetPreferredValues(info.code).x / D.Scale + 40f;
        Destroy(chipLabel.gameObject);
        var chip = D.Fill(face, "Code", Pad, Pad, chipW, 48f, new Color(0f, 0f, 0f, .26f), 24f);
        D.Text(chip.transform, "Label", info.code, 0f, 0f, chipW, 48f, 19f, D.Weight.Bold, Color.white, TextAlignmentOptions.Center, trackingEm: .06f);

        float rightX = W - Pad - 48f;
        var fav = transform.Find("FavoriteButton");
        if (fav != null)
        {
            D.Place(fav, rightX, Pad, 48f, 48f);
            fav.SetAsLastSibling();
            var g = fav.GetComponent<RoundedImage>();
            if (g != null) { g.cornerRadius = D.Px(24f); g.color = Color.white; }
            var favButton = fav.GetComponent<Button>();
            if (favButton != null)
            {
                var c = favButton.colors;
                c.normalColor = c.selectedColor = new Color(0f, 0f, 0f, .26f);
                c.highlightedColor = new Color(0f, 0f, 0f, .40f);
                c.pressedColor = new Color(0f, 0f, 0f, .50f);
                c.fadeDuration = .12f;
                favButton.colors = c;
                favButton.targetGraphic?.CrossFadeColor(c.normalColor, 0f, true, true);
            }
            var o = fav.GetComponent<Outline>();
            if (o != null) o.enabled = false;
            var icon = fav.GetComponentInChildren<ODNavigationIcon>(true);
            if (icon != null) D.Place(icon.transform, 12f, 12f, 24f, 24f);
            rightX -= 12f;
        }
        if (info.live)
        {
            var measure = D.Text(face, "MeasureLive", "Live · Week-end", 0f, 0f, 400f, 48f, 17f, D.Weight.Bold, caps: true, trackingEm: .08f);
            float liveW = measure.GetPreferredValues(measure.text).x / D.Scale + 20f + 11f + 10f + 20f;
            Destroy(measure.gameObject);
            var live = D.Fill(face, "Live", rightX - liveW, Pad, liveW, 48f, new Color(0f, 0f, 0f, .30f), 24f);
            D.Dot(live.transform, 20f, 18.5f, 11f, D.Live, true);
            D.Text(live.transform, "Label", "Live · Week-end", 41f, 0f, liveW - 41f, 48f, 17f, D.Weight.Bold, Color.white, caps: true, trackingEm: .08f);
        }

        // Bottom block: name, subtitle, season line.
        float y = H - Pad;
        D.Fill(face, "Progress", Pad, y - 8f, W - Pad * 2f, 8f, new Color(0f, 0f, 0f, .30f), 4f);
        if (info.progress > 0f)
            D.Fill(face, "ProgressFill", Pad, y - 8f, Mathf.Max(8f, (W - Pad * 2f) * Mathf.Clamp01(info.progress)), 8f, Color.white, 4f);
        y -= 8f + 12f + 28f;
        D.Text(face, "Round", info.round, Pad, y, (W - Pad * 2f) * .5f, 28f, 20f, D.Weight.SemiBold);
        D.Text(face, "Next", info.available ? info.next : "Bientôt dans OverDrive", Pad + (W - Pad * 2f) * .5f, y, (W - Pad * 2f) * .5f, 28f, 20f,
            D.Weight.Regular, new Color(1f, 1f, 1f, .82f), TextAlignmentOptions.MidlineRight);
        y -= 22f;
        D.Line(face, Pad, y, W - Pad * 2f, new Color(1f, 1f, 1f, .22f));
        y -= 26f + 30f;
        D.Text(face, "Subtitle", info.subtitle, Pad, y, W - Pad * 2f, 30f, 22f, D.Weight.Regular, new Color(1f, 1f, 1f, .82f));
        y -= 8f + 50f;
        D.Text(face, "Name", info.name, Pad, y, W - Pad * 2f, 50f, 44f, D.Weight.Bold);

        _hover = _press = 0f;
        Apply();
    }

    void OnEnable()
    {
        _fade = _introTime = _hover = _press = 0f;
        _hovered = _pressed = false;
        if (_group != null) _group.alpha = 0f;
        transform.localScale = Vector3.one;
        if (_surface != null) Apply();
    }

    void Update()
    {
        if (_surface == null) return;
        float dt = ODTime.Delta;
        _introTime += dt;

        float fade = Mathf.MoveTowards(_fade, _introTime >= _introDelay ? 1f : 0f, dt / FadeDuration);
        float hover = Ease(_hover, _hovered ? 1f : 0f, HoverTime, dt);
        float press = Ease(_press, _pressed ? 1f : 0f, PressTime, dt);
        if (fade == _fade && hover == _hover && press == _press) return;   // at rest: no mesh rebuild

        bool look = hover != _hover;
        _fade = fade; _hover = hover; _press = press;
        _group.alpha = Mathf.SmoothStep(0f, 1f, _fade);
        float scale = Mathf.Lerp(1f, PressScale, _press);
        transform.localScale = new Vector3(scale, scale, 1f);
        if (look) Apply();
    }

    /// <summary>Hover look: poster a touch lighter, rim brighter. Same geometry, so nothing moves.</summary>
    void Apply()
    {
        _surface.topLeft = Color.Lerp(_topLeft, Color.white, HoverBrighten * _hover);
        _surface.bottomRight = Color.Lerp(_bottomRight, Color.white, HoverBrighten * _hover);
        _surface.rimTop = _surface.rimBottom = Color.Lerp(RimRest, RimHover, _hover);
        _surface.SetVerticesDirty();
    }

    static float Ease(float value, float target, float timeConstant, float dt)
    {
        value = Mathf.Lerp(value, target, 1f - Mathf.Exp(-dt / timeConstant));
        return Mathf.Abs(value - target) < .002f ? target : value;
    }

    public void OnPointerEnter(PointerEventData eventData) => _hovered = true;
    public void OnPointerExit(PointerEventData eventData) { _hovered = false; _pressed = false; }
    public void OnPointerDown(PointerEventData eventData) => _pressed = true;
    public void OnPointerUp(PointerEventData eventData) => _pressed = false;

    void OnDisable() { _hovered = _pressed = false; }
}
