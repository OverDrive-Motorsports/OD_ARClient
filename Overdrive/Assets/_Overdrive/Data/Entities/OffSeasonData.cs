/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## OffSeasonData - Data shown by the championship panel in its off-season
 ## ("hors direct") state: next event countdown, upcoming calendar and season
 ## standings grouped by category. Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct OffSeasonEvent
{
    public string   name;       // "24 Heures du Mans"
    public string   location;   // "Circuit de la Sarthe · France"
    public int      round;      // Championship round number, 0 when unknown
    public DateTime startsAt;
}

// Not Unity-serialized: nullable members (Unity's serializer skips them).
public class OffSeasonData
{
    public string          championshipId;
    public string          championshipName;  // "WEC"
    public string          headline;          // "World Endurance"
    public Color           accentColor;
    public OffSeasonEvent? nextEvent;          // null when the season is over
    /// <summary>Events after nextEvent, in chronological order.</summary>
    public List<OffSeasonEvent>         calendar  = new();
    public List<SeasonStandingCategory> standings = new();
}
