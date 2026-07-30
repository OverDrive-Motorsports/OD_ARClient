/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## RaceTimingDTO - Mirrors the position/laps/stints/pitstops endpoints of
 ## the "Live - Course" section: GET /sessions/{id}/race/position,
 ## /race/laps, /race/stints, /race/pitstops.
 ##
 */

using System;
using System.Collections.Generic;

// GET /sessions/{id}/race/position
[Serializable]
public class RacePositionDTO
{
    public int driverNumber;
    public int position;
    public string gapToLeader;
    public string gapAhead;   // null for the race leader
    public string gapBehind;
    public int lapsCompleted;
    public string timestamp;
}

// One entry of RaceLapsDTO.laps
[Serializable]
public class LapDTO
{
    public int lapNumber;
    public float lapDuration;
    public float sector1;
    public float sector2;
    public float sector3;
    public bool isPitOutLap;
}

// GET /sessions/{id}/race/laps
[Serializable]
public class RaceLapsDTO
{
    public int driverNumber;
    public List<LapDTO> laps;
    public float bestLap;
    public float averageLap;
}

// GET /sessions/{id}/race/stints (one entry)
[Serializable]
public class StintDTO
{
    public int driverNumber;
    public int stintNumber;
    public string compound;
    public int lapStart;
    public int lapEnd;
    public int tyreAgeAtStart;
}

// GET /sessions/{id}/race/pitstops (one entry)
[Serializable]
public class PitStopDTO
{
    public int driverNumber;
    public int lapNumber;
    public float pitDuration;
}
