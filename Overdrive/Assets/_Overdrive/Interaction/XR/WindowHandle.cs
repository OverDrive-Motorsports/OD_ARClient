using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    public float pillWidth = 0.12f;   // world-space metres
    public float pillThickness = 0.014f;
    public float belowOffset = 0.022f;
    [Tooltip("Use a local-space handle position for a horizontal tabletop model.")]
    public bool customPillPosition;
    public Vector3 pillLocalPosition;
    [Tooltip("Visible window inside a larger canvas: the pill follows its bottom edge (visionOS-style) instead of the canvas'.")]
    public RectTransform followTarget;
    [Tooltip("Gap (m) between the followed window's bottom edge and the pill.")]
    public float followGap = 0.02f;
    /// <summary>
    /// Design window bar (Canva "Window, bar & ornament"): 320 x 18 px under a
    /// 1920-px window, i.e. 1/6 of the window width, white 62 % at rest. Same on every
    /// window; a constant so stale serialized scene values can't override it.
    /// </summary>
    static readonly Vector2 BarSize = new Vector2(0.224f, 0.0126f);
    static readonly Color BarIdle = new Color(1f, 1f, 1f, .62f);
    static readonly Color BarHover = new Color(1f, 1f, 1f, .90f);
    static readonly Color BarGrab = new Color(1f, 1f, 1f, 1f);

    public Color idleColor = new Color(1f, 1f, 1f, 0.85f);
    public Color hoverColor = new Color(0.85f, 0.70f, 0.20f, 1f);
    public Color grabColor = new Color(1f, 0.85f, 0.35f, 1f);

    [Header("Detection")]
    [Tooltip("Radius (m) around the pill the ray must pass within to grab")]
    public float rayHoverRadius = 0.06f;
    [Tooltip("Fingertip proximity grab radius (m)")]
    public float proximityRadius = 0.07f;

    /// <summary>A ray or fingertip is on the bar (ornaments appear while it is).</summary>
    public bool IsHovered { get; private set; }
    public bool IsGrabbed => _grabbed;
    /// <summary>Independent windows can detach from a group exactly when their own bar is grabbed.</summary>
    public event System.Action GrabStarted;

    // ── runtime ───────────────────────────────────────────────────────────────
    private Transform _pill;
    private RoundedImage _pillImage;

    private bool _grabbed;
    private RayInteractor _grabRay;
    private ODWindowGrabInput _grabInput;
    private float _grabDist;
    private Vector3 _localGripPoint;

    // proximity grab
    private bool _proxGrab;
    private OVRHand _proxHand;
    private readonly Dictionary<OVRHand, bool> _handPressed = new Dictionary<OVRHand, bool>();

    private RayInteractor[] _rays;
    private OVRHand[] _hands;

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        BuildPill();
        // Keep both input modes: the rig enables controllers/hands alternately.
        _rays = FindObjectsByType<RayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _hands = FindObjectsByType<OVRHand>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        PrimeHands();
    }

    private void OnEnable() => PrimeHands();
    private void PrimeHands()
    {
        _handPressed.Clear();
        if (_hands != null) foreach (var hand in _hands)
            if (hand != null) _handPressed[hand] = hand.IsTracked && hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
    }

    private void Update()
    {
        // refresh references if the rig spawned interactors after us
        if (_rays == null || _rays.Length == 0)
            _rays = FindObjectsByType<RayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (_grabbed) UpdateDrag();
        else CheckGrabStart();
    }

    private void OnDisable()
    {
        // A window hidden during a grab must not keep following the old ray
        // when it opens again (notably the circuit preview next to the player).
        EndGrab();
    }

    private void LateUpdate() => FollowTarget();

    // ── pill ────────────────────────────────────────────────────────────────────
    /// <summary>
    /// visionOS window bar: a flat UI capsule (fully rounded ends) drawn on the
    /// canvas itself, not a 3D cylinder. Never a raycast target — grabbing is
    /// detected geometrically below, so the ISDK ray still reaches the UI.
    /// </summary>
    private void BuildPill()
    {
        var go = new GameObject("WindowHandlePill", typeof(RectTransform));
        go.layer = 2; // Ignore Raycast
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);

        float inv = 1f / Mathf.Max(transform.lossyScale.x, 0.0001f);
        rect.sizeDelta = BarSize * inv;   // same bar on every window

        float halfH = GetComponent<RectTransform>().rect.height * 0.5f;
        rect.localPosition = customPillPosition ? pillLocalPosition : new Vector3(0f, -halfH - followGap * inv, 0f);

        _pillImage = go.AddComponent<RoundedImage>();
        _pillImage.cornerRadius = BarSize.y * inv * .5f;
        _pillImage.cornerSegments = 12;
        _pillImage.raycastTarget = false;
        _pill = go.transform;
        SetColor(idleColor);
        FollowTarget();
    }

    /// <summary>Keeps the pill centred just under the followed window, whose size changes between pages.</summary>
    private void FollowTarget()
    {
        if (_pill == null || followTarget == null || customPillPosition || !followTarget.gameObject.activeInHierarchy) return;

        var corners = new Vector3[4];
        followTarget.GetWorldCorners(corners);   // 0 = bottom-left, 3 = bottom-right
        Vector3 bottomCentre = transform.InverseTransformPoint((corners[0] + corners[3]) * .5f);
        float inv = 1f / Mathf.Max(transform.lossyScale.x, 0.0001f);
        _pill.localPosition = new Vector3(bottomCentre.x, bottomCentre.y - followGap * inv, 0f);
    }

    // ── grab detection ────────────────────────────────────────────────────────
    private void CheckGrabStart()
    {
        bool hover = false;

        // RAY: use each ISDK ray interactor (matches the visible laser)
        if (_rays != null)
        {
            foreach (var ray in _rays)
            {
                if (ray == null || !ray.isActiveAndEnabled) continue;
                var input = ray.GetComponent<ODWindowGrabInput>();
                if (input == null) continue;
                input.Sample();

                Vector3 o = ray.Origin;
                Vector3 d = ray.Forward;
                if (d.sqrMagnitude < 0.001f) { d = ray.transform.forward; o = ray.transform.position; }

                if (!RayNearPill(o, d, out float hit)) continue;
                hover = true;

                if (input.TryClaim(this))
                {
                    Vector3 hitPoint = o + d.normalized * hit;
                    _grabbed = true;
                    _proxGrab = false;
                    _grabRay = ray;
                    _grabInput = input;
                    _grabDist = hit;
                    _localGripPoint = transform.InverseTransformPoint(hitPoint);
                    GrabStarted?.Invoke();
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
                bool pinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
                bool wasPressed;
                _handPressed.TryGetValue(hand, out wasPressed);
                _handPressed[hand] = pinching;
                Vector3 tip = IndexTip(hand);
                if (Vector3.Distance(tip, ClosestPillPoint(tip)) > proximityRadius) continue;
                hover = true;

                if (pinching && !wasPressed)
                {
                    _grabbed = true; _proxGrab = true;
                    _proxHand = hand;
                    _localGripPoint = transform.InverseTransformPoint(tip);
                    GrabStarted?.Invoke();
                    SetColor(grabColor);
                    return;
                }
            }
        }

        IsHovered = hover;
        SetColor(hover ? hoverColor : idleColor);
    }

    // ── drag ──────────────────────────────────────────────────────────────────
    private void UpdateDrag()
    {
        if (_proxGrab)
        {
            if (_proxHand == null || !_proxHand.IsTracked || !_proxHand.GetFingerIsPinching(OVRHand.HandFinger.Index)) { EndGrab(); return; }
            Vector3 tip = IndexTip(_proxHand);
            MoveFromGrip(tip);
            return;
        }

        if (_grabRay == null || !_grabRay.isActiveAndEnabled || _grabInput == null) { EndGrab(); return; }
        _grabInput.Sample();
        if (!_grabInput.IsPressed) { EndGrab(); return; }
        Vector3 o = _grabRay.Origin;
        Vector3 d = _grabRay.Forward;
        if (d.sqrMagnitude < 0.001f) { d = _grabRay.transform.forward; o = _grabRay.transform.position; }
        Vector3 hitPoint = o + d.normalized * _grabDist;
        MoveFromGrip(hitPoint);
    }

    /// <summary>
    /// Meta-style billboard: while dragged, the window continuously yaws
    /// to face the user (stays upright, no pitch/roll).
    /// </summary>
    private void MoveFromGrip(Vector3 worldGrip)
    {
        Quaternion rotation = transform.rotation;
        var cam = Camera.main;
        if (cam != null)
        {
            var center = (Vector3)GetComponent<RectTransform>().rect.center;
            Vector3 desiredCenter = worldGrip + transform.TransformVector(center - _localGripPoint);
            Vector3 away = Vector3.ProjectOnPlane(desiredCenter - cam.transform.position, Vector3.up);
            if (away.sqrMagnitude >= .0025f)
                rotation = Quaternion.RotateTowards(rotation, Quaternion.LookRotation(away.normalized, Vector3.up), 110f * Time.deltaTime);
        }
        // Rotate about the actual grab point, not the top-left canvas origin. The bar stays on the hand/ray.
        transform.rotation = rotation;
        transform.position = worldGrip - transform.TransformVector(_localGripPoint);
    }

    private void EndGrab()
    {
        IsHovered = false;
        if (_grabInput != null) _grabInput.ReleaseClaim(this);
        _grabbed = false; _proxGrab = false;
        _grabRay = null; _grabInput = null; _proxHand = null;
        PrimeHands();
        SetColor(idleColor);
    }

    // ── helpers ───────────────────────────────────────────────────────────────
    private bool RayNearPill(Vector3 origin, Vector3 dir, out float hitDist)
    {
        dir = dir.normalized;
        hitDist = Vector3.Dot(_pill.position - origin, dir);
        if (hitDist < 0.05f) return false;
        var nearest = ClosestPillPoint(origin + dir * hitDist);
        hitDist = Vector3.Dot(nearest - origin, dir);
        return Vector3.Distance(origin + dir * hitDist, nearest) < rayHoverRadius;
    }

    private Vector3 ClosestPillPoint(Vector3 point)
    {
        var rect = _pill as RectTransform;
        float halfWidth = rect != null ? rect.rect.width * Mathf.Abs(rect.lossyScale.x) * .5f : .1f;
        return _pill.position + _pill.right * Mathf.Clamp(Vector3.Dot(point - _pill.position, _pill.right), -halfWidth, halfWidth);
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

    private void SetColor(Color c)
    {
        if (_pillImage == null) return;
        // Same on every window: white 62 % at rest, lit on gaze, full white while grabbed.
        _pillImage.color = c == idleColor ? BarIdle : c == grabColor ? BarGrab : BarHover;
    }
}
