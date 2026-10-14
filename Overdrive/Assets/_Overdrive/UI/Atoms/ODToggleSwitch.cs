/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODToggleSwitch - Switch of the design system: 84 x 48 capsule, identity
 ## blue when on, white 16 % when off, white 40-px knob that springs across. Built at runtime;
 ## display + local state only.
 ##
 */

using System;
using UnityEngine;
using UnityEngine.UI;

public class ODToggleSwitch : MonoBehaviour
{
    public static readonly float Width = ODDesign.Px(84f), Height = ODDesign.Px(48f);
    static readonly float Knob = ODDesign.Px(40f), Gap = ODDesign.Px(4f);

    static readonly Color OnColor  = ODDesign.Blue;
    static readonly Color OffColor = ODDesign.SwitchOff;

    public event Action<bool> Changed;
    public bool IsOn { get; private set; }

    RoundedImage _track;
    RectTransform _knob;
    ODSpring _t;

    /// <summary>Switch with its top-left at (x, y) in <paramref name="parent"/>.</summary>
    public static ODToggleSwitch Create(Transform parent, float x, float y, bool on)
    {
        var track = ODRuntimeUI.Fill(parent, "Toggle", x, y, Width, Height, Color.white, Height * .5f);
        track.raycastTarget = true;
        var toggle = track.gameObject.AddComponent<ODToggleSwitch>();
        toggle._track = track;
        toggle._knob = ODRuntimeUI.Fill(track.transform, "Knob", Gap, Gap, Knob, Knob, Color.white, Knob * .5f).rectTransform;
        var button = track.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(toggle.Flip);
        toggle.IsOn = on;
        toggle._t.Snap(on ? 1f : 0f);
        toggle.Apply();
        return toggle;
    }

    void Flip()
    {
        IsOn = !IsOn;
        Changed?.Invoke(IsOn);
    }

    void Update()
    {
        float target = IsOn ? 1f : 0f;
        if (Mathf.Abs(_t.value - target) < .001f && Mathf.Abs(_t.velocity) < .001f) return;
        _t.Step(target, 420f, 30f, ODTime.Delta);
        Apply();
    }

    void Apply()
    {
        float t = Mathf.Clamp01(_t.value);
        _track.color = Color.Lerp(OffColor, OnColor, t);
        _knob.anchoredPosition = new Vector2(Mathf.LerpUnclamped(Gap, Width - Gap - Knob, _t.value), -Gap);
    }
}
