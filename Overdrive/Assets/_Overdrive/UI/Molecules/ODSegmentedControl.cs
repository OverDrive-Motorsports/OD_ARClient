/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODSegmentedControl - Segmented control of the design system: recessed
 ## capsule track (black 22 %, 6-px inset) and a white 24 % pill that glides
 ## (spring, slight stretch) under the selected segment, labels 100 % / 66 %.
 ## The "mode" variant (Profil / Réglages) uses a solid white pill with dark
 ## text. Built at runtime; display + selection only, the caller reacts to Changed.
 ##
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ODSegmentedControl : MonoBehaviour
{
    const float SlideK = 340f, SlideC = 30f;

    static readonly Color IdleText = new Color(1f, 1f, 1f, .66f);
    static readonly Color HoverText = new Color(1f, 1f, 1f, .92f);

    float Inset;
    Color _selectedText;

    public event Action<int> Changed;

    public int Selected { get; private set; }

    RoundedImage _pill;
    TextMeshProUGUI[] _labels;
    ODTabBarItem[] _items;
    float _segmentWidth, _pillHeight;
    ODSpring _pillX;
    bool _dirty = true;

    /// <summary>Control of <paramref name="width"/> x <paramref name="height"/>, top-left at (x, y) in <paramref name="parent"/>.</summary>
    public static ODSegmentedControl Create(Transform parent, float x, float y, float width, float height, string[] labels, int selected,
        float fontSize = 15f, bool solidPill = false)
    {
        var track = ODRuntimeUI.Fill(parent, "SegmentedControl", x, y, width, height, ODDesign.SegmentTrack, height * .5f);
        track.cornerSegments = 20;
        var control = track.gameObject.AddComponent<ODSegmentedControl>();
        control.Build((RectTransform)track.transform, width, height, labels, selected, fontSize, solidPill);
        return control;
    }

    void Build(RectTransform root, float width, float height, string[] labels, int selected, float fontSize, bool solidPill)
    {
        Inset = height * 6f / 68f;   // design: 6-px inset on a 68-px track
        _selectedText = solidPill ? ODDesign.Ink : Color.white;

        int n = Mathf.Max(1, labels.Length);
        _segmentWidth = (width - Inset * 2f) / n;
        _pillHeight = height - Inset * 2f;

        _pill = ODRuntimeUI.Fill(root, "Pill", Inset, Inset, _segmentWidth, _pillHeight, solidPill ? Color.white : ODDesign.SegmentOn, _pillHeight * .5f);
        _pill.cornerSegments = 20;

        _labels = new TextMeshProUGUI[n];
        _items = new ODTabBarItem[n];
        for (int i = 0; i < n; i++)
        {
            int index = i;
            float sx = Inset + i * _segmentWidth;

            // Transparent hit area per segment; feedback is the gliding pill, not a tint.
            var hit = ODRuntimeUI.Fill(root, "Segment" + i, sx, Inset, _segmentWidth, _pillHeight, new Color(1f, 1f, 1f, 0f), _pillHeight * .5f);
            hit.raycastTarget = true;
            var button = hit.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => Select(index));
            _items[i] = hit.gameObject.AddComponent<ODTabBarItem>();

            _labels[i] = ODRuntimeUI.Text(hit.transform, "Label", labels[i], 0f, 0f, _segmentWidth, _pillHeight, fontSize, bold: true, centered: true);
            _labels[i].font = ODChampionshipTheme.SemiBold;
            _labels[i].characterSpacing = 0f;
            _labels[i].overflowMode = TMPro.TextOverflowModes.Overflow;
            _labels[i].gameObject.AddComponent<ODTypeScaled>();   // authored at page density
        }

        Selected = Mathf.Clamp(selected, 0, n - 1);
        _pillX.Snap(Inset + Selected * _segmentWidth);
        Apply();
    }

    /// <summary>Selects <paramref name="index"/> (the pill glides there) and raises Changed.</summary>
    public void Select(int index)
    {
        if (_labels == null || index < 0 || index >= _labels.Length || index == Selected) return;
        Selected = index;
        _dirty = true;
        Changed?.Invoke(index);
    }

    void Update()
    {
        if (_pill == null) return;
        float target = Inset + Selected * _segmentWidth;
        bool moving = Mathf.Abs(_pillX.value - target) > .01f || Mathf.Abs(_pillX.velocity) > .01f;
        bool hover = false;
        foreach (var item in _items) hover |= item != null && item.Hovered;
        if (!moving && !hover && !_dirty) return;
        _dirty = moving || hover;
        _pillX.Step(target, SlideK, SlideC, ODTime.Delta);
        Apply();
    }

    void Apply()
    {
        // The pill stretches a little toward where it comes from while it travels.
        float stretch = Mathf.Min(Mathf.Abs(_pillX.velocity) * .03f, _segmentWidth * .25f);
        float x = _pillX.velocity > 0f ? _pillX.value - stretch : _pillX.value;
        var rect = _pill.rectTransform;
        rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
        rect.sizeDelta = new Vector2(_segmentWidth + stretch, _pillHeight);

        // Label colour follows the pill: dark on white, light on the track.
        for (int i = 0; i < _labels.Length; i++)
        {
            float centre = Inset + i * _segmentWidth;
            float t = 1f - Mathf.Clamp01(Mathf.Abs(_pillX.value - centre) / _segmentWidth);
            Color idle = _items[i] != null && _items[i].Hovered ? HoverText : IdleText;
            _labels[i].color = Color.Lerp(idle, _selectedText, t * t);
        }
    }
}
