/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODChampionshipDataService - Loads what the championship panel and the
 ## replay gallery display, from the backend (Core) or from the mobile mocks
 ## (Data/Mock), and decides the panel state from the event/session statuses.
 ##
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public enum ChampionshipViewState
{
    OffSeason,
    Weekend,
    Live
}

/// <summary>Everything the panel needs for the state the backend is in right now.</summary>
public class ChampionshipSnapshot
{
    public ChampionshipViewState state;
    public OffSeasonData offSeason;
    public WeekendData weekend;
    public LiveSessionData live;
    /// <summary>False when the backend answered but holds no event for the championship.</summary>
    public bool hasData = true;

    // Kept so the live state can be refreshed without re-resolving the catalog.
    public EventSummaryDto liveEvent;
    public SessionSummaryDto liveSession;
}

/// <summary>
/// Backend vs demo data. Starts on the backend; switches to the mobile mocks
/// for the rest of the app session once the user picks them in the error popup.
/// </summary>
public static class ODDataSource
{
    public static bool UseMocks { get; set; }
}

public static class ODChampionshipDataService
{
    /// <summary>How many past events are scanned for standings / replays (one request each).</summary>
    const int MaxPastEventsScanned = 12;

    static string Code => ODApiConfig.Instance.championshipCode;

    // ═══════════════════════════════════════════════════════════════════════
    // Championship panel
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// live event + live session → Live; event in progress → Weekend;
    /// otherwise → OffSeason. Throws ODApiException when the backend fails.
    /// </summary>
    public static async Task<ChampionshipSnapshot> LoadAsync()
    {
        DateTime now = DateTime.UtcNow;
        var championships = await ODBackendApi.GetChampionships();
        var championship = championships?.FirstOrDefault(c => c.championshipCode == Code);
        var events = (await ODBackendApi.GetEvents(Code) ?? new List<EventSummaryDto>()).OrderBy(e => e.startDate).ToList();

        // The session pinned in ODApiConfig wins when it's live, whatever the event dates say.
        var pinned = await LoadPinnedAsync();
        if (pinned.session != null && pinned.session.status == "live" && pinned.evt != null)
            return new ChampionshipSnapshot
            {
                hasData = true,
                state = ChampionshipViewState.Live,
                live = await LoadLiveAsync(pinned.evt, pinned.session),
                liveEvent = pinned.evt,
                liveSession = pinned.session,
            };

        var current = events.FirstOrDefault(e => e.status == "live")
                   ?? events.FirstOrDefault(e => e.startDate <= now && now <= e.endDate && e.status != "finished");

        if (current != null)
        {
            var sessions = await ODBackendApi.GetEventSessions(current.eventId) ?? new List<SessionSummaryDto>();
            var liveSession = sessions.FirstOrDefault(s => s.status == "live");

            if (liveSession != null)
                return new ChampionshipSnapshot
                {
                    state = ChampionshipViewState.Live,
                    live = await LoadLiveAsync(current, liveSession),
                    liveEvent = current,
                    liveSession = liveSession,
                };

            var standings = await LoadSeasonStandingsAsync(events, now);
            var weather = await LoadLatestWeatherAsync(sessions);
            return new ChampionshipSnapshot
            {
                state = ChampionshipViewState.Weekend,
                weekend = ODChampionshipMapper.ToWeekend(Code, current, sessions, weather, standings),
            };
        }

        return new ChampionshipSnapshot
        {
            hasData = events.Count > 0,
            state = ChampionshipViewState.OffSeason,
            offSeason = ODChampionshipMapper.ToOffSeason(championship, Code, events, now, await LoadSeasonStandingsAsync(events, now)),
        };
    }

    /// <summary>Called again every few seconds while the live state is shown.</summary>
    public static async Task<LiveSessionData> LoadLiveAsync(EventSummaryDto evt, SessionSummaryDto session)
    {
        string id = session.sessionId;
        var positionsTask = ODBackendApi.GetPositions(id);
        var driversTask = ODBackendApi.GetSessionDrivers(id);
        var stintsTask = ODBackendApi.GetStints(id);
        var pitsTask = ODBackendApi.GetPitStops(id);
        var lapsTask = ODBackendApi.GetLaps(id);
        var weatherTask = ODBackendApi.GetWeather(id);
        await Task.WhenAll(positionsTask, driversTask, stintsTask, pitsTask, lapsTask, weatherTask);

        return ODChampionshipMapper.ToLive(Code, evt, session, positionsTask.Result,
            ODChampionshipMapper.DriverLookup(driversTask.Result), stintsTask.Result, pitsTask.Result,
            lapsTask.Result, weatherTask.Result?.OrderBy(w => w.timestamp).LastOrDefault());
    }

    /// <summary>Standings after the most recent finished race that has standings data.</summary>
    public static async Task<SeasonStandingCategory> LoadSeasonStandingsAsync(List<EventSummaryDto> events, DateTime now)
    {
        var past = events.Where(e => e.status == "finished" || e.endDate < now)
                         .OrderByDescending(e => e.startDate)
                         .Take(MaxPastEventsScanned);

        foreach (var evt in past)
        {
            var sessions = await ODBackendApi.GetEventSessions(evt.eventId);
            var races = sessions?.Where(s => s.type == "race" && s.status == "finished").OrderByDescending(s => s.startTime);
            if (races == null) continue;

            foreach (var race in races)
            {
                var driversTask = ODBackendApi.GetDriverStandings(race.sessionId);
                var teamsTask = ODBackendApi.GetTeamStandings(race.sessionId);
                var lookupTask = ODBackendApi.GetSessionDrivers(race.sessionId);
                await Task.WhenAll(driversTask, teamsTask, lookupTask);

                var standings = ODChampionshipMapper.ToStandings(driversTask.Result?.data, teamsTask.Result?.data,
                    ODChampionshipMapper.DriverLookup(lookupTask.Result));
                if (standings != null) return standings;
            }
        }
        return null;
    }

    /// <summary>Latest weather sample among the weekend's sessions that already have data.</summary>
    static async Task<RaceWeatherSampleDto> LoadLatestWeatherAsync(List<SessionSummaryDto> sessions)
    {
        foreach (var session in sessions.Where(s => s.status != "scheduled").OrderByDescending(s => s.startTime))
        {
            var samples = await ODBackendApi.GetWeather(session.sessionId);
            var latest = samples?.OrderBy(w => w.timestamp).LastOrDefault();
            if (latest != null) return latest;
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Replays
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Finished races and sprints, most recent first.</summary>
    public static async Task<List<ReplaySummary>> LoadReplaysAsync()
    {
        DateTime now = DateTime.UtcNow;
        var championships = await ODBackendApi.GetChampionships();
        var championship = championships?.FirstOrDefault(c => c.championshipCode == Code);
        var events = await ODBackendApi.GetEvents(Code) ?? new List<EventSummaryDto>();

        var replays = new List<ReplaySummary>();

        // The session pinned in ODApiConfig comes first, whatever its type (it may not be a race).
        var pinned = await LoadPinnedAsync();
        if (pinned.session != null && pinned.evt != null && pinned.session.status != "scheduled")
            replays.Add(ODChampionshipMapper.ToReplaySummary(championship, Code, pinned.evt, pinned.session));

        foreach (var evt in events.Where(e => e.status == "finished" || e.endDate < now)
                                  .OrderByDescending(e => e.startDate)
                                  .Take(MaxPastEventsScanned))
        {
            var sessions = await ODBackendApi.GetEventSessions(evt.eventId);
            if (sessions == null) continue;
            foreach (var session in sessions.Where(s => (s.type == "race" || s.type == "sprint") && s.status == "finished")
                                            .OrderByDescending(s => s.startTime))
                if (replays.All(r => r.id != session.sessionId))
                    replays.Add(ODChampionshipMapper.ToReplaySummary(championship, Code, evt, session));
        }
        return replays;
    }

    /// <summary>Session set in ODApiConfig.sessionKey and its event; nulls when unset or unknown to the backend (404).</summary>
    static async Task<(SessionSummaryDto session, EventSummaryDto evt)> LoadPinnedAsync()
    {
        string id = ODApiConfig.Instance.PinnedSessionId;
        if (id == null) return (null, null);
        var session = await ODBackendApi.GetSession(id);
        if (session == null) return (null, null);
        var evt = string.IsNullOrEmpty(session.eventId) ? null : await ODBackendApi.GetEvent(session.eventId);
        return (session, evt);
    }

    public static async Task<ReplayDetail> LoadReplayDetailAsync(ReplaySummary summary)
    {
        var resultTask = ODBackendApi.GetSessionResult(summary.id);
        var driversTask = ODBackendApi.GetSessionDrivers(summary.id);
        await Task.WhenAll(resultTask, driversTask);
        return ODChampionshipMapper.ToReplayDetail(summary, null, resultTask.Result?.data,
            ODChampionshipMapper.DriverLookup(driversTask.Result));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Errors → user-facing text
    // ═══════════════════════════════════════════════════════════════════════

    public static string Title(Exception error)
    {
        if (!(error is ODApiException api)) return "Erreur";
        switch (api.Kind)
        {
            case ODApiErrorKind.Unreachable: return "Backend injoignable";
            case ODApiErrorKind.Unauthorized: return "Accès refusé";
            case ODApiErrorKind.InvalidResponse: return "Réponse invalide";
            default: return "Erreur du backend";
        }
    }

    public static string Describe(Exception error)
    {
        if (error is ODApiException api)
            switch (api.Kind)
            {
                case ODApiErrorKind.Unreachable:
                    return $"Impossible de joindre le backend ({ODApiConfig.Instance.BaseUrl}). Vérifie qu'il est lancé et accessible depuis cet appareil.";
                case ODApiErrorKind.Unauthorized:
                    return "Le gateway refuse la connexion (401). Vérifie le token dans ODApiConfig.";
                case ODApiErrorKind.InvalidResponse:
                    return "Le backend a répondu dans un format inattendu.";
                default:
                    return $"Le backend a renvoyé une erreur (HTTP {api.StatusCode}).";
            }
        return "Erreur inattendue : " + error.Message;
    }
}
