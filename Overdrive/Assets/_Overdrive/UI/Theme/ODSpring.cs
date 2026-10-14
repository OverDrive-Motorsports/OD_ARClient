/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODSpring - Damped spring used by every animated control (tab bar,
 ## championship tiles): velocity carries over between targets and a damping
 ## ratio below 1 gives the soft overshoot of native Apple motion.
 ##
 */

using UnityEngine;

public struct ODSpring
{
    public float value, velocity;

    /// <summary>Integrated in small fixed steps so it stays stable at any frame rate.</summary>
    public void Step(float target, float stiffness, float damping, float dt)
    {
        const float maxStep = 1f / 240f;
        dt = Mathf.Min(dt, .05f);
        while (dt > 0f)
        {
            float h = Mathf.Min(dt, maxStep);
            velocity += (stiffness * (target - value) - damping * velocity) * h;
            value    += velocity * h;
            dt -= h;
        }
    }

    public void Snap(float target) { value = target; velocity = 0f; }
}
