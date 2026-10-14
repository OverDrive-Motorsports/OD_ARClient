/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## SeasonStandings - Season standings shared by every championship panel
 ## state (off-season, event weekend). Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using System.Collections.Generic;

[Serializable]
public struct SeasonStandingEntry
{
    public int    position;
    public string name;
    /// <summary>Driver entries only; empty for team entries (the team IS the entry).</summary>
    public string teamName;
    /// <summary>float because the backend can award half points.</summary>
    public float  points;
}

/// <summary>
/// One standings table per category (e.g. "Hypercar", "LMP2", "GT3").
/// Single-class championships (F1, MotoGP) use one category with an empty label.
/// Lists can hold any number of entries — the UI renders one row per entry.
/// </summary>
[Serializable]
public class SeasonStandingCategory
{
    public string                    label;
    public List<SeasonStandingEntry> drivers = new();
    public List<SeasonStandingEntry> teams   = new();
}
