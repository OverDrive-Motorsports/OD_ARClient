/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODWindowPlacement - Where windows sit around the user (pure geometry, no
 ## state). Windows live on an arc around the headset, like visionOS: each
 ## one at the same distance from the head, turned to face it, and
 ## neighbours side by side with a small gap between their edges. An angle of
 ## 0 is straight ahead along the given yaw; positive angles go to the right.
 ## A canvas can be larger than the window it shows: callers pass the visible
 ## rect, and the pose returned is the canvas pose that puts that rect in place.
 ##
 */

using UnityEngine;

public static class ODWindowPlacement
{
    /// <summary>Metres between the headset and a window when it appears.</summary>
    public const float Distance = 1.3f;
    /// <summary>Metres between two windows side by side.</summary>
    public const float Gap = .06f;
    /// <summary>Closest a window is kept to the head when the arc is measured from where it stands.</summary>
    const float MinRadius = .6f;

    /// <summary>A circle around the head (flat), at a height, on which windows are laid out.</summary>
    public struct Arc
    {
        public Vector3 centre;   // head position, y ignored
        public float radius, height, yaw;   // yaw of the window at angle 0, in degrees

        /// <summary>The arc in front of <paramref name="head"/>, at eye height.</summary>
        public static Arc InFront(Transform head) => new Arc
        {
            centre = head.position,
            radius = Distance,
            height = head.position.y,
            yaw = Yaw(head.forward),
        };

        /// <summary>The arc through <paramref name="windowCentre"/>: its radius, height and direction from the head.</summary>
        public static Arc Through(Transform head, Vector3 windowCentre)
        {
            Vector3 flat = windowCentre - head.position;
            flat.y = 0f;
            return new Arc
            {
                centre = head.position,
                radius = Mathf.Max(MinRadius, flat.magnitude),
                height = windowCentre.y,
                yaw = flat.sqrMagnitude > .0001f ? Yaw(flat) : Yaw(head.forward),
            };
        }

        /// <summary>Centre and rotation of a window at <paramref name="angle"/> degrees on the arc, facing the head.</summary>
        public Pose At(float angle)
        {
            var rotation = Quaternion.Euler(0f, yaw + angle, 0f);
            var position = centre + rotation * Vector3.forward * radius;
            position.y = height;
            return new Pose(position, rotation);
        }

        /// <summary>Degrees between the centres of two neighbours of half widths <paramref name="a"/> and <paramref name="b"/> (metres).</summary>
        public float Step(float a, float b) =>
            (Mathf.Atan2(a, radius) + Mathf.Atan2(b, radius) + Gap / radius) * Mathf.Rad2Deg;
    }

    /// <summary>World centre of a rect.</summary>
    public static Vector3 Centre(RectTransform rect) => rect.TransformPoint(rect.rect.center);

    /// <summary>Half the width of a rect in metres.</summary>
    public static float HalfWidth(RectTransform rect) => rect.rect.width * Mathf.Abs(rect.lossyScale.x) * .5f;

    /// <summary>
    /// Pose to give <paramref name="root"/> so that <paramref name="visible"/> (inside it, or the root
    /// itself when null) is centred on <paramref name="target"/> with the target's rotation.
    /// </summary>
    public static Pose RootPose(Transform root, RectTransform visible, Pose target)
    {
        Vector3 offset = visible != null ? Quaternion.Inverse(root.rotation) * (Centre(visible) - root.position) : Vector3.zero;
        return new Pose(target.position - target.rotation * offset, target.rotation);
    }

    /// <summary>Where the centre of <paramref name="visible"/> would be with <paramref name="root"/> at <paramref name="rootPose"/>.</summary>
    public static Vector3 CentreAt(Transform root, RectTransform visible, Pose rootPose)
    {
        Vector3 offset = visible != null ? Quaternion.Inverse(root.rotation) * (Centre(visible) - root.position) : Vector3.zero;
        return rootPose.position + rootPose.rotation * offset;
    }

    /// <summary>Puts <paramref name="root"/> at once so <paramref name="visible"/> is centred in front of <paramref name="head"/>.</summary>
    public static void InFront(Transform root, RectTransform visible, Transform head)
    {
        if (root == null || head == null) return;
        var pose = RootPose(root, visible, Arc.InFront(head).At(0f));
        root.SetPositionAndRotation(pose.position, pose.rotation);
    }

    static float Yaw(Vector3 direction) => Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
}
