/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODChampionshipMockData - Data-driven championship mocks, ported from the
 ## mobile app's championship_mock_data.dart. Pure data: no MonoBehaviour, no
 ## logic, no knowledge of how the championship page renders any of this.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public enum ChampionshipState
{
    LiveSession,
    OffSeason,
    EventWeekend
}

public enum StandingType
{
    Drivers,
    Teams
}

public enum SessionStatus
{
    Completed,
    Upcoming
}

[Serializable]
public struct ChampionshipCircuit
{
    public string name;
    public string location;
    public float lengthKm;
    public int totalLaps;
}

[Serializable]
public struct ChampionshipWeather
{
    public string condition;
    public int trackTempC;
    public int airTempC;
    public int rainChancePercent;
}

[Serializable]
public struct ChampionshipLiveEntry
{
    public int position;
    public string name;
    public string teamName;
    public string gap;
    public int lap;
    public string tyreCompound;
    public Color teamColor;
}

[Serializable]
public struct ChampionshipLiveGroup
{
    public string label;
    public List<ChampionshipLiveEntry> entries;
}

[Serializable]
public struct ChampionshipSession
{
    public string name;
    public DateTime scheduledAt;
    public SessionStatus status;
}

[Serializable]
public struct ChampionshipStandingEntry
{
    public int position;
    public string name;
    public int points;
    /// <summary>Set for driver entries; empty for team entries (the team IS the entry).</summary>
    public string teamName;
}

[Serializable]
public struct ChampionshipStandingTable
{
    public StandingType type;
    /// <summary>e.g. "Pilotes", "Equipes — Hypercar". Category() strips the type prefix.</summary>
    public string label;
    public List<ChampionshipStandingEntry> entries;

    /// <summary>
    /// Category this table belongs to, derived from label (e.g. "Pilotes — Hypercar" -> "Hypercar",
    /// plain "Pilotes" -> ""). Tables sharing the same category are paired into one
    /// generic drivers/teams standings widget.
    /// </summary>
    public string Category()
    {
        int dash = label.IndexOf('—');
        return dash < 0 ? "" : label.Substring(dash + 1).Trim();
    }
}

[Serializable]
public struct ChampionshipNextEvent
{
    public string name;
    public string location;
    public DateTime startsAt;
}

[Serializable]
public struct ChampionshipReplays
{
    public string label;
}

/// <summary>
/// Full data set for one championship. Every field below is optional except
/// id/name/headline/accentColor/state/standings/replays — the championship
/// page renders only the sections whose data is actually present, without
/// knowing which specific championship it's showing.
/// </summary>
[Serializable]
public class ChampionshipData
{
    public string id;
    public string name;
    public string headline;
    public Color accentColor;
    public ChampionshipState state;

    public ChampionshipCircuit? circuit;
    public ChampionshipWeather? weather;
    public List<ChampionshipLiveGroup> liveGroups;
    public List<ChampionshipStandingTable> standings;
    public List<ChampionshipSession> schedule;
    public ChampionshipNextEvent? nextEvent;
    public ChampionshipReplays replays;
}

public static class ODChampionshipMockData
{
    static Color Hex(byte r, byte g, byte b) => new Color32(r, g, b, 255);

    // ─────────────────────────────────────────────────────────────────────────────
    // Formula 1 — live session
    // ─────────────────────────────────────────────────────────────────────────────

    public static ChampionshipData Formula1Mock => new ChampionshipData
    {
        id = "formula_1",
        name = "Formula 1",
        headline = "Course",
        accentColor = Hex(0xE8, 0x00, 0x00),
        state = ChampionshipState.LiveSession,
        circuit = new ChampionshipCircuit
        {
            name = "Circuit de Monaco",
            location = "Monaco",
            lengthKm = 3.337f,
            totalLaps = 78,
        },
        weather = new ChampionshipWeather
        {
            condition = "Ensoleille",
            trackTempC = 38,
            airTempC = 24,
            rainChancePercent = 0,
        },
        liveGroups = new List<ChampionshipLiveGroup>
        {
            new ChampionshipLiveGroup
            {
                label = "",
                entries = new List<ChampionshipLiveEntry>
                {
                    new ChampionshipLiveEntry { position = 1,  name = "Verstappen", teamName = "Red Bull Racing", gap = "Leader", lap = 42, tyreCompound = "M", teamColor = Hex(0x36,0x71,0xC6) },
                    new ChampionshipLiveEntry { position = 2,  name = "Hamilton",   teamName = "Mercedes",        gap = "+3.4s",  lap = 42, tyreCompound = "M", teamColor = Hex(0x00,0xD2,0xBE) },
                    new ChampionshipLiveEntry { position = 3,  name = "Leclerc",    teamName = "Ferrari",         gap = "+8.1s",  lap = 42, tyreCompound = "S", teamColor = Hex(0xE8,0x00,0x2D) },
                    new ChampionshipLiveEntry { position = 4,  name = "Norris",     teamName = "McLaren",         gap = "+12.7s", lap = 42, tyreCompound = "S", teamColor = Hex(0xFF,0x80,0x00) },
                    new ChampionshipLiveEntry { position = 5,  name = "Sainz",      teamName = "Ferrari",         gap = "+18.3s", lap = 41, tyreCompound = "M", teamColor = Hex(0xE8,0x00,0x2D) },
                    new ChampionshipLiveEntry { position = 6,  name = "Alonso",     teamName = "Aston Martin",    gap = "+24.1s", lap = 41, tyreCompound = "H", teamColor = Hex(0x35,0x8C,0x75) },
                    new ChampionshipLiveEntry { position = 7,  name = "Piastri",    teamName = "McLaren",         gap = "+31.6s", lap = 41, tyreCompound = "H", teamColor = Hex(0xFF,0x80,0x00) },
                    new ChampionshipLiveEntry { position = 8,  name = "Russell",    teamName = "Mercedes",        gap = "+38.2s", lap = 41, tyreCompound = "M", teamColor = Hex(0x00,0xD2,0xBE) },
                    new ChampionshipLiveEntry { position = 9,  name = "Perez",      teamName = "Red Bull Racing", gap = "+45.0s", lap = 40, tyreCompound = "H", teamColor = Hex(0x36,0x71,0xC6) },
                    new ChampionshipLiveEntry { position = 10, name = "Gasly",      teamName = "Alpine",          gap = "+52.3s", lap = 40, tyreCompound = "M", teamColor = Hex(0x22,0x93,0xD1) },
                    new ChampionshipLiveEntry { position = 11, name = "Ocon",       teamName = "Alpine",          gap = "+58.7s", lap = 40, tyreCompound = "H", teamColor = Hex(0x22,0x93,0xD1) },
                    new ChampionshipLiveEntry { position = 12, name = "Stroll",     teamName = "Aston Martin",    gap = "+1 t",   lap = 39, tyreCompound = "H", teamColor = Hex(0x35,0x8C,0x75) },
                    new ChampionshipLiveEntry { position = 13, name = "Tsunoda",    teamName = "RB",              gap = "+1 t",   lap = 39, tyreCompound = "M", teamColor = Hex(0x66,0x92,0xFF) },
                    new ChampionshipLiveEntry { position = 14, name = "Hulkenberg", teamName = "Haas",            gap = "+1 t",   lap = 39, tyreCompound = "H", teamColor = Hex(0xB6,0xBA,0xBD) },
                    new ChampionshipLiveEntry { position = 15, name = "Bottas",     teamName = "Kick Sauber",     gap = "+1 t",   lap = 38, tyreCompound = "H", teamColor = Hex(0x52,0xE2,0x52) },
                    new ChampionshipLiveEntry { position = 16, name = "Zhou",       teamName = "Kick Sauber",     gap = "+1 t",   lap = 38, tyreCompound = "H", teamColor = Hex(0x52,0xE2,0x52) },
                    new ChampionshipLiveEntry { position = 17, name = "Sargeant",   teamName = "Williams",        gap = "+1 t",   lap = 38, tyreCompound = "M", teamColor = Hex(0x37,0xBE,0xDD) },
                    new ChampionshipLiveEntry { position = 18, name = "Albon",      teamName = "Williams",        gap = "+1 t",   lap = 38, tyreCompound = "M", teamColor = Hex(0x37,0xBE,0xDD) },
                    new ChampionshipLiveEntry { position = 19, name = "Magnussen",  teamName = "Haas",            gap = "+2 t",   lap = 37, tyreCompound = "H", teamColor = Hex(0xB6,0xBA,0xBD) },
                    new ChampionshipLiveEntry { position = 20, name = "De Vries",   teamName = "RB",              gap = "+2 t",   lap = 37, tyreCompound = "H", teamColor = Hex(0x66,0x92,0xFF) },
                },
            },
        },
        standings = new List<ChampionshipStandingTable>
        {
            new ChampionshipStandingTable
            {
                type = StandingType.Drivers, label = "Pilotes",
                entries = new List<ChampionshipStandingEntry>
                {
                    new ChampionshipStandingEntry { position = 1, name = "Verstappen", points = 161, teamName = "Red Bull Racing" },
                    new ChampionshipStandingEntry { position = 2, name = "Hamilton",   points = 124, teamName = "Mercedes" },
                    new ChampionshipStandingEntry { position = 3, name = "Leclerc",    points = 112, teamName = "Ferrari" },
                    new ChampionshipStandingEntry { position = 4, name = "Norris",     points = 98,  teamName = "McLaren" },
                    new ChampionshipStandingEntry { position = 5, name = "Sainz",      points = 85,  teamName = "Ferrari" },
                },
            },
            new ChampionshipStandingTable
            {
                type = StandingType.Teams, label = "Equipes",
                entries = new List<ChampionshipStandingEntry>
                {
                    new ChampionshipStandingEntry { position = 1, name = "Red Bull Racing", points = 273 },
                    new ChampionshipStandingEntry { position = 2, name = "Ferrari",         points = 197 },
                    new ChampionshipStandingEntry { position = 3, name = "Mercedes",        points = 181 },
                    new ChampionshipStandingEntry { position = 4, name = "McLaren",         points = 152 },
                    new ChampionshipStandingEntry { position = 5, name = "Aston Martin",    points = 89  },
                },
            },
        },
        replays = new ChampionshipReplays { label = "Toutes les courses de la saison" },
    };

    // ─────────────────────────────────────────────────────────────────────────────
    // WEC — off season (next event countdown + 3 categories x drivers/teams)
    // ─────────────────────────────────────────────────────────────────────────────

    public static ChampionshipData WecMock => new ChampionshipData
    {
        id = "wec",
        name = "WEC",
        headline = "World Endurance",
        accentColor = Hex(0x4A, 0x90, 0xD9),
        state = ChampionshipState.OffSeason,
        nextEvent = new ChampionshipNextEvent
        {
            name = "24 Heures du Mans",
            location = "Circuit de la Sarthe · France",
            startsAt = new DateTime(2026, 6, 5, 16, 0, 0),
        },
        standings = new List<ChampionshipStandingTable>
        {
            new ChampionshipStandingTable
            {
                type = StandingType.Drivers, label = "Pilotes — Hypercar",
                entries = new List<ChampionshipStandingEntry>
                {
                    new ChampionshipStandingEntry { position = 1, name = "Hartley",   points = 89, teamName = "Toyota Gazoo Racing" },
                    new ChampionshipStandingEntry { position = 2, name = "Conway",    points = 76, teamName = "Toyota Gazoo Racing" },
                    new ChampionshipStandingEntry { position = 3, name = "Bamber",    points = 71, teamName = "Porsche Penske" },
                    new ChampionshipStandingEntry { position = 4, name = "Kobayashi", points = 65, teamName = "Toyota Gazoo Racing" },
                    new ChampionshipStandingEntry { position = 5, name = "Buemi",     points = 58, teamName = "Toyota Gazoo Racing" },
                },
            },
            new ChampionshipStandingTable
            {
                type = StandingType.Teams, label = "Equipes — Hypercar",
                entries = new List<ChampionshipStandingEntry>
                {
                    new ChampionshipStandingEntry { position = 1, name = "Toyota Gazoo Racing",   points = 142 },
                    new ChampionshipStandingEntry { position = 2, name = "Porsche Penske",        points = 118 },
                    new ChampionshipStandingEntry { position = 3, name = "Ferrari AF Corse",      points = 97  },
                    new ChampionshipStandingEntry { position = 4, name = "Cadillac Racing",       points = 84  },
                    new ChampionshipStandingEntry { position = 5, name = "Lamborghini Iron Lynx", points = 71  },
                },
            },
            new ChampionshipStandingTable
            {
                type = StandingType.Drivers, label = "Pilotes — LMP2",
                entries = new List<ChampionshipStandingEntry>
                {
                    new ChampionshipStandingEntry { position = 1, name = "Jarvis", points = 94, teamName = "United Autosports" },
                    new ChampionshipStandingEntry { position = 2, name = "Lynn",   points = 81, teamName = "Vector Sport" },
                    new ChampionshipStandingEntry { position = 3, name = "Rast",   points = 73, teamName = "WRT" },
                    new ChampionshipStandingEntry { position = 4, name = "Frijns", points = 67, teamName = "WRT" },
                    new ChampionshipStandingEntry { position = 5, name = "Hanley", points = 59, teamName = "United Autosports" },
                },
            },
            new ChampionshipStandingTable
            {
                type = StandingType.Teams, label = "Equipes — LMP2",
                entries = new List<ChampionshipStandingEntry>
                {
                    new ChampionshipStandingEntry { position = 1, name = "United Autosports", points = 148 },
                    new ChampionshipStandingEntry { position = 2, name = "WRT",                points = 126 },
                    new ChampionshipStandingEntry { position = 3, name = "Vector Sport",        points = 109 },
                    new ChampionshipStandingEntry { position = 4, name = "Prema Racing",        points = 88  },
                    new ChampionshipStandingEntry { position = 5, name = "Inter Europol",       points = 74  },
                },
            },
            new ChampionshipStandingTable
            {
                type = StandingType.Drivers, label = "Pilotes — GT3",
                entries = new List<ChampionshipStandingEntry>
                {
                    new ChampionshipStandingEntry { position = 1, name = "Cairoli",    points = 91, teamName = "Manthey EMA" },
                    new ChampionshipStandingEntry { position = 2, name = "Pera",       points = 84, teamName = "Iron Dames" },
                    new ChampionshipStandingEntry { position = 3, name = "Schiavoni",  points = 75, teamName = "Vista AF Corse" },
                    new ChampionshipStandingEntry { position = 4, name = "Farfus",     points = 69, teamName = "WRT BMW" },
                    new ChampionshipStandingEntry { position = 5, name = "Rovera",     points = 62, teamName = "Vista AF Corse" },
                },
            },
            new ChampionshipStandingTable
            {
                type = StandingType.Teams, label = "Equipes — GT3",
                entries = new List<ChampionshipStandingEntry>
                {
                    new ChampionshipStandingEntry { position = 1, name = "Manthey EMA",    points = 139 },
                    new ChampionshipStandingEntry { position = 2, name = "Vista AF Corse",  points = 128 },
                    new ChampionshipStandingEntry { position = 3, name = "WRT BMW",         points = 111 },
                    new ChampionshipStandingEntry { position = 4, name = "Iron Dames",      points = 97  },
                    new ChampionshipStandingEntry { position = 5, name = "Akkodis ASP",     points = 79  },
                },
            },
        },
        replays = new ChampionshipReplays { label = "Toutes les courses de la saison" },
    };

    // ─────────────────────────────────────────────────────────────────────────────
    // MotoGP — event weekend (circuit/weather + session programme + standings)
    // ─────────────────────────────────────────────────────────────────────────────

    public static ChampionshipData MotoGpMock => new ChampionshipData
    {
        id = "motogp",
        name = "MotoGP",
        headline = "Gran Premio d'Italia",
        accentColor = Hex(0xE8, 0x77, 0x22),
        state = ChampionshipState.EventWeekend,
        circuit = new ChampionshipCircuit
        {
            name = "Autodromo del Mugello",
            location = "Scarperia e San Piero · Italie",
            lengthKm = 5.245f,
            totalLaps = 23,
        },
        weather = new ChampionshipWeather
        {
            condition = "Nuageux",
            trackTempC = 28,
            airTempC = 18,
            rainChancePercent = 10,
        },
        schedule = new List<ChampionshipSession>
        {
            new ChampionshipSession { name = "EL1",            scheduledAt = new DateTime(2026, 5, 22, 9, 0, 0),  status = SessionStatus.Completed },
            new ChampionshipSession { name = "EL2",            scheduledAt = new DateTime(2026, 5, 22, 13, 30, 0), status = SessionStatus.Completed },
            new ChampionshipSession { name = "EL3",            scheduledAt = new DateTime(2026, 5, 23, 10, 0, 0),  status = SessionStatus.Upcoming },
            new ChampionshipSession { name = "Qualifications", scheduledAt = new DateTime(2026, 5, 23, 14, 0, 0),  status = SessionStatus.Upcoming },
            new ChampionshipSession { name = "Course",         scheduledAt = new DateTime(2026, 5, 24, 14, 0, 0),  status = SessionStatus.Upcoming },
        },
        standings = new List<ChampionshipStandingTable>
        {
            new ChampionshipStandingTable
            {
                type = StandingType.Drivers, label = "Pilotes",
                entries = new List<ChampionshipStandingEntry>
                {
                    new ChampionshipStandingEntry { position = 1, name = "Bagnaia",     points = 138, teamName = "Ducati Lenovo" },
                    new ChampionshipStandingEntry { position = 2, name = "Marquez",     points = 121, teamName = "Gresini Racing" },
                    new ChampionshipStandingEntry { position = 3, name = "Bastianini",  points = 98,  teamName = "Ducati Lenovo" },
                    new ChampionshipStandingEntry { position = 4, name = "Martin",      points = 87,  teamName = "Aprilia Racing" },
                    new ChampionshipStandingEntry { position = 5, name = "Quartararo",  points = 76,  teamName = "Monster Yamaha" },
                },
            },
        },
        replays = new ChampionshipReplays { label = "Toutes les courses de la saison" },
    };

    public static List<ChampionshipData> Mocks => new List<ChampionshipData> { Formula1Mock, WecMock, MotoGpMock };

    public static ChampionshipData ById(string id)
    {
        foreach (var data in Mocks)
            if (data.id == id) return data;
        return Formula1Mock;
    }

    /// <summary>Short label for the home nav selector buttons (not part of the ported Dart model).</summary>
    public static string ShortLabel(string id)
    {
        switch (id)
        {
            case "formula_1": return "F1";
            case "wec": return "WEC";
            case "motogp": return "MotoGP";
            default: return id;
        }
    }
}
