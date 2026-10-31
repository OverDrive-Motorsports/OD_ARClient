/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODHomeDataService - Loads what the Home displays, across every
 ## championship the backend serves, and decides which Home it is:
 ## a running session (Live, 2.1a), an event weekend between two sessions
 ## (Weekend, 2.1b) or the next event (NoRace, 2.1c). Also the other events
 ## of the week, the latest finished races and each championship's leader.
 ## Falls back to the existing mobile mocks only when the user picked the
 ## demo data in the error popup.
 ##
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class ODHomeDataService
{
    /// <summary>Replay tiles of the "À revoir" row.</summary>
    public const int MaxReplays = 3;
    /// <summary>Rows of the "Tes championnats" card.</summary>
    public const int MaxLeaders = 3;
    /// <summary>Past events scanned per championship for replays (one request each).</summary>
    const int PastEventsScanned = 3;

    /// <summary>Refills <paramref name="home"/> in place. Throws ODApiException when the backend fails.</summary>
    public static async Task LoadAsync(HomeData home)
    {
        DateTime now = DateTime.UtcNow;
        var championships = (await ODBackendApi.GetChampionships() ?? new List<ChampionshipSummaryDto>())
            .Where(c => c != null && !string.IsNullOrEmpty(c.championshipCode) && c.isActive)
            .GroupBy(c => c.championshipCode).Select(g => g.First())
            .ToList();
        // Older backends don't flag championships as active: fall back to the configured one.
        if (championships.Count == 0)
            championships.Add(new ChampionshipSummaryDto { championshipCode = ODApiConfig.Instance.championshipCode });

        var loads = championships.Select(c => LoadChampionshipAsync(c, now)).ToList();
        await Task.WhenAll(loads);
        var results = loads.Select(t => t.Result).ToList();

        Reset(home);
        home.replays.AddRange(results.SelectMany(r => r.replays)
                                     .OrderByDescending(r => r.summary.date)
                                     .Take(MaxReplays));
        foreach (var result in results)
            if (result.leader.HasValue && home.leaders.Count < MaxLeaders) home.leaders.Add(result.leader.Value);

        ChampionshipResult featured;
        EventSummaryDto featuredEvent;
        HomeAgendaItem? replayTile = null;

        var live = results.FirstOrDefault(r => r.liveSession != null);
        var weekend = results.Where(r => r.current != null).OrderBy(r => NextStart(r.currentSessions, now)).FirstOrDefault();
        if (live != null)
        {
            featured = live;
            featuredEvent = live.current;
            home.state = HomeState.Live;
            home.nextEvent = ODHomeMapper.ToLiveEvent(live.championship, live.Code, live.current);
            home.liveChampionshipCode = live.Code;
            home.liveEvent = live.current;
            home.liveSession = live.liveSession;
            home.live = await LoadLiveAsync(live.Code, live.current, live.liveSession);
        }
        else if (weekend != null)
        {
            featured = weekend;
            featuredEvent = weekend.current;
            home.state = HomeState.Weekend;
            home.nextEvent = ODHomeMapper.ToWeekendEvent(weekend.championship, weekend.Code, weekend.current, weekend.currentSessions, now);

            // What just happened, and its replay first in the row.
            var last = weekend.currentSessions.Where(s => s.status == "finished").OrderBy(s => s.startTime).LastOrDefault();
            if (last != null)
            {
                string session = ODChampionshipMapper.SessionName(last);
                string winner = await WinnerAsync(last);
                home.nextEvent.headline = ODHomeMapper.Headline(session, winner);
                home.replays.RemoveAll(r => r.summary.id == last.sessionId);
                home.replays.Insert(0, ODHomeMapper.ToReplay(weekend.championship, weekend.Code, weekend.current, last));
                if (home.replays.Count > MaxReplays) home.replays.RemoveAt(home.replays.Count - 1);
                var tile = ODHomeMapper.ToAgendaItem(weekend.championship, weekend.Code, weekend.current);
                tile.title = ODHomeMapper.SessionLongName(last);
                tile.replayIndex = 0;
                tile.detail = ODHomeMapper.ResultLabel(session, winner);
                replayTile = tile;
            }
        }
        else
        {
            featured = results.Where(r => r.nextEvent != null).OrderBy(r => r.nextEvent.startDate).FirstOrDefault();
            featuredEvent = featured?.nextEvent;
            if (featured != null)
            {
                var sessions = await ODBackendApi.GetEventSessions(featured.nextEvent.eventId);
                home.nextEvent = ODHomeMapper.ToNextEvent(featured.championship, featured.Code, featured.nextEvent, sessions, now);
            }
        }
        if (featured != null) ODHomeMapper.SetRound(home.nextEvent, featured.events, featuredEvent);

        if (home.state != HomeState.NoRace)
            home.agenda.AddRange(ODHomeMapper.Agenda(
                results.SelectMany(r => r.upcoming.Where(e => e.eventId != featuredEvent?.eventId)
                                                  .Select(e => ODHomeMapper.ToAgendaItem(r.championship, r.Code, e))),
                replayTile, now.ToLocalTime()));
    }

    /// <summary>Running order of the live session (no WebSocket on the backend yet: the page calls it again every few seconds).</summary>
    public static async Task RefreshLiveAsync(HomeData home)
    {
        if (home.liveSession == null) return;
        home.live = await LoadLiveAsync(home.liveChampionshipCode, home.liveEvent, home.liveSession);
    }

    /// <summary>Fills <paramref name="home"/> from the mobile mocks already used by the championship pages.</summary>
    public static void LoadDemo(HomeData home, HomeState state)
    {
        var wec = ODOffSeasonMockData.Wec;
        var motoGp = ODWeekendMockData.MotoGp;

        Reset(home);
        home.state = state;
        home.replays.AddRange(ODReplayMockData.Catalog.Take(MaxReplays).Select(ODHomeMapper.ToReplay));
        foreach (var leader in new[]
                 {
                     ODHomeMapper.ToLeader(motoGp.championshipId, motoGp.standings.FirstOrDefault()),
                     ODHomeMapper.ToLeader(wec.championshipId, wec.standings.FirstOrDefault()),
                 })
            if (leader.HasValue) home.leaders.Add(leader.Value);

        HomeAgendaItem? replayTile = null;
        switch (state)
        {
            case HomeState.Live:
                home.live = ODLiveSessionMockData.Formula1;
                home.nextEvent = ODHomeMapper.ToLiveEvent(home.live, "Formule 1", "Grand Prix de Monaco");
                break;
            case HomeState.Weekend:
                home.nextEvent = ODHomeMapper.ToWeekendEvent(motoGp, "MotoGP");
                var replay = home.replays.Count > 0 ? home.replays[0].summary : default;
                replayTile = new HomeAgendaItem
                {
                    championshipId = replay.championshipId,
                    championshipName = replay.championshipName,
                    title = replay.sessionName,
                    replayIndex = 0,
                };
                break;
            default:
                home.nextEvent = ODHomeMapper.ToNextEvent(wec);
                return;
        }

        var events = new List<OffSeasonEvent>(wec.calendar);
        if (wec.nextEvent.HasValue) events.Insert(0, wec.nextEvent.Value);
        home.agenda.AddRange(ODHomeMapper.Agenda(events.Select(e => ODHomeMapper.ToAgendaItem(wec, e)),
            home.replays.Count > 0 ? replayTile : null, DateTime.Now));
    }

    static void Reset(HomeData home)
    {
        home.state = HomeState.NoRace;
        home.nextEvent = null;
        home.live = null;
        home.liveChampionshipCode = null;
        home.liveEvent = null;
        home.liveSession = null;
        home.agenda.Clear();
        home.replays.Clear();
        home.leaders.Clear();
    }

    sealed class ChampionshipResult
    {
        public ChampionshipSummaryDto championship;
        public string Code => championship.championshipCode;
        public List<EventSummaryDto> events;
        /// <summary>Event in progress (its dates include today, or flagged live) and its sessions.</summary>
        public EventSummaryDto current;
        public List<SessionSummaryDto> currentSessions = new();
        public SessionSummaryDto liveSession;
        /// <summary>Earliest event still to come or running.</summary>
        public EventSummaryDto nextEvent;
        /// <summary>The next few events still to come or running, for the week row.</summary>
        public List<EventSummaryDto> upcoming = new();
        public List<HomeReplay> replays = new();
        public HomeLeader? leader;
    }

    static async Task<ChampionshipResult> LoadChampionshipAsync(ChampionshipSummaryDto championship, DateTime now)
    {
        string code = championship.championshipCode;
        var events = (await ODBackendApi.GetEvents(code) ?? new List<EventSummaryDto>())
            .Where(e => e != null).OrderBy(e => e.startDate).ToList();
        // The end date can be the last day at midnight: the event runs until that day is over.
        var notOver = events.Where(e => e.status != "finished" && now < e.endDate.Date.AddDays(1)).ToList();
        var result = new ChampionshipResult
        {
            championship = championship,
            events = events,
            current = events.FirstOrDefault(e => e.status == "live") ?? notOver.FirstOrDefault(e => e.startDate <= now),
            nextEvent = notOver.FirstOrDefault(e => e.endDate >= now),
            upcoming = notOver.Take(ODHomeMapper.MaxAgenda + 1).ToList(),
        };

        var standingsTask = ODChampionshipDataService.LoadSeasonStandingsAsync(events, now);
        if (result.current != null)
        {
            result.currentSessions = await ODBackendApi.GetEventSessions(result.current.eventId) ?? new List<SessionSummaryDto>();
            result.liveSession = result.currentSessions.Where(s => s != null && s.status == "live")
                                                       .OrderBy(s => s.type == "race" ? 0 : 1).FirstOrDefault();
        }
        foreach (var evt in events.Where(e => e != result.current && (e.status == "finished" || e.endDate < now))
                                  .OrderByDescending(e => e.startDate)
                                  .Take(PastEventsScanned))
        {
            var sessions = await ODBackendApi.GetEventSessions(evt.eventId);
            if (sessions == null) continue;
            foreach (var session in sessions.Where(s => (s.type == "race" || s.type == "sprint") && s.status == "finished"))
                result.replays.Add(ODHomeMapper.ToReplay(championship, code, evt, session));
        }
        result.leader = ODHomeMapper.ToLeader(code, await standingsTask);
        return result;
    }

    static async Task<LiveSessionData> LoadLiveAsync(string code, EventSummaryDto evt, SessionSummaryDto session)
    {
        string id = session.sessionId;
        var positionsTask = ODBackendApi.GetPositions(id);
        var driversTask = ODBackendApi.GetSessionDrivers(id);
        var weatherTask = Optional(ODBackendApi.GetWeather(id));
        await Task.WhenAll(positionsTask, driversTask, weatherTask);
        return ODChampionshipMapper.ToLive(code, evt, session, positionsTask.Result,
            ODChampionshipMapper.DriverLookup(driversTask.Result), null, null, null,
            weatherTask.Result?.OrderBy(w => w.timestamp).LastOrDefault());
    }

    /// <summary>Earliest session still to come, so the closest weekend wins when two run at once.</summary>
    static DateTime NextStart(List<SessionSummaryDto> sessions, DateTime now) =>
        sessions.Where(s => s != null && s.status != "finished" && s.startTime >= now).Select(s => s.startTime)
                .DefaultIfEmpty(DateTime.MaxValue).Min();

    /// <summary>P1 of a finished session; null when the result isn't ingested (the headline then just says it's over).</summary>
    static async Task<string> WinnerAsync(SessionSummaryDto session)
    {
        var result = await Optional(ODBackendApi.GetSessionResult(session.sessionId));
        var first = result?.data?.FirstOrDefault(r => r != null && r.position == 1);
        return first == null ? null : ODChampionshipMapper.DriverName(null, first.driver_name, first.driver_number);
    }

    /// <summary>Extra data the Home can do without: a failed request gives null instead of an error popup.</summary>
    static async Task<T> Optional<T>(Task<T> request) where T : class
    {
        try { return await request; }
        catch (ODApiException) { return null; }
    }
}
