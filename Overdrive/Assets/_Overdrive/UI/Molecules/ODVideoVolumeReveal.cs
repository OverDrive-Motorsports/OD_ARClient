using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shared hover/press region for the TV's speaker and volume slider. The speaker slides left,
/// the slider fades outwards, and the timeline lends it space. Ray and touch presses keep it open
/// until release, including when the pointer leaves the slider during a drag.
/// </summary>
[DisallowMultipleComponent]
public sealed class ODVideoVolumeReveal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public ODVideoVolumeReveal owner;
    public RectTransform speaker;
    public Slider volume;
    public Slider timeline;
    public RectTransform separator;
    public CanvasGroup sliderGroup;
    private readonly HashSet<int> _hovered = new HashSet<int>();
    private readonly HashSet<int> _pressed = new HashSet<int>();
    private readonly HashSet<ODVideoVolumeReveal> _regions = new HashSet<ODVideoVolumeReveal>();
    private float _amount, _lastActive = -1;
    private ODVideoVolumeReveal Root => owner != null ? owner : this;

    public void OnPointerEnter(PointerEventData data) { _hovered.Add(data.pointerId); Register(); }
    public void OnPointerExit(PointerEventData data) { _hovered.Remove(data.pointerId); Register(); }
    public void OnPointerDown(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        _pressed.Add(data.pointerId); Register();
    }
    public void OnPointerUp(PointerEventData data) { _pressed.Remove(data.pointerId); Register(); }

    private void Register()
    {
        var root = Root;
        if (_hovered.Count > 0 || _pressed.Count > 0)
        { root._regions.Add(this); root._lastActive = Time.unscaledTime; }
        else root._regions.Remove(this);
    }
    private void Update()
    {
        if (owner != null) return;
        bool active = _regions.Count > 0;
        if (active) _lastActive = Time.unscaledTime;
        // Do not move a captured button/slider under a pressed fingertip or controller ray.
        foreach (var region in _regions)
            if (region.owner != null && region._pressed.Count > 0) return;
        // Small grace period bridges the space between speaker and slider without flicker.
        float target = active || Time.unscaledTime - _lastActive < .18f ? 1 : 0;
        float next = Mathf.MoveTowards(_amount, target, Time.unscaledDeltaTime / .2f);
        if (Mathf.Approximately(next, _amount)) return;
        _amount = next;
        ApplyPresentation(Mathf.SmoothStep(0, 1, _amount));
    }

    /// <summary>Saved prefabs and newly reopened TVs start with a compact, non-interactive slider.</summary>
    public void ResetPresentation()
    {
        _hovered.Clear(); _pressed.Clear(); _regions.Clear(); _amount = 0; _lastActive = -1;
        if (owner == null) ApplyPresentation(0);
    }
    private void OnEnable() => ResetPresentation();
    private void OnDisable()
    {
        if (owner != null) owner._regions.Remove(this);
        ResetPresentation();
    }
    private void ApplyPresentation(float reveal)
    {
        if (speaker == null || volume == null || timeline == null || separator == null || sliderGroup == null) return;
        float x = Mathf.Lerp(1052, 912, reveal);
        speaker.anchoredPosition = new Vector2(x, -597);
        volume.GetComponent<RectTransform>().anchoredPosition = new Vector2(x + 56, -601);
        ODVideoUI.SetSliderWidth(volume, Mathf.Max(0, 1100 - (x + 56)));
        sliderGroup.alpha = reveal;
        sliderGroup.interactable = sliderGroup.blocksRaycasts = reveal > .85f;
        separator.anchoredPosition = new Vector2(x - 20, -606);
        ODVideoUI.SetSliderWidth(timeline, x - 44 - 320);
        var hit = (RectTransform)transform;
        hit.anchoredPosition = new Vector2(x - 12, -589);
        hit.sizeDelta = new Vector2(1112 - (x - 12), 64);
    }
}
