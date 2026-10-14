/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## CircuitManipulator - Lets the user handle the 3D circuit miniature with the
 ## same ISDK rays used everywhere else (hands: pinch, controllers: trigger):
 ##   - one ray, drag sideways: spin (keeps its momentum when released);
 ##   - two rays: pull apart / together to zoom, twist to rotate;
 ##   - controller thumbstick while aiming at the circuit: X spins, Y zooms.
 ## Every value is smoothed so the model glides like a visionOS volume.
 ##
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Oculus.Interaction;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CircuitManipulator : MonoBehaviour
{
    [Tooltip("Transform that carries the model (spun and scaled around its origin).")]
    public Transform target;
    public float minScale = .5f, maxScale = 5f;
    [Tooltip("Degrees of spin per metre of sideways ray motion.")]
    public float spinPerMetre = 420f;

    const float Smoothing = 16f;       // 1/s, how fast the model reaches its target pose
    const float MomentumDecay = 3.5f;  // 1/s, how fast a released spin slows down
    const float StickDeadZone = .2f;


    sealed class Pointer
    {
        public RayInteractor ray;
        public ISelector selector;
        public bool selected, pressedThisFrame, grabbing, hovering;
        public Action OnSelected, OnUnselected;
        public float grabDistance, restoreLength;
        public bool lengthOverridden;
        public OVRInput.Controller controller = OVRInput.Controller.None;
        public Vector3 lastPoint;
    }

    readonly List<Pointer> _pointers = new List<Pointer>();
    float _nextScan;

    Bounds _box;   // model bounds in target space (constant: the model never moves inside the target)
    bool _hasBox;

    float _yaw, _scale = 1f;                      // targets
    float _shownYaw, _shownScale = 1f;
    float _spinVelocity;                          // deg/s, for momentum after release

    // Two-hand gesture baseline.
    bool _twoHand;
    Vector3 _pairStart;
    float _pairYawStart, _pairScaleStart;


    static PropertyInfo _selectorProperty;

    public bool IsHandled
    {
        get { foreach (var p in _pointers) if (p.grabbing) return true; return false; }
    }

    /// <summary>
    /// The model inside <see cref="target"/> changed: <paramref name="bounds"/> are its
    /// mesh bounds in target space (null when there is no model).
    /// </summary>
    public void SetModelBounds(Bounds? bounds)
    {
        _hasBox = bounds.HasValue;
        if (_hasBox)
        {
            // Flat tracks are hard to aim at: give the box some height and a small margin.
            var size = bounds.Value.size;
            size.x *= 1.06f; size.z *= 1.06f;
            size.y = Mathf.Max(size.y, 60f);
            _box = new Bounds(bounds.Value.center, size);
        }
    }

    public void ResetView(bool instant)
    {
        _yaw = 0f;
        _scale = 1f;
        _spinVelocity = 0f;
        if (instant) { _shownYaw = 0f; _shownScale = 1f; Apply(); }
    }

    public void Spin(float degrees) { _spinVelocity = 0f; _yaw += degrees; }
    public void Zoom(float factor) => _scale = Mathf.Clamp(_scale * factor, minScale, maxScale);

    /// <summary>Zoom as 0..1 on a log scale (even steps along a slider), 1x sits in between.</summary>
    public float NormalizedZoom
    {
        get => Mathf.InverseLerp(Mathf.Log(minScale), Mathf.Log(maxScale), Mathf.Log(_scale));
        set => _scale = Mathf.Exp(Mathf.Lerp(Mathf.Log(minScale), Mathf.Log(maxScale), Mathf.Clamp01(value)));
    }

    void OnDisable()
    {
        foreach (var p in _pointers) Release(p);
        _twoHand = false;
    }

    void OnDestroy()
    {
        foreach (var p in _pointers)
            if (p.selector != null) { p.selector.WhenSelected -= p.OnSelected; p.selector.WhenUnselected -= p.OnUnselected; }
    }

    void Update()
    {
        if (target == null) return;
        float dt = ODTime.Delta;

        ScanRays();
        UpdatePointers();
        UpdateGestures(dt);
        UpdateSticks(dt);

        // Momentum after a single-hand spin.
        if (!IsHandled && Mathf.Abs(_spinVelocity) > .5f)
        {
            _yaw += _spinVelocity * dt;
            _spinVelocity *= Mathf.Exp(-MomentumDecay * dt);
        }

        float k = 1f - Mathf.Exp(-Smoothing * dt);
        _shownYaw   = Mathf.Lerp(_shownYaw, _yaw, k);
        _shownScale = Mathf.Lerp(_shownScale, _scale, k);
        Apply();
    }

    void Apply()
    {
        target.localRotation = Quaternion.AngleAxis(_shownYaw, Vector3.up);
        target.localScale = Vector3.one * _shownScale;
    }

    // ── Rays ────────────────────────────────────────────────────────────────

    void ScanRays()
    {
        if (ODTime.Now < _nextScan) return;
        _nextScan = ODTime.Now + 2f;   // the rig can spawn interactors late

        foreach (var ray in FindObjectsByType<RayInteractor>(FindObjectsInactive.Include))
        {
            if (_pointers.Exists(p => p.ray == ray)) continue;
            var pointer = new Pointer { ray = ray, controller = ControllerOf(ray) };
            _pointers.Add(pointer);
        }
    }

    void UpdatePointers()
    {
        foreach (var p in _pointers)
        {
            if (p.ray == null || !p.ray.isActiveAndEnabled) { Release(p); p.selected = false; continue; }
            if (p.selector == null) Subscribe(p);

            Ray ray = p.ray.Ray;
            bool hit = HitModel(ray, out float distance);

            // Something interactive (a window) closer than the circuit keeps the ray.
            if (hit && p.ray.CollisionInfo.HasValue && !p.lengthOverridden)
            {
                float uiDistance = Vector3.Distance(ray.origin, p.ray.CollisionInfo.Value.Point);
                if (uiDistance < distance) hit = false;
            }
            p.hovering = hit;

            // While aiming at the circuit, the ray stops on it: no click on a window behind.
            if (hit || p.grabbing) ShortenRay(p, p.grabbing ? p.grabDistance : distance);
            else RestoreRay(p);

            if (!p.grabbing && hit && p.selected && p.pressedThisFrame)
            {
                p.grabbing = true;
                p.grabDistance = distance;
                p.lastPoint = ray.GetPoint(distance);
                _twoHand = false;   // re-baseline
            }
            else if (p.grabbing && !p.selected)
            {
                Release(p);
            }
            p.pressedThisFrame = false;
        }
    }

    void UpdateGestures(float dt)
    {
        Pointer a = null, b = null;
        foreach (var p in _pointers)
        {
            if (!p.grabbing) continue;
            if (a == null) a = p; else if (b == null) b = p;
        }

        if (a != null && b != null)
        {
            Vector3 pa = a.ray.Ray.GetPoint(a.grabDistance), pb = b.ray.Ray.GetPoint(b.grabDistance);
            Vector3 pair = Vector3.ProjectOnPlane(pb - pa, transform.up);
            if (!_twoHand)
            {
                _twoHand = true;
                _pairStart = pair;
                _pairYawStart = _yaw;
                _pairScaleStart = _scale;
                _spinVelocity = 0f;
            }
            if (_pairStart.magnitude > .02f && pair.magnitude > .02f)
            {
                _scale = Mathf.Clamp(_pairScaleStart * pair.magnitude / _pairStart.magnitude, minScale, maxScale);
                _yaw = _pairYawStart - Vector3.SignedAngle(_pairStart, pair, transform.up);
            }
            a.lastPoint = pa; b.lastPoint = pb;
            return;
        }
        _twoHand = false;
        if (a == null) return;

        // One hand: sideways motion in the user's view spins the circuit like a turntable.
        Vector3 point = a.ray.Ray.GetPoint(a.grabDistance);
        Vector3 delta = point - a.lastPoint;
        a.lastPoint = point;

        var head = Camera.main != null ? Camera.main.transform : transform;
        Vector3 right = Vector3.ProjectOnPlane(head.right, Vector3.up).normalized;
        float spin = -Vector3.Dot(delta, right) * spinPerMetre;
        _yaw += spin;
        if (dt > 0f) _spinVelocity = Mathf.Lerp(_spinVelocity, spin / dt, .35f);
    }

    void UpdateSticks(float dt)
    {
        foreach (var p in _pointers)
        {
            if (p.controller == OVRInput.Controller.None || !(p.hovering || p.grabbing)) continue;
            Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, p.controller);
            if (Mathf.Abs(stick.x) > StickDeadZone) { _spinVelocity = 0f; _yaw -= stick.x * 120f * dt; }
            if (Mathf.Abs(stick.y) > StickDeadZone) _scale = Mathf.Clamp(_scale * (1f + stick.y * 1.2f * dt), minScale, maxScale);
        }
    }

    void Release(Pointer p)
    {
        p.grabbing = false;
        p.hovering = false;
        RestoreRay(p);
    }

    void ShortenRay(Pointer p, float distance)
    {
        if (!p.lengthOverridden) { p.restoreLength = p.ray.MaxRayLength; p.lengthOverridden = true; }
        p.ray.MaxRayLength = Mathf.Max(.05f, distance - .01f);
    }

    static void RestoreRay(Pointer p)
    {
        if (!p.lengthOverridden || p.ray == null) return;
        p.ray.MaxRayLength = p.restoreLength;
        p.lengthOverridden = false;
    }

    /// <summary>Ray against the model's box, in target space (handles rotation and zoom).</summary>
    bool HitModel(Ray ray, out float distance)
    {
        distance = 0f;
        if (!_hasBox || !target.gameObject.activeInHierarchy) return false;
        Vector3 o = target.InverseTransformPoint(ray.origin);
        Vector3 d = target.InverseTransformVector(ray.direction.normalized);   // keeps t in world metres
        Vector3 min = _box.min, max = _box.max;
        float tMin = 0f, tMax = float.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            if (Mathf.Abs(d[i]) < 1e-8f)
            {
                if (o[i] < min[i] || o[i] > max[i]) return false;
                continue;
            }
            float t1 = (min[i] - o[i]) / d[i], t2 = (max[i] - o[i]) / d[i];
            if (t1 > t2) { float s = t1; t1 = t2; t2 = s; }
            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            if (tMin > tMax) return false;
        }
        distance = tMin;
        return distance > .05f;
    }

    // ── Selection (pinch / trigger) per ray ─────────────────────────────────

    void Subscribe(Pointer p)
    {
        if (_selectorProperty == null)
            _selectorProperty = typeof(RayInteractor).GetProperty("Selector", BindingFlags.Instance | BindingFlags.NonPublic);
        p.selector = _selectorProperty != null ? _selectorProperty.GetValue(p.ray) as ISelector : null;
        if (p.selector == null) return;
        p.OnSelected = () => { p.selected = true; p.pressedThisFrame = true; };
        p.OnUnselected = () => p.selected = false;
        p.selector.WhenSelected += p.OnSelected;
        p.selector.WhenUnselected += p.OnUnselected;
    }

    static OVRInput.Controller ControllerOf(RayInteractor ray)
    {
        string path = "";
        for (var t = ray.transform; t != null; t = t.parent) path = t.name + "/" + path;
        if (path.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) < 0 || path.Contains("Hand and No Controller")) return OVRInput.Controller.None;
        return path.Contains("Left") ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
    }
}
