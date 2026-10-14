/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODReplayMockData - Replay gallery mock, ported from the mobile app's
 ## replay_mock_data.dart (replayCatalogMock: Grand Prix de Bahrein 2024,
 ## whose standings are championshipFormula1Mock's). Shown when the backend is
 ## unreachable and the user picks the demo data.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public static class ODReplayMockData
{
    public static List<ReplaySummary> Catalog => new List<ReplaySummary> { Bahrain2024 };

    static ReplaySummary Bahrain2024 => new ReplaySummary
    {
        id               = "f1_bahrein_2024",
        championshipId   = "formula_1",
        championshipName = "Formula 1",
        grandPrix        = "Grand Prix de Bahrein 2024",
        sessionName      = "Course",
        season           = "2024",
        circuit          = "Bahrain International Circuit",
        date             = new DateTime(2024, 3, 2),
    };

    /// <summary>
    /// The mobile replay only carries season standings (no race result), so the
    /// podium shows the top 3 drivers with their points.
    /// </summary>
    public static ReplayDetail Detail(string id)
    {
        if (id != Bahrain2024.id) return null;
        return new ReplayDetail
        {
            summary   = Bahrain2024,
            location  = "Sakhir, Bahrein",
            totalLaps = 57,
            results   = new List<LiveEntry>
            {
                Entry(1, "Verstappen", "Red Bull Racing", 0x3671C6, "161 pts"),
                Entry(2, "Hamilton",   "Mercedes",        0x00D2BE, "124 pts"),
                Entry(3, "Leclerc",    "Ferrari",         0xE8002D, "112 pts"),
                Entry(4, "Norris",     "McLaren",         0xFF8000, "98 pts"),
                Entry(5, "Sainz",      "Ferrari",         0xE8002D, "85 pts"),
            },
        };
    }

    static LiveEntry Entry(int position, string name, string team, int rgb, string value) => new LiveEntry
    {
        position  = position,
        name      = name,
        teamName  = team,
        teamColor = new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 0xFF),
        gap       = value,
        tyreAgeLaps = -1,
        pitStops    = -1,
    };
}
