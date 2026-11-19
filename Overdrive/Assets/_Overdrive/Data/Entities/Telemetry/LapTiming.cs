/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## LapTiming - Timing of one driver, as the Sectors and Lap time widgets
 ## read it: the running lap (time, share driven, delta to the best lap), its
 ## 3 sectors and 24 mini-sectors with their colour code, the previous lap's
 ## sectors, the best lap and the last laps.
 ## Colour code (F1): purple = best of the session, green = personal best,
 ## yellow = slower, grey = not driven yet.
 ## Simulated for now (ODMockTelemetry), the backend feed will fill the same
 ## types. Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public enum SplitStatus { Upcoming, Running, Slower, PersonalBest, SessionBest }

[Serializable]
public struct LapRecord
{
    public int lap;       // 31
    public float time;    // seconds
}

[Serializable]
public class LapTiming
{
    public const int Sectors = 3, MiniSectors = 24;

    public string driverCode;   // "VER"
    public Color teamColor;

    public int lap;              // running lap number
    public float lapTime;        // seconds since the line
    public float lapShare;       // 0 – 1, share of the lap driven
    public float delta;          // seconds vs the best lap at the same point (negative = faster)

    // Running lap; a time < 0 is not set yet.
    public float[] sectorTimes = new float[Sectors];
    public SplitStatus[] sectorStatus = new SplitStatus[Sectors];
    public SplitStatus[] miniSectors = new SplitStatus[MiniSectors];
    // Previous lap, shown in place of the sectors not reached yet.
    public float[] lastSectorTimes = new float[Sectors];

    public float bestLap;        // personal best, seconds (0 = none yet)
    /// <summary>Last completed laps, oldest first (5 at most).</summary>
    public List<LapRecord> lastLaps = new();
}
