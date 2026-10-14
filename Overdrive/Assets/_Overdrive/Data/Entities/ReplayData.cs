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
    public string   id;                 // backend sessionId, or a mock id
    public string   championshipId;     // "f1", "formula_1" — drives the page colours
    public string   championshipName;   // "Formule 1"
    public string   grandPrix;          // "Grand Prix de Bahrein 2024"
    public string   sessionName;        // "Course", "Sprint"
    public string   season;             // "2024"
    public string   circuit;            // "Bahrain International Circuit"
    public DateTime date;
}

[Serializable]
public class ReplayDetail
{
    public ReplaySummary   summary;
    public string          location;    // "Sakhir, Bahrein"
    public int             totalLaps;
    /// <summary>Final classification, P1 first. "gap" holds the gap to the winner (or points for mocks).</summary>
    public List<LiveEntry> results = new();
}
