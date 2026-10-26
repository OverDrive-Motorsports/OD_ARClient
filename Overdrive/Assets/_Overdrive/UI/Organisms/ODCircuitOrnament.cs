/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODCircuitOrnament - visionOS "ornament" floating under the 3D circuit: one
 ## glass capsule with spin left / right, a zoom slider (− ━━●━━ +) and Reset.
 ## Built at runtime on its own world-space canvas; display only — the caller
 ## wires the events and keeps the slider in sync with the model.
 ##
 */

using System;
using UnityEngine;
using UnityEngine.UI;

public class ODCircuitOrnament : MonoBehaviour
{
    public const float Width = 640f, Height = 84f;

    const float Pad = 14f, Gap = 10f, Round = 56f;

    public event Action<float> ZoomChanged;   // 0..1 along the slider
    public event Action<int> SpinRequested;   // +1 left, -1 right
    public event Action ResetRequested;

    Slider _zoom;

    public bool IsDraggingZoom { get; private set; }

    /// <summary>Builds the ornament on <paramref name="canvas"/> (a world-space canvas sized Width x Height).</summary>
    public static ODCircuitOrnament Build(Canvas canvas)
    {
        var ornament = canvas.gameObject.AddComponent<ODCircuitOrnament>();
        ornament.BuildContent((RectTransform)canvas.transform);
        return ornament;
    }

    /// <summary>Moves the knob without raising ZoomChanged (model zoomed by gesture).</summary>
    public void SetZoom(float normalized)
    {
        if (_zoom != null && !IsDraggingZoom) _zoom.SetValueWithoutNotify(Mathf.Clamp01(normalized));
    }

    void BuildContent(RectTransform root)
    {
        root.sizeDelta = new Vector2(Width, Height);

        var glass = ODRuntimeUI.Stretch(root, "Glass").gameObject.AddComponent<ODHomepageSurface>();
        glass.raycastTarget = true;   // the capsule swallows the ray like the other windows
        ODChampionshipTheme.ApplyBar(glass);
        glass.cornerRadius = Height * .5f;
        glass.shadowSize = 18f;

        float y = (Height - Round) * .5f;
        float x = Pad;
        RoundButton(root, "SpinLeft", "↺", x, y, () => SpinRequested?.Invoke(1)); x += Round + Gap;
        RoundButton(root, "SpinRight", "↻", x, y, () => SpinRequested?.Invoke(-1)); x += Round + Gap;
        Divider(root, x, Height); x += 1f + Gap * 1.5f;

        ODRuntimeUI.Text(root, "ZoomOutIcon", "−", x, 0f, 24f, Height, 28f, bold: true, centered: true, color: ODRuntimeUI.Secondary); x += 24f + Gap;
        const float sliderWidth = 236f;
        _zoom = BuildSlider(root, x, sliderWidth); x += sliderWidth + Gap;
        ODRuntimeUI.Text(root, "ZoomInIcon", "+", x, 0f, 24f, Height, 28f, bold: true, centered: true, color: ODRuntimeUI.Secondary); x += 24f + Gap * 1.5f;
        Divider(root, x, Height); x += 1f + Gap;

        float resetWidth = Width - Pad - x;
        var reset = PillButton(root, "Reset", "Réinitialiser", x, y, resetWidth, Round);
        reset.onClick.AddListener(() => ResetRequested?.Invoke());
    }

    Slider BuildSlider(RectTransform root, float x, float width)
    {
        var area = ODRuntimeUI.Rect(root, "Zoom", x, 0f, width, Height);

        // Invisible hit area: the whole row height grabs the knob, easier to aim at with a ray.
        var hit = area.gameObject.AddComponent<RoundedImage>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        hit.raycastTarget = true;

        const float trackH = 6f, knob = 30f;
        var track = ODRuntimeUI.Fill(area, "Track", 0f, (Height - trackH) * .5f, width, trackH, new Color(1f, 1f, 1f, .18f), trackH * .5f);

        // Slider drives anchors: fill stretches across FillArea, the knob spans HandleArea's height.
        var fillArea = ODRuntimeUI.Rect(area, "FillArea", knob * .5f, (Height - trackH) * .5f, width - knob, trackH);
        var fill = ODRuntimeUI.Fill(fillArea, "Fill", 0f, 0f, 0f, trackH, new Color(1f, 1f, 1f, .92f), trackH * .5f);
        Stretch(fill.rectTransform);

        var handleArea = ODRuntimeUI.Rect(area, "HandleArea", knob * .5f, (Height - knob) * .5f, width - knob, knob);
        var handle = ODRuntimeUI.Fill(handleArea, "Knob", 0f, 0f, knob, knob, Color.white, knob * .5f);
        var handleRect = handle.rectTransform;
        handleRect.pivot = new Vector2(.5f, .5f);
        handleRect.sizeDelta = new Vector2(knob, 0f);
        handleRect.anchoredPosition = Vector2.zero;

        var slider = area.gameObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        slider.transition = Selectable.Transition.ColorTint;
        var colors = slider.colors;
        colors.normalColor = colors.selectedColor = Color.white;
        colors.highlightedColor = new Color(.93f, .78f, .45f);   // gold, like the window bars
        colors.pressedColor = new Color(1f, .85f, .35f);
        colors.fadeDuration = .12f;
        slider.colors = colors;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.onValueChanged.AddListener(v => ZoomChanged?.Invoke(v));

        var drag = area.gameObject.AddComponent<DragState>();
        drag.changed = dragging => IsDraggingZoom = dragging;
        track.raycastTarget = false;
        return slider;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, .5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static void RoundButton(RectTransform root, string name, string glyph, float x, float y, UnityEngine.Events.UnityAction onClick)
    {
        var button = PillButton(root, name, glyph, x, y, Round, Round);
        button.onClick.AddListener(onClick);
    }

    static Button PillButton(RectTransform root, string name, string label, float x, float y, float w, float h)
    {
        // White graphic; the glass levels live in the ColorBlock (the tint multiplies the graphic colour).
        var image = ODRuntimeUI.Fill(root, name, x, y, w, h, Color.white, h * .5f);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = colors.selectedColor = new Color(1f, 1f, 1f, .12f);
        colors.highlightedColor = new Color(1f, 1f, 1f, .26f);
        colors.pressedColor = new Color(1f, 1f, 1f, .40f);
        colors.fadeDuration = .12f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ODRuntimeUI.Text(image.transform, "Label", label, 0f, 0f, w, h, label.Length > 2 ? 19f : 28f, bold: true, centered: true);
        return button;
    }

    static void Divider(RectTransform root, float x, float height)
    {
        ODRuntimeUI.Fill(root, "Divider", x, height * .25f, 1f, height * .5f, ODRuntimeUI.Line, 0f);
    }

    /// <summary>Tells whether the slider is held, so gesture zoom doesn't fight the user's drag.</summary>
    sealed class DragState : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        public Action<bool> changed;
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData) => changed?.Invoke(true);
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData) => changed?.Invoke(false);
        void OnDisable() => changed?.Invoke(false);
    }
}
