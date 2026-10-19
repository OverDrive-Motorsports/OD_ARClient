/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## WeekendData - Data shown by the championship panel during an event
 ## weekend ("LIVE · WEEK-END"): circuit and weather, session programme and
 ## season standings. Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeekendSessionStatus
{
    Completed,
    Live,
    Upcoming
}

[Serializable]
public struct WeekendSession
{
    public string name;      // "EL1", "Qualifications", "Sprint"...
    public DateTime startsAt;
    public WeekendSessionStatus status;
}

[Serializable]
public struct WeekendCircuit
{
    public string name;       // "Autodromo del Mugello"
    public string location;   // "Scarperia e San Piero · Italie"
    public float lengthKm;
    public int totalLaps;
}

[Serializable]
public struct WeekendWeather
{
    public float airTempC;
    public float trackTempC;
    public float windKmh;
    public int rainChancePercent;
}

// Not Unity-serialized: nullable members (Unity's serializer skips them).
public class WeekendData
{
    public string championshipId;
    public string eventName;     // "Gran Premio d'Italia"
    public Color accentColor;
    public WeekendCircuit? circuit;
    public WeekendWeather? weather;
    /// <summary>Every session of the weekend in chronological order — any count.</summary>
    public List<WeekendSession> sessions = new();
    public List<SeasonStandingCategory> standings = new();
}
