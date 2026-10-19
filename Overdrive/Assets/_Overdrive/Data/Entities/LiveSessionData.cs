/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## LiveSessionData - Data shown by the championship panel while a session
 ## is running ("LIVE · COURSE"): lap progress, track status and the running
 ## order. Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LiveEntry
{
    public int position;
    public string name;
    public string code;           // "VER" — derived from the name when empty
    public string teamName;
    public Color teamColor;
    public string gap;            // "Leader", "+3.4s", "+1 tour"
    public string tyreCompound;   // "S", "M", "H", "I", "W"
    public int tyreAgeLaps;    // -1 when unknown
    public int pitStops;       // -1 when unknown
    public string lastLap;        // "1:13.884"
    public bool isFastestLap;   // shown in purple
}

[Serializable]
public class LiveSessionData
{
    public string championshipId;
    public string sessionName;   // "Course"
    public string circuitName;   // "Circuit de Monaco"
    public string conditions;    // "38°C · Ensoleillé"
    public Color accentColor;
    public int currentLap;
    public int totalLaps;     // 0 when unknown (timed sessions)
    public string trackStatus;   // "Piste libre", "Safety Car", "Drapeau rouge"...
    /// <summary>Running order, P1 first — any count.</summary>
    public List<LiveEntry> entries = new();
}
