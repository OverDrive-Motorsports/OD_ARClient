/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODWeekendMockData - MotoGP event-weekend mock, ported from the mobile
 ## app's championship_mock_data.dart (championshipMotoGpMock). Shown when
 ## the backend is unreachable and the user picks the demo data.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public static class ODWeekendMockData
{
    /// <summary>
    /// Same sessions as the mobile mock, shifted relative to now (the mobile
    /// dates are in the past) while keeping their status: EL1/EL2 done, EL3
    /// next. The mobile mock has no wind speed: -1 is shown as "--".
    /// </summary>
    public static WeekendData MotoGp => new WeekendData
    {
        championshipId = "motogp",
        eventName      = "Gran Premio d’Italia",
        accentColor    = new Color32(0xE8, 0x77, 0x22, 0xFF),
        circuit = new WeekendCircuit
        {
            name = "Autodromo del Mugello", location = "Scarperia e San Piero · Italie",
            lengthKm = 5.245f, totalLaps = 23,
        },
        weather = new WeekendWeather
        {
            airTempC = 18, trackTempC = 28, windKmh = -1, rainChancePercent = 10,
        },
        sessions = new List<WeekendSession>
        {
            Session("EL1",            DateTime.Now.AddHours(-25),               WeekendSessionStatus.Completed),
            Session("EL2",            DateTime.Now.AddHours(-20.5),             WeekendSessionStatus.Completed),
            Session("EL3",            DateTime.Now.AddMinutes(12),              WeekendSessionStatus.Upcoming),
            Session("Qualifications", DateTime.Now.AddHours(4).AddMinutes(12),  WeekendSessionStatus.Upcoming),
            Session("Course",         DateTime.Now.AddHours(28).AddMinutes(12), WeekendSessionStatus.Upcoming),
        },
        standings = new List<SeasonStandingCategory>
        {
            new SeasonStandingCategory
            {
                label   = "",
                drivers = new List<SeasonStandingEntry>
                {
                    new SeasonStandingEntry { position = 1, name = "Bagnaia",    teamName = "Ducati Lenovo",  points = 138 },
                    new SeasonStandingEntry { position = 2, name = "Marquez",    teamName = "Gresini Racing", points = 121 },
                    new SeasonStandingEntry { position = 3, name = "Bastianini", teamName = "Ducati Lenovo",  points = 98  },
                    new SeasonStandingEntry { position = 4, name = "Martin",     teamName = "Aprilia Racing", points = 87  },
                    new SeasonStandingEntry { position = 5, name = "Quartararo", teamName = "Monster Yamaha", points = 76  },
                },
            },
        },
    };

    static WeekendSession Session(string name, DateTime startsAt, WeekendSessionStatus status)
        => new WeekendSession { name = name, startsAt = startsAt, status = status };
}
