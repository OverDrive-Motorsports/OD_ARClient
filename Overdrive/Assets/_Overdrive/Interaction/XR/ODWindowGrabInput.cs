using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>Owns the press/release of one ray. A different hand cannot start or hold its window grab.</summary>
public sealed class ODWindowGrabInput : MonoBehaviour
{
    public UnityEngine.Object selector;
    public bool IsPressed { get; private set; }
    public bool JustPressed => IsPressed && Time.frameCount - _pressedFrame <= 1;
    private ISelector _source;
    private bool _trigger, _armed;
    private int _pressedFrame = -100;
    private WindowHandle _owner;

    public void Bind(UnityEngine.Object value)
    {
        Unbind(); selector = value; _source = value as ISelector;
        _armed = false; _trigger = false; IsPressed = false;
        if (_source != null) { _source.WhenSelected += Press; _source.WhenUnselected += Release; }
    }
    private void OnEnable() => Bind(selector);
    private void OnDisable() { Unbind(); IsPressed = false; _owner = null; }
    private void Unbind()
    {
        if (_source != null) { _source.WhenSelected -= Press; _source.WhenUnselected -= Release; }
        _source = null;
    }
    private void Press() { _trigger = true; Sample(); }
    private void Release() { _trigger = false; Sample(); }
    public void Sample()
    {
        var behaviour = selector as Behaviour;
        if (_source == null || behaviour == null || !behaviour.isActiveAndEnabled)
        { _armed = false; _trigger = false; IsPressed = false; return; }
        var controller = selector as ControllerSelector;
        bool grip = controller != null && controller.Controller != null && controller.Controller.IsConnected &&
            controller.Controller.IsButtonUsageAnyActive(ControllerButtonUsage.GripButton);
        bool held = _trigger || grip;
        if (!held) _armed = true;
        if (held && !IsPressed && _armed) _pressedFrame = Time.frameCount;
        IsPressed = held;
    }
    public bool TryClaim(WindowHandle window)
    {
        Sample();
        if (!JustPressed || (_owner != null && _owner != window)) return false;
        _owner = window; return true;
    }
    public void ReleaseClaim(WindowHandle window) { if (_owner == window) _owner = null; }
    private void Update() => Sample();
}
