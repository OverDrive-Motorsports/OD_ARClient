/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODChampionshipMapper - Pure DTO -> Entity conversions for the
 ## championship panel and the replay gallery. No network, no Unity scene
 ## access: everything it needs is passed in, so it's trivially testable.
 ##
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class ODChampionshipMapper
{
    // ═══════════════════════════════════════════════════════════════════════
    // Off-season
    // ═══════════════════════════════════════════════════════════════════════

    /// <param name="events">Every event of the championship, any order.</param>
    public static OffSeasonData ToOffSeason(ChampionshipSummaryDto championship, string championshipCode,
        List<EventSummaryDto> events, DateTime nowUtc, SeasonStandingCategory standings, int maxCalendar = 6)
    {
        var upcoming = (events ?? new List<EventSummaryDto>())
            .Where(e => e.startDate > nowUtc && e.status != "finished")
            .OrderBy(e => e.startDate)
            .ToList();

        var data = new OffSeasonData
        {
            championshipId = championshipCode,
            championshipName = ChampionshipName(championship, championshipCode),
            headline = ChampionshipName(championship, championshipCode),
            accentColor = AccentFor(championshipCode),
        };
        if (upcoming.Count > 0) data.nextEvent = ToOffSeasonEvent(upcoming[0]);
        foreach (var evt in upcoming.Skip(1).Take(maxCalendar)) data.calendar.Add(ToOffSeasonEvent(evt));
        if (standings != null) data.standings.Add(standings);
        return data;
    }

    public static OffSeasonEvent ToOffSeasonEvent(EventSummaryDto evt) => new OffSeasonEvent
    {
        name = evt.name,
        location = EventLocation(evt),
        round = evt.roundNumber ?? 0,
        startsAt = evt.startDate.ToLocalTime(),
    };

    // ═══════════════════════════════════════════════════════════════════════
    // Standings — "championship_drivers" / "championship_teams" datasets
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Single-class championship: one category with an empty label. Null when both lists are empty.</summary>
    public static SeasonStandingCategory ToStandings(List<DriverStandingRowDto> driverRows, List<TeamStandingRowDto> teamRows,
        Dictionary<int, DriverSummaryDto> drivers)
    {
        var category = new SeasonStandingCategory { label = "" };

        if (driverRows != null)
            foreach (var row in driverRows.Where(r => r != null).OrderBy(r => r.position_current ?? int.MaxValue))
            {
                drivers.TryGetValue(row.driver_number, out var driver);
                category.drivers.Add(new SeasonStandingEntry
                {
                    position = row.position_current ?? category.drivers.Count + 1,
                    name = DriverName(driver, row.driver_name, row.driver_number),
                    teamName = driver?.teamName ?? row.team_name ?? "",
                    points = row.points_current ?? 0f,
                });
            }

        if (teamRows != null)
            foreach (var row in teamRows.Where(r => r != null).OrderBy(r => r.position_current ?? int.MaxValue))
                category.teams.Add(new SeasonStandingEntry
                {
                    position = row.position_current ?? category.teams.Count + 1,
                    name = row.team_name ?? "",
                    points = row.points_current ?? 0f,
                });

        return category.drivers.Count == 0 && category.teams.Count == 0 ? null : category;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Event weekend
    // ═══════════════════════════════════════════════════════════════════════

    public static WeekendData ToWeekend(string championshipCode, EventSummaryDto evt, List<SessionSummaryDto> sessions,
        RaceWeatherSampleDto weather, SeasonStandingCategory standings)
    {
        var data = new WeekendData
        {
            championshipId = championshipCode,
            eventName = evt.name,
            accentColor = AccentFor(championshipCode),
            circuit = new WeekendCircuit
            {
                name = string.IsNullOrEmpty(evt.circuit) ? evt.location : evt.circuit,
                location = JoinNonEmpty(" · ", evt.circuit != null && evt.location != evt.circuit ? evt.location : null, evt.countryName),
                // Not exposed by the backend yet — the panel shows "--".
                lengthKm = 0f,
                totalLaps = 0,
            },
            weather = ToWeather(weather),
        };

        if (sessions != null)
            foreach (var session in sessions.OrderBy(s => s.startTime))
                data.sessions.Add(new WeekendSession
                {
                    name = SessionName(session),
                    startsAt = session.startTime.ToLocalTime(),
                    status = session.status == "finished" ? WeekendSessionStatus.Completed
                             : session.status == "live" ? WeekendSessionStatus.Live
                                                            : WeekendSessionStatus.Upcoming,
                });

        if (standings != null) data.standings.Add(standings);
        return data;
    }

    /// <summary>The backend only has a rainfall flag (no forecast %): -1 means "unknown", shown as "--".</summary>
    public static WeekendWeather? ToWeather(RaceWeatherSampleDto sample)
    {
        if (sample == null) return null;
        return new WeekendWeather
        {
            airTempC = sample.airTemperature ?? float.NaN,
            trackTempC = sample.trackTemperature ?? float.NaN,
            windKmh = sample.windSpeed.HasValue ? sample.windSpeed.Value * 3.6f : -1f,
            rainChancePercent = sample.rainfall ? 100 : -1,
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Live session
    // ═══════════════════════════════════════════════════════════════════════

    public static LiveSessionData ToLive(string championshipCode, EventSummaryDto evt, SessionSummaryDto session,
        List<RacePositionDto> positions, Dictionary<int, DriverSummaryDto> drivers, List<RaceStintDto> stints,
        List<RacePitStopDto> pitStops, List<RaceLapsDto> laps, RaceWeatherSampleDto weather)
    {
        var ordered = (positions ?? new List<RacePositionDto>()).Where(p => p != null).OrderBy(p => p.position).ToList();
        int leaderLaps = ordered.Count > 0 ? ordered[0].lapsCompleted : 0;

        var bestLaps = laps?.Where(l => l?.bestLap != null).Select(l => l.bestLap.Value).ToList();
        float? fastest = bestLaps != null && bestLaps.Count > 0 ? bestLaps.Min() : (float?)null;

        var data = new LiveSessionData
        {
            championshipId = championshipCode,
            sessionName = SessionName(session),
            circuitName = string.IsNullOrEmpty(evt?.circuit) ? evt?.location : evt.circuit,
            conditions = Conditions(weather),
            accentColor = AccentFor(championshipCode),
            currentLap = session.status == "finished" ? leaderLaps : leaderLaps + 1,
            totalLaps = 0,      // not exposed by the backend yet
            trackStatus = null,   // race control is long-poll only; not wired yet
        };

        foreach (var p in ordered)
        {
            drivers.TryGetValue(p.driverNumber, out var driver);
            var stint = stints?.Where(s => s.driverNumber == p.driverNumber).OrderByDescending(s => s.stintNumber).FirstOrDefault();
            var driverLap = laps?.FirstOrDefault(l => l != null && l.driverNumber == p.driverNumber);
            var lastLap = driverLap?.laps?.Where(l => l.lapDuration.HasValue).OrderBy(l => l.lapNumber).LastOrDefault();

            data.entries.Add(new LiveEntry
            {
                position = p.position,
                name = DriverName(driver, null, p.driverNumber),
                code = driver?.code,
                teamName = driver?.teamName ?? "",
                teamColor = ParseColor(driver?.teamColor),
                gap = p.position == 1 ? "Leader" : FormatGap(p.gapToLeader),
                tyreCompound = CompoundLetter(stint?.compound),
                tyreAgeLaps = stint != null ? stint.tyreAgeAtStart + Mathf.Max(0, p.lapsCompleted - stint.lapStart) : -1,
                pitStops = pitStops?.Count(s => s.driverNumber == p.driverNumber) ?? 0,
                lastLap = lastLap != null ? FormatLapTime(lastLap.lapDuration.Value) : "",
                isFastestLap = fastest.HasValue && driverLap?.bestLap != null && Mathf.Approximately(driverLap.bestLap.Value, fastest.Value),
            });
        }
        return data;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Replays
    // ═══════════════════════════════════════════════════════════════════════

    public static ReplaySummary ToReplaySummary(ChampionshipSummaryDto championship, string championshipCode,
        EventSummaryDto evt, SessionSummaryDto session) => new ReplaySummary
        {
            id = session.sessionId,
            championshipId = championshipCode,
            championshipName = ChampionshipName(championship, championshipCode),
            grandPrix = evt.name,
            sessionName = SessionName(session),
            season = (evt.seasonYear > 0 ? evt.seasonYear : session.startTime.Year).ToString(),
            circuit = string.IsNullOrEmpty(evt.circuit) ? evt.location : evt.circuit,
            date = session.startTime.ToLocalTime(),
        };

    public static ReplayDetail ToReplayDetail(ReplaySummary summary, EventSummaryDto evt,
        List<SessionResultRowDto> results, Dictionary<int, DriverSummaryDto> drivers)
    {
        var detail = new ReplayDetail
        {
            summary = summary,
            location = evt != null ? EventLocation(evt) : summary.circuit,
        };
        if (results == null) return detail;

        foreach (var row in results.Where(r => r != null).OrderBy(r => r.position ?? int.MaxValue))
        {
            drivers.TryGetValue(row.driver_number, out var driver);
            detail.results.Add(new LiveEntry
            {
                position = row.position ?? detail.results.Count + 1,
                name = DriverName(driver, row.driver_name, row.driver_number),
                code = driver?.code,
                teamName = driver?.teamName ?? row.team_name ?? "",
                teamColor = ParseColor(driver?.teamColor ?? row.team_color),
                tyreAgeLaps = -1,
                pitStops = -1,
                gap = row.dnf ? "Abandon" : row.dns ? "Non partant" : row.dsq ? "Disqualifié"
                          : row.position == 1 ? "1er" : FormatGap(row.gap_to_leader),
            });
            if (row.position == 1 && row.number_of_laps.HasValue) detail.totalLaps = row.number_of_laps.Value;
        }
        return detail;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Shared formatting
    // ═══════════════════════════════════════════════════════════════════════

    public static Dictionary<int, DriverSummaryDto> DriverLookup(List<DriverSummaryDto> drivers)
    {
        var lookup = new Dictionary<int, DriverSummaryDto>();
        if (drivers != null)
            foreach (var d in drivers)
                if (d != null) lookup[d.driverNumber] = d;
        return lookup;
    }

    /// <summary>"Verstappen" — last name in title case, whatever the source format ("Max VERSTAPPEN", "M VERSTAPPEN").</summary>
    public static string DriverName(DriverSummaryDto driver, string fallbackName, int number)
    {
        string raw = !string.IsNullOrWhiteSpace(driver?.lastName) ? driver.lastName
                   : !string.IsNullOrWhiteSpace(driver?.fullName) ? driver.fullName
                   : fallbackName;
        if (string.IsNullOrWhiteSpace(raw)) return "#" + number;
        string last = raw.Trim().Split(' ').Last();
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(last.ToLowerInvariant());
    }

    /// <summary>Backend session types → French labels used across the app.</summary>
    public static string SessionName(SessionSummaryDto session)
    {
        if (session == null) return "Session";
        string name = session.name ?? "";
        switch (session.type)
        {
            case "race": return "Course";
            case "sprint": return name.IndexOf("quali", StringComparison.OrdinalIgnoreCase) >= 0 ? "Qualifs sprint" : "Sprint";
            case "quali": return "Qualifications";
            case "practice":
                string digits = new string(name.Where(char.IsDigit).ToArray());
                return string.IsNullOrEmpty(digits) ? "Essais libres" : "EL" + digits;
            default: return string.IsNullOrEmpty(name) ? "Session" : name;
        }
    }

    public static string EventLocation(EventSummaryDto evt) =>
        JoinNonEmpty(" · ", string.IsNullOrEmpty(evt.circuit) ? evt.location : evt.circuit, evt.countryName);

    static string Conditions(RaceWeatherSampleDto weather)
    {
        if (weather == null) return "";
        string temp = weather.trackTemperature.HasValue ? $"Piste {weather.trackTemperature.Value:0}°C"
                    : weather.airTemperature.HasValue ? $"{weather.airTemperature.Value:0}°C" : "";
        return JoinNonEmpty(" · ", temp, weather.rainfall ? "Pluie" : "Sec");
    }

    /// <summary>
    /// session_result "gap_to_leader" as the backend sends it (raw OpenF1): seconds as a number,
    /// "+1 LAP" as a string, or [Q1, Q2, Q3] for qualifying (the last segment reached counts).
    /// Numbers are formatted invariantly: ToString() would give "22,457" on a French headset.
    /// </summary>
    public static string FormatGap(object raw)
    {
        switch (raw)
        {
            case null:
                return "";
            case JArray segments:
                var reached = segments.LastOrDefault(t => t != null && t.Type != JTokenType.Null);
                return reached != null ? FormatGap((object)reached) : "";
            case JValue value:
                return FormatGap(value.Value);
            case string text:
                return FormatGap(text);
            case IConvertible number:
                return FormatGap(Convert.ToDouble(number, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
            default:
                return "";
        }
    }

    /// <summary>"3.456" → "+3.5s"; "+1 LAP" → "+1 tour"; empty → "".</summary>
    public static string FormatGap(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        string s = raw.Trim().TrimStart('+');
        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
            return "+" + seconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
        if (s.ToUpperInvariant().Contains("LAP"))
        {
            string n = new string(s.Where(char.IsDigit).ToArray());
            return "+" + (string.IsNullOrEmpty(n) ? "1" : n) + (n == "1" || n == "" ? " tour" : " tours");
        }
        return raw;
    }

    public static string FormatLapTime(float seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);
        return $"{(int)t.TotalMinutes}:{t.Seconds:00}.{t.Milliseconds:000}";
    }

    static string CompoundLetter(string compound)
    {
        if (string.IsNullOrEmpty(compound)) return "";
        switch (compound.ToUpperInvariant())
        {
            case "SOFT": return "S";
            case "MEDIUM": return "M";
            case "HARD": return "H";
            case "INTERMEDIATE": return "I";
            case "WET": return "W";
            default: return compound.Substring(0, 1).ToUpperInvariant();
        }
    }

    public static Color ParseColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Color.white;
        string value = hex.StartsWith("#") ? hex : "#" + hex;
        return ColorUtility.TryParseHtmlString(value, out var color) ? color : Color.white;
    }

    static string ChampionshipName(ChampionshipSummaryDto championship, string code)
    {
        if (!string.IsNullOrEmpty(championship?.name)) return championship.name;
        return code == "f1" ? "Formule 1" : (code ?? "").ToUpperInvariant();
    }

    static Color AccentFor(string code) => code == "f1" ? new Color32(0xE8, 0x00, 0x00, 0xFF) : new Color32(0x4A, 0x90, 0xD9, 0xFF);

    static string JoinNonEmpty(string separator, params string[] parts) =>
        string.Join(separator, parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}
