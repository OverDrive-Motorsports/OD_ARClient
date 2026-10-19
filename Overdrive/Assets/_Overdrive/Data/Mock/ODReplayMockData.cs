/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODReplayMockData - Replay gallery mock, shown when the backend is
 ## unreachable and the user picks the demo data: one replay, the Grand Prix
 ## de Bahreïn 2024 of the mobile app's replay_mock_data.dart, with its final
 ## classification (top 5, gaps to the winner) for the broadcast page.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public static class ODReplayMockData
{
    const int RedBull = 0x3671C6, Ferrari = 0xE8002D, Mercedes = 0x27F4D2;

    sealed class Race
    {
        public ReplaySummary summary;
        public string location;
        public int laps;
        public LiveEntry[] top5;
    }

    static readonly Race[] Races =
    {
        new Race
        {
            summary  = Summary("f1_bahrein_2024", "Grand Prix de Bahreïn", "Course", "Bahrain International Circuit", new DateTime(2024, 3, 2)),
            location = "Sakhir, Bahreïn", laps = 57,
            top5     = Top5(("Verstappen", "Red Bull Racing", RedBull, ""), ("Pérez", "Red Bull Racing", RedBull, "+22.457"),
                            ("Sainz", "Ferrari", Ferrari, "+25.110"), ("Leclerc", "Ferrari", Ferrari, "+39.669"), ("Russell", "Mercedes", Mercedes, "+46.788")),
        },
    };

    public static List<ReplaySummary> Catalog
    {
        get
        {
            var list = new List<ReplaySummary>();
            foreach (var race in Races) list.Add(race.summary);
            return list;
        }
    }

    /// <summary>Final classification (top 5) of a demo replay; null for an unknown id.</summary>
    public static ReplayDetail Detail(string id)
    {
        foreach (var race in Races)
        {
            if (race.summary.id != id) continue;
            return new ReplayDetail
            {
                summary = race.summary,
                location = race.location,
                totalLaps = race.laps,
                results = new List<LiveEntry>(race.top5),
            };
        }
        return null;
    }

    static ReplaySummary Summary(string id, string grandPrix, string session, string circuit, DateTime date) => new ReplaySummary
    {
        id = id,
        championshipId = "formula_1",
        championshipName = "Formula 1",
        grandPrix = grandPrix,
        sessionName = session,
        season = date.Year.ToString(),
        circuit = circuit,
        date = date,
    };

    static LiveEntry[] Top5(params (string name, string team, int rgb, string gap)[] rows)
    {
        var entries = new LiveEntry[rows.Length];
        for (int i = 0; i < rows.Length; i++)
            entries[i] = new LiveEntry
            {
                position = i + 1,
                name = rows[i].name,
                teamName = rows[i].team,
                teamColor = new Color32((byte)(rows[i].rgb >> 16), (byte)(rows[i].rgb >> 8), (byte)rows[i].rgb, 0xFF),
                gap = i == 0 ? "1er" : ODChampionshipMapper.FormatGap(rows[i].gap),
                tyreAgeLaps = -1,
                pitStops = -1,
            };
        return entries;
    }
}
