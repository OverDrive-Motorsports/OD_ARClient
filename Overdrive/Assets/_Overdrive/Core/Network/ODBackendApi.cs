/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODBackendApi - Typed gateway endpoints used by the championship panel
 ## and the replays. One method per route, returning DTOs exactly as
 ## the backend sends them. Route list: OD_Backend/gateway/ROUTES.md
 ##
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

public static class ODBackendApi
{
    const string Championship = "/v1/championship";
    const string RaceData = "/v1/race-data";

    static string Esc(string value) => UnityWebRequest.EscapeURL(value ?? "");

    // ── championship-service ────────────────────────────────────────────────

    public static Task<List<ChampionshipSummaryDto>> GetChampionships()
        => ODApiClient.Get<List<ChampionshipSummaryDto>>($"{Championship}/championships");

    /// <summary>Every event of the championship, all seasons.</summary>
    public static Task<List<EventSummaryDto>> GetEvents(string championshipCode)
        => ODApiClient.Get<List<EventSummaryDto>>($"{Championship}/championships/{Esc(championshipCode)}/events");

    public static Task<List<SessionSummaryDto>> GetEventSessions(string eventId)
        => ODApiClient.Get<List<SessionSummaryDto>>($"{Championship}/events/{Esc(eventId)}/sessions");

    public static Task<List<DriverSummaryDto>> GetSessionDrivers(string sessionId)
        => ODApiClient.Get<List<DriverSummaryDto>>($"{Championship}/sessions/{Esc(sessionId)}/drivers");

    public static Task<SessionDatasetDto<DriverStandingRowDto>> GetDriverStandings(string sessionId)
        => ODApiClient.Get<SessionDatasetDto<DriverStandingRowDto>>($"{Championship}/sessions/{Esc(sessionId)}/datasets/championship_drivers");

    public static Task<SessionDatasetDto<TeamStandingRowDto>> GetTeamStandings(string sessionId)
        => ODApiClient.Get<SessionDatasetDto<TeamStandingRowDto>>($"{Championship}/sessions/{Esc(sessionId)}/datasets/championship_teams");

    public static Task<SessionDatasetDto<SessionResultRowDto>> GetSessionResult(string sessionId)
        => ODApiClient.Get<SessionDatasetDto<SessionResultRowDto>>($"{Championship}/sessions/{Esc(sessionId)}/datasets/session_result");

    // ── race-data-service (same session IDs: it reads its catalog from championship-service) ──

    public static Task<List<RacePositionDto>> GetPositions(string sessionId)
        => ODApiClient.Get<List<RacePositionDto>>($"{RaceData}/sessions/{Esc(sessionId)}/race/position");

    public static Task<List<RaceLapsDto>> GetLaps(string sessionId)
        => ODApiClient.Get<List<RaceLapsDto>>($"{RaceData}/sessions/{Esc(sessionId)}/race/laps");

    public static Task<List<RaceStintDto>> GetStints(string sessionId)
        => ODApiClient.Get<List<RaceStintDto>>($"{RaceData}/sessions/{Esc(sessionId)}/race/stints");

    public static Task<List<RacePitStopDto>> GetPitStops(string sessionId)
        => ODApiClient.Get<List<RacePitStopDto>>($"{RaceData}/sessions/{Esc(sessionId)}/race/pitstops");

    public static Task<List<RaceWeatherSampleDto>> GetWeather(string sessionId)
        => ODApiClient.Get<List<RaceWeatherSampleDto>>($"{RaceData}/sessions/{Esc(sessionId)}/race/weather");

    /// <summary>Sessions of the championship that can be replayed, newest first. Null when the championship is unknown.</summary>
    public static Task<List<ReplayCatalogEntryDto>> GetReplays(string championshipCode)
        => ODApiClient.Get<List<ReplayCatalogEntryDto>>($"{RaceData}/championships/{Esc(championshipCode)}/replays");

    /// <summary>The whole session (telemetry with x/y/z, positions, laps, stints, pit stops, radio,
    /// race control, weather) written to <paramref name="filePath"/> as the backend's JSON.</summary>
    public static Task DownloadReplay(string sessionId, string filePath, IProgress<ulong> progress, CancellationToken token)
        => ODApiClient.Download($"{RaceData}/sessions/{Esc(sessionId)}/race/replay", filePath, progress, token);
}
