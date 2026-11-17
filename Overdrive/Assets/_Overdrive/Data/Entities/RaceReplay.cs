/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## RaceReplay - A downloaded replay ready to be played: each car's path on
 ## the track and its timing, on one clock in seconds. Built from replay.json
 ## by ODRaceReplayReader, read at a given time by ODReplayMapper.
 ## Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RaceReplay
{
    /// <summary>UTC instant of time 0. Every time below is in seconds from it.</summary>
    public DateTime origin;
    /// <summary>Lights out ("SESSION STARTED"): playback starts here.</summary>
    public float start;
    /// <summary>Chequered flag, or the last car sample when there is none.</summary>
    public float finish;
    /// <summary>Last car sample: playback stops here.</summary>
    public float end;
    public int totalLaps;
    /// <summary>Track outline (x, y), one clean lap of one car, in the backend's track units.</summary>
    public Vector2[] outline = Array.Empty<Vector2>();
    /// <summary>
    /// outlineDistance[i]: track distance from outline[0] to outline[i]. One value more than
    /// the outline: the last one closes the loop back to outline[0] (the lap length).
    /// </summary>
    public float[] outlineDistance = Array.Empty<float>();
    /// <summary>
    /// True when the backend gave every car the same x/y (race_stream bug). Cars are then
    /// placed on the outline from their lap times instead of their own samples.
    /// </summary>
    public bool sharedPositions;
    /// <summary>
    /// Where the timing line is on the outline, as a fraction of the lap from outline[0]
    /// (shared positions only: the outline is not cut at the line).
    /// </summary>
    public float lineOffset;
    /// <summary>By car number.</summary>
    public Dictionary<int, ReplayCar> cars = new();
    /// <summary>Name, code and team of each car number (from meta.json), set by ODReplayService.</summary>
    public Dictionary<int, DriverSummaryDto> drivers = new();
}

public sealed class ReplayCar
{
    public int number;

    // Car samples, sorted by time (about 4 per second).
    public float[] time = Array.Empty<float>();
    public float[] x = Array.Empty<float>();
    public float[] y = Array.Empty<float>();

    /// <summary>lapStart[n]: when lap n began (index 0 unused). Lap 1 starts at lights out.</summary>
    public float[] lapStart = Array.Empty<float>();
    /// <summary>lapTime[n]: duration of lap n (index 0 unused), NaN when the backend has none.</summary>
    public float[] lapTime = Array.Empty<float>();
    /// <summary>sector1[n] / sector2[n]: durations of the first two sectors of lap n, NaN when unknown.</summary>
    public float[] sector1 = Array.Empty<float>();
    public float[] sector2 = Array.Empty<float>();
    /// <summary>
    /// Odometer: odometer[i] metres driven at odometerTime[i], integrated from the car's own
    /// speed (right even when the backend shares the x/y between cars).
    /// </summary>
    public float[] odometerTime = Array.Empty<float>();
    public float[] odometer = Array.Empty<float>();
    /// <summary>Typical lap length driven by the car, in metres (NaN when unknown).</summary>
    public float lapDistance = float.NaN;
    /// <summary>Laps the car completed.</summary>
    public int lapCount;

    /// <summary>Official position changes, sorted by time.</summary>
    public float[] positionTime = Array.Empty<float>();
    public int[] position = Array.Empty<int>();

    public ReplayStint[] stints = Array.Empty<ReplayStint>();
    /// <summary>Lap of each pit stop.</summary>
    public int[] pitLaps = Array.Empty<int>();
}

public struct ReplayStint
{
    public string compound;   // "SOFT", "MEDIUM", "HARD", "INTERMEDIATE", "WET"
    public int lapStart;
    public int lapEnd;
    public int tyreAgeAtStart;
}
