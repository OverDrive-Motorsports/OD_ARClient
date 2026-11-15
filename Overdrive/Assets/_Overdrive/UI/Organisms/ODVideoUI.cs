using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Shared video surfaces using the existing Overdrive type, glass and capsule vocabulary.</summary>
public static class ODVideoUI
{
    public static readonly Color Glass = new Color(.13f, .14f, .16f, .90f);
    public static RectTransform Rect(Transform p, string n, float x, float y, float w, float h) => ODRuntimeUI.Rect(p, n, x, y, w, h);
    public static TextMeshProUGUI Text(Transform p, string n, string s, float x, float y, float w, float h, float size = 20, bool bold = false, Color? c = null)
    {
        // DM Sans needs the same line-height allowance as the other Overdrive pages.
        ODDesign.FitLine(ref y, ref h, size);
        return ODRuntimeUI.Text(p, n, s, x, y, w, h, size, bold, false, c);
    }
    public static RoundedImage Fill(Transform p, string n, float x, float y, float w, float h, Color c, float r = 22)
        => ODRuntimeUI.Fill(p, n, x, y, w, h, c, r);
    public static ODVideoIcon Icon(Transform p, string name, ODVideoIcon.Kind kind, float x, float y, float size)
    {
        var icon = Rect(p, name, x, y, size, size).gameObject.AddComponent<ODVideoIcon>();
        icon.kind = kind; icon.color = ODDesign.Primary; icon.raycastTarget = false;
        return icon;
    }
    public static Button Button(Transform p, string n, string label, float x, float y, float w, float h, UnityAction action = null, bool primary = false)
    {
        var image = Fill(p, n, x, y, w, h, Color.white, h / 2);
        image.raycastTarget = true;
        var b = image.gameObject.AddComponent<Button>(); b.targetGraphic = image;
        var colors = b.colors;
        colors.normalColor = colors.selectedColor = primary ? Color.white : ODDesign.ButtonFill;
        colors.highlightedColor = primary ? new Color(.92f, .94f, .96f) : ODDesign.ButtonHover;
        colors.pressedColor = primary ? new Color(.78f, .81f, .84f) : ODDesign.ButtonPressed;
        colors.disabledColor = new Color(1, 1, 1, .06f); b.colors = colors;
        var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
        if (label != null) { float pad = Mathf.Min(16, w * .1f); var t = Text(b.transform, "Label", label, pad, 0, w - 2 * pad, h, 19, true, primary ? ODDesign.Ink : ODDesign.Primary); t.alignment = TextAlignmentOptions.Center; }
        if (action != null) b.onClick.AddListener(action);
        return b;
    }
    public static Button IconButton(Transform p, string n, ODVideoIcon.Kind kind, float x, float y, float size, UnityAction action = null, bool bare = false)
    {
        var b = Button(p, n, null, x, y, size, size, action);
        var icon = Icon(b.transform, "Icon", kind, size * .24f, size * .24f, size * .52f);
        if (bare)
        {
            // Keep the hit target, but never draw a circular backplate, including hover/press states.
            b.GetComponent<RoundedImage>().color = Color.clear; b.targetGraphic = icon;
            var c = b.colors; c.normalColor = c.selectedColor = Color.white;
            c.highlightedColor = new Color(.76f, .89f, 1f); c.pressedColor = ODDesign.GoldLight; b.colors = c;
        }
        return b;
    }
    public static RawImage Video(Transform p, string n, float x, float y, float w, float h, Texture texture, float radius = 26)
    {
        var mask = Fill(p, n, x, y, w, h, Color.white, radius);
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var image = ODRuntimeUI.Stretch(mask.transform, "Image").gameObject.AddComponent<RawImage>();
        image.texture = texture; image.raycastTarget = false;
        var fit = image.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = texture != null && texture.height > 0 ? texture.width / (float)texture.height : 16f / 9;
        return image;
    }
    /// <summary>A missing source must not render a stale frame or Unity's default white texture.</summary>
    public static void BindVideoTexture(RawImage image, Texture texture)
    {
        if (image == null) return;
        image.texture = texture; image.enabled = texture != null;
        var fit = image.GetComponent<AspectRatioFitter>();
        if (fit != null && texture != null && texture.height > 0) fit.aspectRatio = texture.width / (float)texture.height;
    }

    public static Slider Slider(Transform p, string n, float x, float y, float w, float h, float value)
    {
        var hit = Rect(p, n, x, y, w, h).gameObject.AddComponent<Image>(); hit.color = Color.clear;
        var slider = hit.gameObject.AddComponent<Slider>(); slider.targetGraphic = hit; slider.value = value;
        Fill(hit.transform, "Track", 0, h / 2 - 2, w, 4, new Color(1, 1, 1, .25f), 2);
        var fillArea = Rect(hit.transform, "FillArea", 0, h / 2 - 2, w, 4);
        var fill = Fill(fillArea, "Fill", 0, 0, 0, 0, Color.white, 2);
        slider.fillRect = fill.rectTransform;
        var handleArea = Rect(hit.transform, "HandleArea", 0, h / 2 - 8, w, 16);
        var handle = Fill(handleArea, "Handle", 0, 0, 16, 16, Color.white, 8);
        handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0, .5f);
        handle.rectTransform.pivot = new Vector2(.5f, .5f); handle.rectTransform.anchoredPosition = Vector2.zero;
        // Slider drives the perpendicular anchors to stretch. Negative height keeps a 16-unit dot inside this hit area.
        handle.rectTransform.sizeDelta = new Vector2(16, 0);
        slider.handleRect = handle.rectTransform;
        var nav = slider.navigation; nav.mode = Navigation.Mode.None; slider.navigation = nav;
        slider.SetValueWithoutNotify(value);
        return slider;
    }
    public static void Clear(Transform p)
    {
        for (int i = p.childCount - 1; i >= 0; i--) Remove(p.GetChild(i).gameObject);
    }
    public static void Remove(Object o)
    {
        if (o is GameObject go) go.SetActive(false);
        if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
    }
}
