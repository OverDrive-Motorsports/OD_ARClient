/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODHomeMapper - DTOs and championship entities -> HomeData pieces (pure
 ## functions). The Home picks the key sessions of the featured event, says
 ## what just happened during a weekend, lists the other events of the week,
 ## sizes the replays and reads each leader off the season standings.
 ##
 */

using System;
using System.Collections.Generic;
using System.Linq;

public static class ODHomeMapper
{
    /// <summary>How many sessions the countdown card lists.</summary>
    public const int MaxSessions = 3;
    /// <summary>Tiles of the "Cette semaine" / "Aussi ce week-end" row.</summary>
    public const int MaxAgenda = 3;

    // ═══════════════════════════════════════════════════════════════════════
    // Featured event
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Next event (no race today): the next session first, then the races
    /// (sprint, main race) and, if room is left, the other sessions — all in
    /// chronological order.
    /// </summary>
    public static HomeNextEvent ToNextEvent(ChampionshipSummaryDto championship, string championshipCode,
        EventSummaryDto evt, List<SessionSummaryDto> sessions, DateTime nowUtc)
    {
        var data = Featured(championship, championshipCode, evt);
        AddKeySessions(data, Candidates(sessions, nowUtc).Where(c => !c.finished), false);
        if (data.sessions.Count > 0 && data.sessions[0].startsAt < data.startsAt) data.startsAt = data.sessions[0].startsAt;
        return data;
    }

    /// <summary>Event weekend: the last finished session, then the key upcoming ones (same rule as <see cref="ToNextEvent"/>).</summary>
    public static HomeNextEvent ToWeekendEvent(ChampionshipSummaryDto championship, string championshipCode,
        EventSummaryDto evt, List<SessionSummaryDto> sessions, DateTime nowUtc)
    {
        var data = Featured(championship, championshipCode, evt);
        AddKeySessions(data, Candidates(sessions, nowUtc), true);
        return data;
    }

    /// <summary>Running event: title, place and round only — the running order comes with the live data.</summary>
    public static HomeNextEvent ToLiveEvent(ChampionshipSummaryDto championship, string championshipCode, EventSummaryDto evt) =>
        Featured(championship, championshipCode, evt);

    /// <summary>"Manche 19 / 22": position of <paramref name="evt"/> in its season.</summary>
    public static void SetRound(HomeNextEvent data, List<EventSummaryDto> events, EventSummaryDto evt)
    {
        if (data == null || evt == null || events == null) return;
        var season = events.Where(e => e != null && e.seasonYear == evt.seasonYear).OrderBy(e => e.startDate).ToList();
        data.roundCount = season.Count;
        data.round = evt.roundNumber ?? season.FindIndex(e => e.eventId == evt.eventId) + 1;
    }

    static HomeNextEvent Featured(ChampionshipSummaryDto championship, string championshipCode, EventSummaryDto evt) => new HomeNextEvent
    {
        championshipId = championshipCode,
        championshipName = ODChampionshipMapper.ChampionshipName(championship, championshipCode),
        eventName = evt.name,
        location = ODChampionshipMapper.EventLocation(evt),
        place = Place(evt),
        startsAt = evt.startDate.ToLocalTime(),
    };

    static string Place(EventSummaryDto evt) => string.IsNullOrEmpty(evt.location) ? evt.circuit : evt.location;

    sealed class Candidate
    {
        public string name;
        public DateTime startsAt;   // local time
        public bool race, finished;
    }

    static IEnumerable<Candidate> Candidates(List<SessionSummaryDto> sessions, DateTime nowUtc) =>
        (sessions ?? new List<SessionSummaryDto>()).Where(s => s != null).OrderBy(s => s.startTime).Select(s => new Candidate
        {
            name = SessionLongName(s),
            startsAt = s.startTime.ToLocalTime(),
            race = IsRace(s),
            finished = s.status == "finished" || (s.endTime ?? s.startTime) < nowUtc,
        });

    static void AddKeySessions(HomeNextEvent data, IEnumerable<Candidate> chronological, bool withLastFinished)
    {
        var all = chronological.ToList();
        var upcoming = all.Where(c => !c.finished).ToList();
        var picked = new List<Candidate>();
        var last = withLastFinished ? all.LastOrDefault(c => c.finished) : null;
        if (last != null) picked.Add(last);
        if (upcoming.Count > 0)
        {
            picked.Add(upcoming[0]);
            foreach (var c in upcoming.Skip(1).Where(c => c.race).Reverse())
                if (picked.Count < MaxSessions) picked.Add(c);
            foreach (var c in upcoming.Skip(1).Where(c => !c.race))
                if (picked.Count < MaxSessions) picked.Add(c);
        }
        foreach (var c in picked.OrderBy(c => c.startsAt))
            data.sessions.Add(new HomeSession { name = c.name, startsAt = c.startsAt, finished = c.finished });
    }

    static bool IsRace(SessionSummaryDto session) =>
        session.type == "race" || (session.type == "sprint" && ODChampionshipMapper.SessionName(session) == "Sprint");

    /// <summary>Spelled-out session name ("EL1" reads "Essais libres 1" on the Home).</summary>
    public static string SessionLongName(SessionSummaryDto session) => LongName(ODChampionshipMapper.SessionName(session));

    static string LongName(string name) =>
        name != null && name.StartsWith("EL") && name.Length > 2 ? "Essais libres " + name.Substring(2) : name;

    // ═══════════════════════════════════════════════════════════════════════
    // What just happened (weekend)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>"Qualifications terminées · pole pour Márquez", "Course terminée", "Essais libres 2 terminés · meilleur temps pour Martín".</summary>
    public static string Headline(string sessionName, string winner)
    {
        if (string.IsNullOrEmpty(sessionName)) return null;
        string name = LongName(sessionName);
        string done = name.StartsWith("Qualif") ? "terminées"
                    : name.StartsWith("Essais") ? "terminés"
                    : name == "Sprint" ? "terminé" : "terminée";
        if (string.IsNullOrEmpty(winner)) return name + " " + done;
        string result = name.StartsWith("Qualif") ? "pole pour " + winner
                      : name.StartsWith("Essais") ? "meilleur temps pour " + winner
                      : name == "Course" || name == "Sprint" ? "victoire " + Of(winner)
                      : winner + " en tête";
        return name + " " + done + " · " + result;
    }

    /// <summary>Short result for a replay tile: "Pole : Márquez", "Victoire : Bagnaia"; null without a winner.</summary>
    public static string ResultLabel(string sessionName, string winner)
    {
        if (string.IsNullOrEmpty(winner)) return null;
        string name = LongName(sessionName) ?? "";
        if (name.StartsWith("Qualif")) return "Pole : " + winner;
        if (name == "Course" || name == "Sprint") return "Victoire : " + winner;
        return "Meilleur temps : " + winner;
    }

    static string Of(string name) => "AEIOUYÉÈÀÂ".IndexOf(char.ToUpperInvariant(name[0])) >= 0 ? "d’" + name : "de " + name;

    // ═══════════════════════════════════════════════════════════════════════
    // Rest of the week
    // ═══════════════════════════════════════════════════════════════════════

    public static HomeAgendaItem ToAgendaItem(ChampionshipSummaryDto championship, string championshipCode, EventSummaryDto evt) => new HomeAgendaItem
    {
        championshipId = championshipCode,
        championshipName = ODChampionshipMapper.ChampionshipName(championship, championshipCode),
        title = evt.name,
        location = Place(evt),
        startsAt = evt.startDate.ToLocalTime(),
        endsAt = evt.endDate.ToLocalTime(),
        replayIndex = -1,
    };

    /// <summary>
    /// Events still to come or running, soonest first; the replay of the featured event (if any)
    /// goes right after the first running event, as in the design ("WRC today, replay, F1 next week").
    /// </summary>
    public static List<HomeAgendaItem> Agenda(IEnumerable<HomeAgendaItem> events, HomeAgendaItem? replay, DateTime nowLocal)
    {
        var list = (events ?? Enumerable.Empty<HomeAgendaItem>()).Where(e => e.endsAt >= nowLocal).OrderBy(e => e.startsAt).ToList();
        if (replay.HasValue) list.Insert(Math.Min(1, list.Count(e => e.startsAt <= nowLocal)), replay.Value);
        return list.Take(MaxAgenda).ToList();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Replays and leaders
    // ═══════════════════════════════════════════════════════════════════════

    public static HomeReplay ToReplay(ChampionshipSummaryDto championship, string championshipCode,
        EventSummaryDto evt, SessionSummaryDto session) => new HomeReplay
        {
            summary = ODChampionshipMapper.ToReplaySummary(championship, championshipCode, evt, session),
            durationMinutes = session.endTime.HasValue && session.endTime.Value > session.startTime
                ? (int)Math.Round((session.endTime.Value - session.startTime).TotalMinutes) : 0,
        };

    /// <summary>Drivers' leader of the standings (teams when the championship only ranks teams); null when empty.</summary>
    public static HomeLeader? ToLeader(string championshipId, SeasonStandingCategory standings)
    {
        if (standings == null) return null;
        var table = standings.drivers.Count > 0 ? standings.drivers : standings.teams;
        var ranked = table.OrderBy(e => e.position).ToList();
        if (ranked.Count == 0) return null;
        return new HomeLeader
        {
            championshipId = championshipId,
            name = ranked[0].name,
            points = ranked[0].points,
            lead = ranked.Count > 1 ? ranked[0].points - ranked[1].points : -1f,
        };
    }

    // ── Demo data (existing mobile mocks, picked from the backend error popup) ──

    public static HomeNextEvent ToNextEvent(OffSeasonData offSeason)
    {
        if (offSeason?.nextEvent == null) return null;
        var evt = offSeason.nextEvent.Value;
        return new HomeNextEvent
        {
            championshipId = offSeason.championshipId,
            championshipName = offSeason.headline,
            eventName = evt.name,
            location = evt.location,
            place = evt.location,
            startsAt = evt.startsAt,
        };
    }

    public static HomeNextEvent ToWeekendEvent(WeekendData weekend, string championshipName)
    {
        if (weekend == null) return null;
        var sessions = weekend.sessions.OrderBy(s => s.startsAt).ToList();
        var data = new HomeNextEvent
        {
            championshipId = weekend.championshipId,
            championshipName = championshipName,
            eventName = weekend.eventName,
            location = weekend.circuit?.name,
            place = weekend.circuit?.name,
            startsAt = sessions.Count > 0 ? sessions[0].startsAt : DateTime.Now,
        };
        AddKeySessions(data, sessions.Select(s => new Candidate
        {
            name = LongName(s.name),
            startsAt = s.startsAt,
            race = s.name == "Course" || s.name == "Sprint",
            finished = s.status == WeekendSessionStatus.Completed,
        }), true);
        var last = sessions.LastOrDefault(s => s.status == WeekendSessionStatus.Completed);
        data.headline = Headline(last.name, null);
        return data;
    }

    public static HomeNextEvent ToLiveEvent(LiveSessionData live, string championshipName, string eventName) => live == null ? null : new HomeNextEvent
    {
        championshipId = live.championshipId,
        championshipName = championshipName,
        eventName = eventName,
        location = live.circuitName,
        place = live.circuitName,
        startsAt = DateTime.Now,
    };

    public static HomeAgendaItem ToAgendaItem(OffSeasonData offSeason, OffSeasonEvent evt) => new HomeAgendaItem
    {
        championshipId = offSeason.championshipId,
        championshipName = offSeason.headline,
        title = evt.name,
        location = evt.location,
        startsAt = evt.startsAt,
        endsAt = evt.startsAt.AddDays(2),
        replayIndex = -1,
    };

    public static HomeReplay ToReplay(ReplaySummary summary) => new HomeReplay { summary = summary };
}
