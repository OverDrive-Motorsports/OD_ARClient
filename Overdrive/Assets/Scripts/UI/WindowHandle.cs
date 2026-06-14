using UnityEngine;

/// <summary>
/// Attach to any World-Space Canvas to get a Meta-style draggable pill handle.
///
/// Grab modes:
///   - RAY GRAB  : ISDK ray points at the pill → pinch to grab (default, far-field)
///   - PROXIMITY : fingertip physically within proximityRadius of pill (close-range)
///
/// The pill is on the "Ignore Raycast" layer so the ISDK ray passes through it
/// without interference. Hit detection is done geometrically (no physics needed).
/// </summary>
[RequireComponent(typeof(Canvas))]
public class WindowHandle : MonoBehaviour
{
    [Header("Pill Appearance")]
    public float pillWidth     = 0.12f;   // world-space metres
    public float pillThickness = 0.014f;  // world-space metres
    public float belowOffset   = 0.022f;  // gap below canvas bottom, in metres

    public Color idleColor  = new Color(1f,    1f,    1f,    0.85f);
    public Color hoverColor = new Color(0.85f, 0.70f, 0.20f, 1f);  // gold on hover
    public Color grabColor  = new Color(1f,    0.85f, 0.35f, 1f);  // bright on grab

    [Header("Grab Detection")]
    [Tooltip("Radius (metres) for geometric ray-to-pill hit detection")]
    public float rayHoverRadius  = 0.05f;
    [Tooltip("Radius (metres) for fingertip proximity grab")]
    public float proximityRadius = 0.06f;

    // ── runtime ───────────────────────────────────────────────────────────────
    private Transform _pill;
    private Material  _pillMat;

    private bool    _grabbed;
    private bool    _isRayGrab;

    // Proximity grab state
    private Vector3 _grabberPosAtGrab;
    private Vector3 _canvasPosAtGrab;

    // Ray grab state
    private float   _grabRayDist;
    private Vector3 _grabOffsetFromHit;

    private OVRHand _leftHand, _rightHand;
    private OVRHand _activeHand;
    private bool    _controllerGrab;

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        BuildPill();
        FindHands();
    }

    private void Update()
    {
        if (_grabbed) UpdateDrag();
        else          CheckGrabStart();
    }

    // ── pill construction ─────────────────────────────────────────────────────
    private void BuildPill()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "WindowHandlePill";

        // Layer 2 = "Ignore Raycast" → ISDK ray passes straight through, no interference
        go.layer = 2;

        // DestroyImmediate = synchronous, collider gone this frame (no 1-frame ray block)
        DestroyImmediate(go.GetComponent<Collider>());

        go.transform.SetParent(transform, false);

        // Canvas lives at world scale ~0.001.
        // inv = local units per world metre
        float inv   = 1f / Mathf.Max(transform.lossyScale.x, 0.0001f);
        float halfH = GetComponent<RectTransform>().rect.height * 0.5f;

        go.transform.localPosition = new Vector3(0f, -halfH - belowOffset * inv, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // lay flat → pill shape
        go.transform.localScale    = new Vector3(
            pillThickness * inv * 0.5f,
            pillWidth     * inv * 0.5f,
            pillThickness * inv * 0.5f
        );

        _pillMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Unlit/Color"));
        _pillMat.color = idleColor;
        go.GetComponent<Renderer>().material = _pillMat;
        _pill = go.transform;
    }

    // ── grab detection ────────────────────────────────────────────────────────
    private void CheckGrabStart()
    {
        bool anyHover = false;

        // ── Hand tracking ─────────────────────────────────────────────────────
        foreach (var hand in new[] { _leftHand, _rightHand })
        {
            if (hand == null || !hand.IsTracked) continue;

            bool pinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);

            // --- Ray mode (far-field, ISDK pointer ray) -----------------------
            if (hand.IsPointerPoseValid)
            {
                Vector3 rayOrigin = hand.PointerPose.position;
                Vector3 rayDir    = hand.PointerPose.forward.normalized;

                if (RayNearPill(rayOrigin, rayDir, out float hitDist))
                {
                    anyHover = true;
                    if (pinching)
                    {
                        Vector3 hitPoint = rayOrigin + rayDir * hitDist;
                        BeginRayGrab(hand, hitDist, hitPoint);
                        return;
                    }
                }
            }

            // --- Proximity mode (fingertip near pill) -------------------------
            Vector3 tip = GetIndexTip(hand);
            if (Vector3.Distance(tip, _pill.position) < proximityRadius)
            {
                anyHover = true;
                if (pinching) { BeginProximityGrab(hand, tip); return; }
            }
        }

        // ── Controllers (grip trigger) ────────────────────────────────────────
        foreach (var ctrl in new[] { OVRInput.Controller.LTouch, OVRInput.Controller.RTouch })
        {
            Vector3 ctrlPos = OVRInput.GetLocalControllerPosition(ctrl);
            if (Vector3.Distance(ctrlPos, _pill.position) < proximityRadius)
            {
                anyHover = true;
                if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, ctrl))
                {
                    BeginProximityGrab(null, ctrlPos);
                    _controllerGrab = true;
                    return;
                }
            }
        }

        SetPillColor(anyHover ? hoverColor : idleColor);
    }

    // ── begin grab ────────────────────────────────────────────────────────────
    private void BeginRayGrab(OVRHand hand, float hitDist, Vector3 hitPoint)
    {
        _grabbed           = true;
        _isRayGrab         = true;
        _activeHand        = hand;
        _grabRayDist       = hitDist;
        _grabOffsetFromHit = transform.position - hitPoint;
        SetPillColor(grabColor);
    }

    private void BeginProximityGrab(OVRHand hand, Vector3 grabberPos)
    {
        _grabbed          = true;
        _isRayGrab        = false;
        _activeHand       = hand;
        _grabberPosAtGrab = grabberPos;
        _canvasPosAtGrab  = transform.position;
        SetPillColor(grabColor);
    }

    // ── drag update ───────────────────────────────────────────────────────────
    private void UpdateDrag()
    {
        if (_isRayGrab) UpdateRayDrag();
        else            UpdateProximityDrag();
    }

    private void UpdateRayDrag()
    {
        if (_activeHand == null || !_activeHand.IsTracked
            || !_activeHand.GetFingerIsPinching(OVRHand.HandFinger.Index)
            || !_activeHand.IsPointerPoseValid)
        {
            EndGrab(); return;
        }

        Vector3 curOrigin = _activeHand.PointerPose.position;
        Vector3 curDir    = _activeHand.PointerPose.forward.normalized;

        // Canvas follows the ray at the same distance as when grabbed
        transform.position = curOrigin + curDir * _grabRayDist + _grabOffsetFromHit;
    }

    private void UpdateProximityDrag()
    {
        Vector3 curPos;
        bool    stillGrabbing;

        if (_controllerGrab)
        {
            bool lG = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
            bool rG = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
            if      (lG) { curPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch); stillGrabbing = true; }
            else if (rG) { curPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch); stillGrabbing = true; }
            else         { curPos = _grabberPosAtGrab; stillGrabbing = false; }
        }
        else if (_activeHand != null && _activeHand.IsTracked)
        {
            curPos        = GetIndexTip(_activeHand);
            stillGrabbing = _activeHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        }
        else { curPos = _grabberPosAtGrab; stillGrabbing = false; }

        if (!stillGrabbing) { EndGrab(); return; }

        transform.position = _canvasPosAtGrab + (curPos - _grabberPosAtGrab);
    }

    private void EndGrab()
    {
        _grabbed = _isRayGrab = _controllerGrab = false;
        _activeHand = null;
        SetPillColor(idleColor);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// True if the ray passes within rayHoverRadius of the pill centre.
    private bool RayNearPill(Vector3 origin, Vector3 dir, out float hitDist)
    {
        Vector3 toPill = _pill.position - origin;
        hitDist = Vector3.Dot(toPill, dir);
        if (hitDist < 0.05f) return false;
        return Vector3.Distance(origin + dir * hitDist, _pill.position) < rayHoverRadius;
    }

    private void FindHands()
    {
        foreach (var h in FindObjectsByType<OVRHand>(FindObjectsSortMode.None))
        {
            var skel  = h.GetComponent<OVRSkeleton>();
            bool isLeft = skel != null
                ? skel.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft
                : h.gameObject.name.ToLower().Contains("left");

            if (isLeft) _leftHand  = h;
            else        _rightHand = h;
        }
    }

    private Vector3 GetIndexTip(OVRHand hand)
    {
        var skel = hand.GetComponent<OVRSkeleton>();
        if (skel != null)
        {
            var bones = skel.Bones;
            for (int i = 0; i < bones.Count; i++)
                if (bones[i].Id == OVRSkeleton.BoneId.Hand_IndexTip)
                    return bones[i].Transform.position;
        }
        return hand.transform.position;
    }

    private void SetPillColor(Color c)
    {
        if (_pillMat != null) _pillMat.color = c;
    }
}
