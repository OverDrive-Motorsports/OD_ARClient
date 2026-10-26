/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODOffSeasonMockData - WEC off-season mock, ported from the mobile app:
 ## championship_mock_data.dart (championshipWecMock: next event + standings)
 ## and calendar_mock_backend.dart (the other WEC rounds). Shown when the
 ## backend is unreachable and the user picks the demo data.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public static class ODOffSeasonMockData
{
    /// <summary>
    /// The mobile mock uses fixed 2026 dates that are already in the past, so
    /// dates are shifted relative to now: the countdown always shows a live value.
    /// The mobile calendar has no round numbers (round = 0: the badge shows the date).
    /// </summary>
    public static OffSeasonData Wec => new OffSeasonData
    {
        championshipId = "wec",
        championshipName = "WEC",
        headline = "World Endurance",
        accentColor = new Color32(0x4A, 0x90, 0xD9, 0xFF),
        nextEvent = new OffSeasonEvent
        {
            name = "24 Heures du Mans",
            location = "Circuit de la Sarthe · France",
            startsAt = DateTime.Now.AddDays(12).AddHours(4).AddMinutes(37),
        },
        calendar = new List<OffSeasonEvent>
        {
            new OffSeasonEvent { name = "6 Hours of Spa", location = "Spa-Francorchamps",           startsAt = DateTime.Now.AddDays(47) },
            new OffSeasonEvent { name = "Qatar 1812 KM",  location = "Lusail International Circuit", startsAt = DateTime.Now.AddDays(82) },
        },
        standings = new List<SeasonStandingCategory>
        {
            Category("Hypercar",
                Drivers(("Hartley", "Toyota Gazoo Racing", 89), ("Conway", "Toyota Gazoo Racing", 76), ("Bamber", "Porsche Penske", 71),
                        ("Kobayashi", "Toyota Gazoo Racing", 65), ("Buemi", "Toyota Gazoo Racing", 58)),
                Teams(("Toyota Gazoo Racing", 142), ("Porsche Penske", 118), ("Ferrari AF Corse", 97),
                      ("Cadillac Racing", 84), ("Lamborghini Iron Lynx", 71))),
            Category("LMP2",
                Drivers(("Jarvis", "United Autosports", 94), ("Lynn", "Vector Sport", 81), ("Rast", "WRT", 73),
                        ("Frijns", "WRT", 67), ("Hanley", "United Autosports", 59)),
                Teams(("United Autosports", 148), ("WRT", 126), ("Vector Sport", 109),
                      ("Prema Racing", 88), ("Inter Europol", 74))),
            Category("GT3",
                Drivers(("Cairoli", "Manthey EMA", 91), ("Pera", "Iron Dames", 84), ("Schiavoni", "Vista AF Corse", 75),
                        ("Farfus", "WRT BMW", 69), ("Rovera", "Vista AF Corse", 62)),
                Teams(("Manthey EMA", 139), ("Vista AF Corse", 128), ("WRT BMW", 111),
                      ("Iron Dames", 97), ("Akkodis ASP", 79))),
        },
    };

    static SeasonStandingCategory Category(string label, List<SeasonStandingEntry> drivers, List<SeasonStandingEntry> teams)
        => new SeasonStandingCategory { label = label, drivers = drivers, teams = teams };

    // Positions follow list order, so adding or removing a row never needs renumbering.
    static List<SeasonStandingEntry> Drivers(params (string name, string team, float pts)[] rows)
    {
        var list = new List<SeasonStandingEntry>();
        for (int i = 0; i < rows.Length; i++)
            list.Add(new SeasonStandingEntry { position = i + 1, name = rows[i].name, teamName = rows[i].team, points = rows[i].pts });
        return list;
    }

    static List<SeasonStandingEntry> Teams(params (string name, float pts)[] rows)
    {
        var list = new List<SeasonStandingEntry>();
        for (int i = 0; i < rows.Length; i++)
            list.Add(new SeasonStandingEntry { position = i + 1, name = rows[i].name, points = rows[i].pts });
        return list;
    }
}
