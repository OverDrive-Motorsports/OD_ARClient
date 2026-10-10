using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>
/// Lives on an always-active parent, not on the window it hides. Uses the left
/// tracked hand or Touch grip pose and never moves the content window.
/// </summary>
[DefaultExecutionOrder(100)]
public class LeftWristNavigation : MonoBehaviour
{
    public FloatingNavigation navigation;
    public OVRCameraRig cameraRig;
    public Hand leftHand;

    [Header("Wrist placement (metres)")]
    [Min(.1f)] public float width = .36f;
    public float abovePalm = .08f;
    public float towardViewer = .04f;
    [Tooltip("Estimated wrist relative to the left Touch grip pose.")]
    public Vector3 controllerWristOffset = new Vector3(0, -.04f, -.06f);
    [Tooltip("Palm-facing direction in the left Touch grip frame; independent of the thumbstick face.")]
    public Vector3 controllerPalmAxis = Vector3.right;
    [Min(1f)] public float followSpeed = 18f;

    [Header("Palm-up gesture")]
    [Range(-1, 1)] public float showDot = .65f;
    [Range(-1, 1)] public float hideDot = .30f;
    [Min(0)] public float showDelay = .12f;
    [Min(0)] public float hideDelay = .15f;

    public bool IsVisible => _gate.Visible;
    public string TrackingSource { get; private set; } = "None";
    private readonly WristRevealGate _gate = new WristRevealGate();
    private WindowHandle _handle;
    private bool _handleWasEnabled;
    private Vector3 _originalScale;
    private OVRHand _rawHand;

    private void OnEnable()
    {
        if (navigation == null) return;
        _originalScale = navigation.transform.localScale;
        if (leftHand != null) _rawHand = leftHand.GetComponent<OVRHand>();
        _handle = navigation.GetComponent<WindowHandle>();
        if (_handle != null)
        {
            _handleWasEnabled = _handle.enabled;
            // Wrist following and free dragging must not drive the same transform.
            _handle.enabled = false;
            SetHandleVisible(false);
        }
        _gate.Reset();
        navigation.Hide();
    }

    private void OnDisable()
    {
        _gate.Reset();
        if (navigation != null) navigation.transform.localScale = _originalScale;
        if (_handle != null)
        {
            _handle.enabled = _handleWasEnabled;
            SetHandleVisible(_handleWasEnabled);
        }
    }

    private void SetHandleVisible(bool visible)
    {
        var pill = navigation.transform.Find("WindowHandlePill");
        if (pill != null) pill.gameObject.SetActive(visible);
    }

    private void LateUpdate()
    {
        if (navigation == null) return;
        if (cameraRig == null) cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (!TryGetWrist(out Vector3 position, out Vector3 palm))
        {
            TrackingSource = "None";
            _gate.Reset();
            navigation.Hide();
            return;
        }
        var eye = cameraRig != null ? cameraRig.centerEyeAnchor : null;
        if (eye == null) { _gate.Reset(); navigation.Hide(); return; }
        bool wasVisible = _gate.Visible;
        bool visible = _gate.Step(Vector3.Dot(palm, Vector3.up), Time.unscaledDeltaTime,
            showDot, hideDot, showDelay, hideDelay);
        if (!visible) { navigation.Hide(); return; }

        Vector3 towardEye = (eye.position - position).normalized;
        Vector3 target = position + palm * abovePalm + towardEye * towardViewer;
        Vector3 forward = target - eye.position;
        if (forward.sqrMagnitude < .0001f) { navigation.Hide(); return; }
        Vector3 up = Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > .95f
            ? eye.up : Vector3.up;
        Quaternion rotation = Quaternion.LookRotation(forward, up);
        Transform window = navigation.transform;
        float t = wasVisible ? 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime) : 1f;
        window.SetPositionAndRotation(Vector3.Lerp(window.position, target, t),
            Quaternion.Slerp(window.rotation, rotation, t));
        float units = ((RectTransform)window).rect.width;
        window.localScale = Vector3.one * (width / Mathf.Max(1, units));
        navigation.Show();
    }

    private bool TryGetWrist(out Vector3 position, out Vector3 palm)
    {
        position = Vector3.zero;
        palm = Vector3.up;
        // Query the LEFT device, so switching the right hand cannot change this menu.
        var device = OVRInput.GetActiveControllerForHand(OVRInput.Handedness.LeftHanded);
        // In simultaneous mode, hands may remain the "active" input even while
        // a Touch controller is held. Prefer that controller only when held (or
        // explicitly active), never just because it is connected on a table.
        bool leftControllerHeld = OVRInput.GetControllerIsInHandState(OVRInput.Hand.HandLeft)
            == OVRInput.ControllerInHandState.ControllerInHand;
        if (device == OVRInput.Controller.LTouch || leftControllerHeld)
        {
            device = OVRInput.Controller.LTouch;
            if (cameraRig == null || !OVRInput.GetControllerPositionTracked(device)
                || !OVRInput.GetControllerOrientationTracked(device)) return false;
            // Use raw grip pose in tracking space, without a visual-model offset.
            Transform space = cameraRig.trackingSpace;
            Quaternion rotation = space.rotation * OVRInput.GetLocalControllerRotation(device);
            position = space.TransformPoint(OVRInput.GetLocalControllerPosition(device))
                + rotation * controllerWristOffset;
            palm = (rotation * controllerPalmAxis).normalized;
            TrackingSource = "Left controller";
            return palm.sqrMagnitude > .5f;
        }
        // LastKnownGoodHand preserves stale poses and confidence; check the raw
        // sensor too so losing sight of the hand always closes the menu.
        if (_rawHand == null || !_rawHand.IsTracked || !_rawHand.IsDataHighConfidence
            || leftHand == null || !leftHand.isActiveAndEnabled || !leftHand.IsTrackedDataValid
            || !leftHand.IsHighConfidence || leftHand.Handedness != Handedness.Left
            || !leftHand.GetRootPose(out Pose wrist)) return false;
        position = wrist.position;
        // SDK constants select the correct convention for OVR/OpenXR hand skeletons.
        palm = wrist.rotation * Constants.LeftPalmar;
        TrackingSource = "Left hand";
        return true;
    }
}

/// <summary>Hysteresis plus dwell time, independent of the tracking provider.</summary>
public sealed class WristRevealGate
{
    public bool Visible { get; private set; }
    private float _elapsed;
    public void Reset() { Visible = false; _elapsed = 0; }

    public bool Step(float dot, float delta, float showDot, float hideDot, float showDelay, float hideDelay)
    {
        bool change = Visible ? dot < hideDot : dot >= showDot;
        if (!change) { _elapsed = 0; return Visible; }
        _elapsed += Mathf.Max(0, delta);
        if (_elapsed >= (Visible ? hideDelay : showDelay))
        {
            Visible = !Visible;
            _elapsed = 0;
        }
        return Visible;
    }
}
