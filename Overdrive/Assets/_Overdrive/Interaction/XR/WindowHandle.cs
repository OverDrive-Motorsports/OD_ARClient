using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Attach to any World-Space Canvas to get a Meta-style draggable pill handle.
/// Point the ISDK ray at the pill and pinch (hands) or grip (controllers) to
/// grab; the whole window then follows your ray. Uses the SAME ray you see
/// (Oculus.Interaction.RayInteractor), so detection always matches the visuals.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class WindowHandle : MonoBehaviour
{
    [Header("Pill Appearance")]
    public float pillWidth     = 0.12f;   // world-space metres
    public float pillThickness = 0.014f;
    public float belowOffset   = 0.022f;

    public Color idleColor  = new Color(1f,    1f,    1f,    0.85f);
    public Color hoverColor = new Color(0.85f, 0.70f, 0.20f, 1f);
    public Color grabColor  = new Color(1f,    0.85f, 0.35f, 1f);

    [Header("Detection")]
    [Tooltip("Radius (m) around the pill the ray must pass within to grab")]
    public float rayHoverRadius  = 0.06f;
    [Tooltip("Fingertip proximity grab radius (m)")]
    public float proximityRadius = 0.07f;

    // ── runtime ───────────────────────────────────────────────────────────────
    private Transform _pill;
    private Material  _pillMat;

    private bool         _grabbed;
    private RayInteractor _grabRay;
    private float        _grabDist;
    private Vector3      _grabOffset;     // canvas pos - ray hit point

    // proximity grab
    private bool    _proxGrab;
    private OVRHand _proxHand;
    private Vector3 _proxStartFinger, _proxStartCanvas;

    private RayInteractor[] _rays;
    private OVRHand[]       _hands;

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        BuildPill();
        _rays  = FindObjectsByType<RayInteractor>(FindObjectsSortMode.None);
        _hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        // refresh references if the rig spawned interactors after us
        if (_rays == null || _rays.Length == 0)
            _rays = FindObjectsByType<RayInteractor>(FindObjectsSortMode.None);

        if (_grabbed) UpdateDrag();
        else          CheckGrabStart();
    }

    // ── pill ────────────────────────────────────────────────────────────────────
    private void BuildPill()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name  = "WindowHandlePill";
        go.layer = 2; // Ignore Raycast → ISDK ray passes through, stays visible

        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        go.transform.SetParent(transform, false);

        float inv  = 1f / Mathf.Max(transform.lossyScale.x, 0.0001f);
        float halfH = GetComponent<RectTransform>().rect.height * 0.5f;

        go.transform.localPosition = new Vector3(0f, -halfH - belowOffset * inv, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        go.transform.localScale    = new Vector3(
            pillThickness * inv * 0.5f,
            pillWidth     * inv * 0.5f,
            pillThickness * inv * 0.5f);

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        _pillMat = new Material(shader);
        _pillMat.color = idleColor;
        go.GetComponent<Renderer>().material = _pillMat;
        _pill = go.transform;
    }

    // ── grab detection ────────────────────────────────────────────────────────
    private void CheckGrabStart()
    {
        bool pinch = PinchActive();
        bool hover = false;

        // RAY: use each ISDK ray interactor (matches the visible laser)
        if (_rays != null)
        {
            foreach (var ray in _rays)
            {
                if (ray == null || !ray.isActiveAndEnabled) continue;

                Vector3 o = ray.Origin;
                Vector3 d = ray.Forward;
                if (d.sqrMagnitude < 0.001f) { d = ray.transform.forward; o = ray.transform.position; }

                if (!RayNearPill(o, d, out float hit)) continue;
                hover = true;

                if (pinch)
                {
                    Vector3 hitPoint = o + d.normalized * hit;
                    _grabbed   = true;
                    _proxGrab  = false;
                    _grabRay   = ray;
                    _grabDist  = hit;
                    _grabOffset = transform.position - hitPoint;
                    SetColor(grabColor);
                    return;
                }
            }
        }

        // PROXIMITY: fingertip near the pill
        if (_hands != null)
        {
            foreach (var hand in _hands)
            {
                if (hand == null || !hand.IsTracked) continue;
                Vector3 tip = IndexTip(hand);
                if (Vector3.Distance(tip, _pill.position) > proximityRadius) continue;
                hover = true;

                if (hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
                {
                    _grabbed = true; _proxGrab = true;
                    _proxHand = hand;
                    _proxStartFinger = tip;
                    _proxStartCanvas = transform.position;
                    SetColor(grabColor);
                    return;
                }
            }
        }

        SetColor(hover ? hoverColor : idleColor);
    }

    // ── drag ──────────────────────────────────────────────────────────────────
    private void UpdateDrag()
    {
        if (!PinchActive()) { EndGrab(); return; }

        if (_proxGrab)
        {
            if (_proxHand == null || !_proxHand.IsTracked) { EndGrab(); return; }
            Vector3 tip = IndexTip(_proxHand);
            transform.position = _proxStartCanvas + (tip - _proxStartFinger);
            FaceUser();
            return;
        }

        if (_grabRay == null) { EndGrab(); return; }
        Vector3 o = _grabRay.Origin;
        Vector3 d = _grabRay.Forward;
        if (d.sqrMagnitude < 0.001f) { d = _grabRay.transform.forward; o = _grabRay.transform.position; }
        Vector3 hitPoint = o + d.normalized * _grabDist;
        transform.position = hitPoint + _grabOffset;
        FaceUser();
    }

    /// <summary>
    /// Meta-style billboard: while dragged, the window continuously yaws
    /// to face the user (stays upright, no pitch/roll).
    /// </summary>
    private void FaceUser()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 away = transform.position - cam.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0025f) return; // too close, keep rotation

        transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
    }

    private void EndGrab()
    {
        _grabbed = false; _proxGrab = false;
        _grabRay = null;  _proxHand = null;
        SetColor(idleColor);
    }

    // ── helpers ───────────────────────────────────────────────────────────────
    private bool PinchActive()
    {
        if (_hands != null)
            foreach (var h in _hands)
                if (h != null && h.IsTracked && h.GetFingerIsPinching(OVRHand.HandFinger.Index))
                    return true;

        return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch)
            || OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch)
            || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)
            || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
    }

    private bool RayNearPill(Vector3 origin, Vector3 dir, out float hitDist)
    {
        dir = dir.normalized;
        Vector3 toPill = _pill.position - origin;
        hitDist = Vector3.Dot(toPill, dir);
        if (hitDist < 0.05f) return false;
        return Vector3.Distance(origin + dir * hitDist, _pill.position) < rayHoverRadius;
    }

    private Vector3 IndexTip(OVRHand hand)
    {
        var skel = hand.GetComponent<OVRSkeleton>();
        if (skel != null)
            foreach (var b in skel.Bones)
                if (b.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                    return b.Transform.position;
        return hand.transform.position;
    }

    private void SetColor(Color c) { if (_pillMat != null) _pillMat.color = c; }
}
