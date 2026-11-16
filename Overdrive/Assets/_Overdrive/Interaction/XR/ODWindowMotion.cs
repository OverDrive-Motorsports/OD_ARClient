/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODWindowMotion - Glides a window to a new pose (position + rotation)
 ## with a critically damped spring: it starts at once, slows down softly and
 ## never overshoots. The component only runs while the window moves, then
 ## switches itself off; grabbing the window's bar stops the glide so the
 ## hand always wins. An optional follower (the floating navigation bar under
 ## the main window) keeps its place relative to the window as it moves.
 ##
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class ODWindowMotion : MonoBehaviour
{
    /// <summary>Seconds to cover most of the way (the spring's smooth time).</summary>
    const float SmoothTime = .22f;
    const float ArrivedDistance = .0005f, ArrivedAngle = .05f;

    Pose _target;
    Vector3 _velocity;
    float _angleVelocity;
    Transform _follower;
    Vector3 _followerOffset;
    Quaternion _followerRotation;
    WindowHandle _handle;
    bool _moving;

    /// <summary>Where the window is going (its current pose when it isn't moving).</summary>
    public Pose Target => _moving ? _target : new Pose(transform.position, transform.rotation);

    /// <summary>
    /// Sends <paramref name="window"/> to <paramref name="pose"/>: gliding when <paramref name="animate"/>,
    /// at once otherwise (a window that is just appearing). <paramref name="follower"/> moves with it.
    /// </summary>
    public static void MoveTo(Transform window, Pose pose, bool animate, Transform follower = null)
    {
        if (window == null) return;
        if (!window.TryGetComponent(out ODWindowMotion motion))
        {
            motion = window.gameObject.AddComponent<ODWindowMotion>();
            motion.enabled = false;
        }
        motion.Begin(pose, animate, follower);
    }

    void Begin(Pose pose, bool animate, Transform follower)
    {
        _target = pose;
        _follower = follower;
        if (_follower != null)
        {
            var inverse = Quaternion.Inverse(transform.rotation);
            _followerOffset = inverse * (_follower.position - transform.position);
            _followerRotation = inverse * _follower.rotation;
        }
        if (_handle == null) _handle = GetComponentInChildren<WindowHandle>(true);

        if (!animate || !gameObject.activeInHierarchy)
        {
            Apply(pose.position, pose.rotation);
            Stop();
            return;
        }
        _moving = true;
        enabled = true;
    }

    void Update()
    {
        if (_handle != null && _handle.IsGrabbed) { Stop(); return; }

        float dt = Time.unscaledDeltaTime;
        Vector3 position = Vector3.SmoothDamp(transform.position, _target.position, ref _velocity, SmoothTime, Mathf.Infinity, dt);
        float angle = Quaternion.Angle(transform.rotation, _target.rotation);
        float left = Mathf.SmoothDamp(angle, 0f, ref _angleVelocity, SmoothTime, Mathf.Infinity, dt);
        Quaternion rotation = angle > 0f
            ? Quaternion.Slerp(transform.rotation, _target.rotation, 1f - left / angle)
            : _target.rotation;

        if ((position - _target.position).sqrMagnitude < ArrivedDistance * ArrivedDistance && Quaternion.Angle(rotation, _target.rotation) < ArrivedAngle)
        {
            Apply(_target.position, _target.rotation);
            Stop();
            return;
        }
        Apply(position, rotation);
    }

    void Apply(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        if (_follower != null)
            _follower.SetPositionAndRotation(position + rotation * _followerOffset, rotation * _followerRotation);
    }

    void Stop()
    {
        _moving = false;
        _velocity = Vector3.zero;
        _angleVelocity = 0f;
        enabled = false;
    }

    void OnDisable()
    {
        // Hidden mid-glide: land at once, so it reappears where it was going.
        if (_moving) Apply(_target.position, _target.rotation);
        _moving = false;
        _velocity = Vector3.zero;
        _angleVelocity = 0f;
    }
}
