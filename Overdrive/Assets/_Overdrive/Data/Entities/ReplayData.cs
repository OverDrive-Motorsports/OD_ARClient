/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ReplayData - A finished session available in the replay gallery
 ## (ReplaySummary) and what the championship panel shows once it's opened
 ## (ReplayDetail). Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using System.Collections.Generic;

[Serializable]
public struct ReplaySummary
{
    public string id;                 // backend sessionId
    public string championshipId;     // "f1", "formula_1" — drives the page colours
    public string championshipName;   // "Formula 1"
    public string grandPrix;          // "Bahrain Grand Prix 2024"
    public string sessionName;        // "Race", "Sprint"
    public string season;             // "2024"
    public string circuit;            // "Bahrain International Circuit"
    public DateTime date;
}

[Serializable]
public class ReplayDetail
{
    public ReplaySummary summary;
    public string location;    // "Sakhir, Bahrein"
    public int totalLaps;
    /// <summary>Final classification, P1 first. "gap" holds the gap to the winner.</summary>
    public List<LiveEntry> results = new();
}
