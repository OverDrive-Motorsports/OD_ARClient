/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## HomeData - Data shown by the Home page (Canva 2.1): the featured event
 ## (a running session, an event weekend or the next event) with its key
 ## sessions, the other events of the week, the latest replays and each
 ## championship's leader. Fetched when the Home opens, then kept and
 ## refilled in place. Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using System.Collections.Generic;

/// <summary>Which Home the user gets: 2.1c, 2.1b or 2.1a.</summary>
public enum HomeState
{
    NoRace,    // "Aucune course aujourd'hui": next event + countdown
    Weekend,   // "Live · Week-end": event in progress, between two sessions
    Live       // "En direct": a session is running
}

[Serializable]
public struct HomeSession
{
    public string name;       // "Essais libres 1", "Sprint", "Course"
    public DateTime startsAt; // local time
    public bool finished;
}

// Not Unity-serialized: holds a list of structs refilled in place.
public class HomeNextEvent
{
    public string championshipId;    // "f1", "motogp"
    public string championshipName;  // "Formule 1"
    public string eventName;         // "Grand Prix de Malaisie"
    public string location;          // "Sepang International Circuit · Malaysia"
    public string place;             // "Phillip Island" — short, for the caps line
    public int round;                // 19 — 0 when unknown
    public int roundCount;           // 22 — 0 when unknown
    public DateTime startsAt;        // local time
    /// <summary>Weekend only: what just happened ("Qualifications terminées · pole pour Márquez"), null when nothing yet.</summary>
    public string headline;
    /// <summary>Key sessions in chronological order (at most three): during a weekend, the last finished one comes first.</summary>
    public List<HomeSession> sessions = new();
}

[Serializable]
public struct HomeReplay
{
    public ReplaySummary summary;
    /// <summary>Session length in minutes, 0 when the backend has no end time.</summary>
    public int durationMinutes;
}

[Serializable]
public struct HomeLeader
{
    public string championshipId;  // "f1"
    public string name;            // "Verstappen"
    /// <summary>Lead over P2 in points, -1 when there is no P2.</summary>
    public float lead;
    public float points;
}

/// <summary>Tile of the "Cette semaine" / "Aussi ce week-end" row: another event, or a replay of the featured one.</summary>
[Serializable]
public struct HomeAgendaItem
{
    public string championshipId;
    public string championshipName;
    public string title;          // "Rallye d'Espagne"
    public string location;       // "Valence"
    public DateTime startsAt;     // local time
    public DateTime endsAt;       // local time
    /// <summary>Index in HomeData.replays when the tile is a replay, -1 for an event.</summary>
    public int replayIndex;
    /// <summary>Replay tiles: what happened ("Pole : Márquez"), null to fall back on the session and date.</summary>
    public string detail;
}

public class HomeData
{
    public HomeState state;
    /// <summary>Featured event: the running one (Live, Weekend) or the earliest upcoming one; null when none is scheduled.</summary>
    public HomeNextEvent nextEvent;
    /// <summary>Live only: running order of the session (the Home shows the top 3).</summary>
    public LiveSessionData live;
    /// <summary>Live and Weekend: other events of the week, and a replay of the featured event.</summary>
    public List<HomeAgendaItem> agenda = new();
    /// <summary>Most recent first.</summary>
    public List<HomeReplay> replays = new();
    public List<HomeLeader> leaders = new();

    // Kept so the live order can be refreshed without re-resolving every championship.
    public string liveChampionshipCode;
    public EventSummaryDto liveEvent;
    public SessionSummaryDto liveSession;
}
