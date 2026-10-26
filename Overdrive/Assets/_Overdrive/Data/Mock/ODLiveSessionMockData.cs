/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODLiveSessionMockData - F1 live race mock, ported from the mobile app's
 ## championship_mock_data.dart (championshipFormula1Mock: Monaco, lap 42/78,
 ## live timing group). Shown when the backend is unreachable and the user
 ## picks the demo data. The mobile mock has no tyre age, pit stops, last lap
 ## nor track status, so those stay empty.
 ##
 */

using System.Collections.Generic;
using UnityEngine;

public static class ODLiveSessionMockData
{
    public static LiveSessionData Formula1 => new LiveSessionData
    {
        championshipId = "formula_1",
        sessionName = "Course",
        circuitName = "Circuit de Monaco",
        conditions = "38°C · Ensoleillé",
        accentColor = new Color32(0xE8, 0x00, 0x00, 0xFF),
        currentLap = 42,
        totalLaps = 78,
        trackStatus = null,
        entries = new List<LiveEntry>
        {
            Entry(1,  "Verstappen", "Red Bull Racing", 0x3671C6, "Leader", "M"),
            Entry(2,  "Hamilton",   "Mercedes",        0x00D2BE, "+3.4s",  "M"),
            Entry(3,  "Leclerc",    "Ferrari",         0xE8002D, "+8.1s",  "S"),
            Entry(4,  "Norris",     "McLaren",         0xFF8000, "+12.7s", "S"),
            Entry(5,  "Sainz",      "Ferrari",         0xE8002D, "+18.3s", "M"),
            Entry(6,  "Alonso",     "Aston Martin",    0x358C75, "+24.1s", "H"),
            Entry(7,  "Piastri",    "McLaren",         0xFF8000, "+31.6s", "H"),
            Entry(8,  "Russell",    "Mercedes",        0x00D2BE, "+38.2s", "M"),
            Entry(9,  "Perez",      "Red Bull Racing", 0x3671C6, "+45.0s", "H"),
            Entry(10, "Gasly",      "Alpine",          0x2293D1, "+52.3s", "M"),
            Entry(11, "Ocon",       "Alpine",          0x2293D1, "+58.7s", "H"),
        },
    };

    static LiveEntry Entry(int position, string name, string team, int rgb, string gap, string tyre) => new LiveEntry
    {
        position = position,
        name = name,
        teamName = team,
        teamColor = new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 0xFF),
        gap = gap,
        tyreCompound = tyre,
        tyreAgeLaps = -1,   // not in the mobile mock
        pitStops = -1,   // not in the mobile mock
    };
}
