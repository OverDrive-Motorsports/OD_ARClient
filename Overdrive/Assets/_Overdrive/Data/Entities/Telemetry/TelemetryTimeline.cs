/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## TelemetryTimeline - The backend telemetry of a session, as the widgets
 ## placed in the space read it: the followed car (Verstappen for now), the
 ## compared car (Hamilton), and the best time of each sector over every car,
 ## all on one clock in seconds. Built by ODTelemetryMapper from a downloaded
 ## replay or from the live routes, read at a given time by the same mapper
 ## (ODTelemetryMapper.Fill → TelemetryFrame).
 ## Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using UnityEngine;

/// <summary>What the placed widgets can show at the moment.</summary>
public enum TelemetryStatus
{
    /// <summary>The session or its data is on its way: nothing to show yet.</summary>
    Loading,
    /// <summary>Data for the time shown: the widgets display it.</summary>
    Ready,
    /// <summary>The session has no telemetry for the followed car (at all, or at this time).</summary>
    NoData,
    /// <summary>The backend stopped answering: the last values are hidden, never left on screen.</summary>
    ConnectionLost,
}

public sealed class TelemetryTimeline
{
    /// <summary>Driver codes the widgets follow and compare ("VER", "HAM"): shown even before any data.</summary>
    public string driverCode, rivalCode;
    /// <summary>Followed and compared cars; null when the session has no data for them.</summary>
    public DriverTelemetry driver, rival;
    /// <summary>
    /// sectorBestTime[s][i]: when the i-th sector s time was set (sorted), sectorBest[s][i]: the best
    /// sector s of any car up to then. A time of -infinity is "already set" (live laps).
    /// </summary>
    public float[][] sectorBestTime = { Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>() };
    public float[][] sectorBest = { Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>() };
}

public sealed class DriverTelemetry
{
    public int number;
    public string code;          // "VER"
    public Color teamColor;

    // Telemetry samples, sorted by time (about 4 per second).
    public float[] time = Array.Empty<float>();
    public float[] speed = Array.Empty<float>();      // km/h
    public float[] throttle = Array.Empty<float>();   // %, 0 – 100
    public float[] brake = Array.Empty<float>();      // %, 0 – 100
    public float[] rpm = Array.Empty<float>();
    public int[] gear = Array.Empty<int>();
    /// <summary>odometer[i]: metres driven at time[i], from the car's own speed.</summary>
    public float[] odometer = Array.Empty<float>();
    public float maxRpm;

    /// <summary>lapStart[n]: when lap n began (index 0 unused); lapStart[n + 1] is its end. NaN when unknown.</summary>
    public float[] lapStart = Array.Empty<float>();
    /// <summary>Duration of lap n and of its 3 sectors (index 0 unused), NaN when unknown.</summary>
    public float[] lapTime = Array.Empty<float>();
    public float[] sector1 = Array.Empty<float>();
    public float[] sector2 = Array.Empty<float>();
    public float[] sector3 = Array.Empty<float>();
    public int lapCount;
    /// <summary>Typical lap length driven, in metres (NaN when unknown).</summary>
    public float lapLength = float.NaN;
    /// <summary>Share of the lap where S1 and S2 end (null when no complete lap has samples).</summary>
    public float[] sectorEnds;

    /// <summary>
    /// Mini-sectors of the completed laps, filled on first read by ODTelemetryMapper:
    /// miniTimes[n][m] (duration) and miniEnds[n][m] (when it ended). An empty array: no samples for lap n.
    /// </summary>
    public float[][] miniTimes, miniEnds;
}
