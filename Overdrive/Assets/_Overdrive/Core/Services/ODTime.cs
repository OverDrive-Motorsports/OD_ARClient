/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTime - Clock of the UI animations: real (unscaled) time, so UI motion
 ## keeps its speed whatever Time.timeScale is.
 ##
 */

using UnityEngine;

public static class ODTime
{
    /// <summary>Seconds since the previous frame.</summary>
    public static float Delta => Time.unscaledDeltaTime;

    /// <summary>Seconds since startup, on the same clock as <see cref="Delta"/>.</summary>
    public static float Now => Time.unscaledTime;
}
