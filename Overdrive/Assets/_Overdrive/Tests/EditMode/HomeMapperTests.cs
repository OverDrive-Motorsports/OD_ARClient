/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## HomeMapperTests - Covers ODHomeMapper: key sessions of the next event
 ## and of a weekend, the weekend headline, the week row, replay duration and
 ## championship leader.
 ##
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;

public class HomeMapperTests
{
    static readonly DateTime Now = new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);

    static SessionSummaryDto Session(string type, string name, int day, string status = "scheduled", int hour = 10) => new SessionSummaryDto
    {
        sessionId = name,
        type = type,
        name = name,
        status = status,
        startTime = new DateTime(2026, 10, day, hour, 0, 0, DateTimeKind.Utc),
    };

    static EventSummaryDto Event() => new EventSummaryDto
    {
        eventId = "openf1:event:1",
        name = "Grand Prix de Malaisie",
        circuit = "Sepang",
        startDate = new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc),
    };

    [Test]
    public void ToNextEvent_KeepsNextSessionThenRaces()
    {
        var sessions = new List<SessionSummaryDto>
        {
            Session("practice", "Practice 1", 30),
            Session("practice", "Practice 2", 30),
            Session("sprint", "Sprint", 31, hour: 8),
            Session("quali", "Qualifying", 31, hour: 12),
            Session("race", "Race", 31, hour: 14),
        };

        var next = ODHomeMapper.ToNextEvent(null, "motogp", Event(), sessions, Now);

        Assert.AreEqual(3, next.sessions.Count);
        Assert.AreEqual("Essais libres 1", next.sessions[0].name);
        Assert.AreEqual("Sprint", next.sessions[1].name);
        Assert.AreEqual("Course", next.sessions[2].name);
    }

    [Test]
    public void ToNextEvent_SkipsFinishedSessions()
    {
        var sessions = new List<SessionSummaryDto>
        {
            Session("practice", "Practice 1", 26, "finished"),
            Session("race", "Race", 31),
        };

        var next = ODHomeMapper.ToNextEvent(null, "f1", Event(), sessions, Now);

        Assert.AreEqual(1, next.sessions.Count);
        Assert.AreEqual("Course", next.sessions[0].name);
    }

    [Test]
    public void ToReplay_ComputesDuration_OnlyWithEndTime()
    {
        var race = Session("race", "Race", 25, "finished");
        Assert.AreEqual(0, ODHomeMapper.ToReplay(null, "f1", Event(), race).durationMinutes);

        race.endTime = race.startTime.AddMinutes(98);
        Assert.AreEqual(98, ODHomeMapper.ToReplay(null, "f1", Event(), race).durationMinutes);
    }

    [Test]
    public void ToLeader_ReturnsP1AndLeadOverP2()
    {
        var standings = new SeasonStandingCategory();
        standings.drivers.Add(new SeasonStandingEntry { position = 2, name = "Norris", points = 338 });
        standings.drivers.Add(new SeasonStandingEntry { position = 1, name = "Verstappen", points = 356 });

        HomeLeader? leader = ODHomeMapper.ToLeader("f1", standings);

        Assert.IsTrue(leader.HasValue);
        Assert.AreEqual("Verstappen", leader.Value.name);
        Assert.AreEqual(18f, leader.Value.lead);
    }

    [Test]
    public void ToLeader_ReturnsNull_WhenStandingsEmpty()
    {
        Assert.IsNull(ODHomeMapper.ToLeader("f1", new SeasonStandingCategory()));
        Assert.IsNull(ODHomeMapper.ToLeader("f1", null));
    }

    [Test]
    public void ToWeekendEvent_KeepsLastFinishedThenNextSessions()
    {
        var sessions = new List<SessionSummaryDto>
        {
            Session("practice", "Practice 1", 26, "finished"),
            Session("quali", "Qualifying", 26, "finished", hour: 14),
            Session("sprint", "Sprint", 28, hour: 8),
            Session("practice", "Warm Up", 28, hour: 9),
            Session("race", "Race", 28, hour: 14),
        };

        var weekend = ODHomeMapper.ToWeekendEvent(null, "motogp", Event(), sessions, Now);

        Assert.AreEqual(3, weekend.sessions.Count);
        Assert.AreEqual("Qualifications", weekend.sessions[0].name);
        Assert.IsTrue(weekend.sessions[0].finished);
        Assert.AreEqual("Sprint", weekend.sessions[1].name);
        Assert.IsFalse(weekend.sessions[1].finished);
        Assert.AreEqual("Course", weekend.sessions[2].name);
    }

    [Test]
    public void Headline_AgreesWithTheSession()
    {
        Assert.AreEqual("Qualifications terminées · pole pour Márquez", ODHomeMapper.Headline("Qualifications", "Márquez"));
        Assert.AreEqual("Course terminée · victoire d’Alonso", ODHomeMapper.Headline("Course", "Alonso"));
        Assert.AreEqual("Sprint terminé", ODHomeMapper.Headline("Sprint", null));
        Assert.AreEqual("Essais libres 2 terminés · meilleur temps pour Martín", ODHomeMapper.Headline("EL2", "Martín"));
        Assert.IsNull(ODHomeMapper.Headline(null, "Márquez"));
    }

    [Test]
    public void Agenda_PutsReplayAfterRunningEvent_AndDropsFinishedOnes()
    {
        var now = new DateTime(2026, 10, 17, 12, 0, 0);
        HomeAgendaItem Item(string title, int startDay, int endDay) => new HomeAgendaItem
        {
            title = title,
            startsAt = new DateTime(2026, 10, startDay),
            endsAt = new DateTime(2026, 10, endDay, 23, 0, 0),
            replayIndex = -1,
        };
        var events = new[] { Item("F1", 23, 25), Item("Past", 10, 12), Item("WRC", 16, 18), Item("WEC", 30, 31) };
        var replay = new HomeAgendaItem { title = "Qualifications", replayIndex = 0 };

        var agenda = ODHomeMapper.Agenda(events, replay, now);

        Assert.AreEqual(3, agenda.Count);
        Assert.AreEqual("WRC", agenda[0].title);
        Assert.AreEqual("Qualifications", agenda[1].title);
        Assert.AreEqual("F1", agenda[2].title);
    }

    [Test]
    public void SetRound_CountsTheSeason()
    {
        var events = new List<EventSummaryDto>
        {
            new EventSummaryDto { eventId = "a", seasonYear = 2025, startDate = new DateTime(2025, 3, 1) },
            new EventSummaryDto { eventId = "b", seasonYear = 2026, startDate = new DateTime(2026, 3, 1) },
            new EventSummaryDto { eventId = "c", seasonYear = 2026, startDate = new DateTime(2026, 4, 1) },
        };
        var data = new HomeNextEvent();

        ODHomeMapper.SetRound(data, events, events[2]);

        Assert.AreEqual(2, data.round);
        Assert.AreEqual(2, data.roundCount);
    }
}
