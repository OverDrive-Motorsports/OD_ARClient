/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ReplayTests - Replay catalogue JSON and mapping, session names, and the
 ## headset storage of downloaded replays (ODReplayStore) in a temp folder.
 ##
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

public class ReplayTests
{
    const string CatalogJson = @"[
        {""sessionId"":""openf1:session:9472"",""name"":""Race"",""type"":""race"",""startTime"":""2024-03-02T15:00:00Z"",
         ""seasonYear"":2024,""eventName"":""Bahrain Grand Prix"",""circuit"":""Bahrain International Circuit""}
    ]";

    static ReplayCatalogEntryDto Entry(string type = "race", string name = "Race") => new ReplayCatalogEntryDto
    {
        sessionId = "openf1:session:9472",
        name = name,
        type = type,
        startTime = new DateTime(2024, 3, 2, 15, 0, 0, DateTimeKind.Utc),
        seasonYear = 2024,
        eventName = "Bahrain Grand Prix",
        circuit = "Bahrain International Circuit",
    };

    // ── Catalogue ───────────────────────────────────────────────────────────

    [Test]
    public void CatalogJson_Deserializes()
    {
        var entries = JsonConvert.DeserializeObject<List<ReplayCatalogEntryDto>>(CatalogJson,
            new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc });

        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual("openf1:session:9472", entries[0].sessionId);
        Assert.AreEqual(2024, entries[0].seasonYear);
        Assert.AreEqual(new DateTime(2024, 3, 2, 15, 0, 0, DateTimeKind.Utc), entries[0].startTime);
    }

    [Test]
    public void ToReplaySummary_MapsTheCatalogEntry()
    {
        var summary = ODChampionshipMapper.ToReplaySummary(null, "f1", Entry());

        Assert.AreEqual("openf1:session:9472", summary.id);
        Assert.AreEqual("f1", summary.championshipId);
        Assert.AreEqual("Bahrain Grand Prix", summary.grandPrix);
        Assert.AreEqual("Course", summary.sessionName);
        Assert.AreEqual("2024", summary.season);
        Assert.AreEqual("Bahrain International Circuit", summary.circuit);
        Assert.AreEqual(Entry().startTime.ToLocalTime(), summary.date);
    }

    [TestCase("race", "Race", "Course")]
    [TestCase("race", "Sprint", "Sprint")]
    [TestCase("sprint", "Sprint", "Sprint")]
    [TestCase("sprint", "Sprint Qualifying", "Qualifs sprint")]
    [TestCase("quali", "Sprint Shootout", "Qualifs sprint")]
    [TestCase("quali", "Qualifying", "Qualifications")]
    [TestCase("practice", "Practice 2", "EL2")]
    [TestCase("practice", null, "Essais libres")]
    public void SessionName_IsFrench(string type, string name, string expected)
    {
        Assert.AreEqual(expected, ODChampionshipMapper.SessionName(type, name));
    }

    // ── Storage ─────────────────────────────────────────────────────────────

    string _previousRoot;
    string _root;

    [SetUp]
    public void UseTempRoot()
    {
        _previousRoot = ODReplayStore.Root;
        _root = Path.Combine(Path.GetTempPath(), "od-replays-" + Guid.NewGuid().ToString("N"));
        ODReplayStore.Root = _root;
    }

    [TearDown]
    public void RestoreRoot()
    {
        ODReplayStore.Root = _previousRoot;
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    static DownloadedReplayDto Save(ReplaySummary summary)
    {
        File.WriteAllText(ODReplayStore.BeginDownload(summary.id), "{\"telemetry\":{}}");
        return ODReplayStore.Commit(summary,
            new List<SessionResultRowDto> { new SessionResultRowDto { driver_number = 1, position = 1 } },
            new List<DriverSummaryDto> { new DriverSummaryDto { driverNumber = 1, lastName = "VERSTAPPEN" } });
    }

    [Test]
    public void Commit_IsReadBackAfterARescan()
    {
        var summary = ODChampionshipMapper.ToReplaySummary(null, "f1", Entry());
        var saved = Save(summary);

        ODReplayStore.Root = _root;   // drops the cached index: reads meta.json again
        var replay = ODReplayStore.Get(summary.id);

        Assert.IsNotNull(replay);
        Assert.IsTrue(ODReplayStore.IsCurrent(replay));
        Assert.AreEqual(saved.sizeBytes, replay.sizeBytes);
        Assert.Greater(replay.sizeBytes, 0);
        Assert.AreEqual(summary.date, replay.summary.date);
        Assert.AreEqual(1, replay.results.Single().position);
        Assert.AreEqual("VERSTAPPEN", replay.drivers.Single().lastName);
        Assert.IsTrue(File.Exists(ODReplayStore.DataPath(summary.id)));
        Assert.IsFalse(File.Exists(ODReplayStore.DataPath(summary.id) + ".part"));
    }

    [Test]
    public void UnfinishedDownload_IsNotAReplay_AndIsCleanedUp()
    {
        string part = ODReplayStore.BeginDownload("openf1:session:1");
        File.WriteAllText(part, "{\"tele");

        ODReplayStore.Root = _root;

        Assert.IsNull(ODReplayStore.Get("openf1:session:1"));
        Assert.AreEqual(0, ODReplayStore.All.Count);
        Assert.IsFalse(File.Exists(part));
    }

    [Test]
    public void Delete_RemovesTheFiles()
    {
        var summary = ODChampionshipMapper.ToReplaySummary(null, "f1", Entry());
        Save(summary);
        int changes = 0;
        void Count() => changes++;
        ODReplayStore.Changed += Count;
        try
        {
            ODReplayStore.Delete(summary.id);
        }
        finally
        {
            ODReplayStore.Changed -= Count;
        }

        Assert.IsNull(ODReplayStore.Get(summary.id));
        Assert.IsFalse(File.Exists(ODReplayStore.DataPath(summary.id)));
        Assert.AreEqual(0, ODReplayStore.TotalBytes);
        Assert.AreEqual(1, changes);
    }

    [Test]
    public void SessionId_StaysInsideTheRoot()
    {
        string path = ODReplayStore.DataPath("../../etc:passwd");

        StringAssert.StartsWith(Path.GetFullPath(_root), Path.GetFullPath(path));
    }

    // ── Playback ────────────────────────────────────────────────────────────

    // Lights out at 07:00:00 (25200 s from midnight). Car 1: laps of 100 s and 90 s, pits on lap 1.
    // Car 2: 101 s and 95 s. Car 1 takes the lead at 07:01:00.
    const string RaceJson = @"{
        ""telemetry"": {
            ""1"": [ {""x"":0,""y"":0,""lapNumber"":1,""timestamp"":""2025-03-23T06:59:00Z""},
                     {""x"":10,""y"":0,""lapNumber"":1,""timestamp"":""2025-03-23T07:00:00Z""},
                     {""x"":20,""y"":0,""lapNumber"":1,""timestamp"":""2025-03-23T07:00:01.5Z""} ],
            ""2"": [ {""x"":5,""y"":5,""speed"":120,""timestamp"":""2025-03-23T07:00:00Z""} ] },
        ""position"": {
            ""1"": [ {""position"":1,""timestamp"":""2025-03-23T07:01:00Z""}, {""position"":2,""timestamp"":""2025-03-23T06:50:00Z""} ],
            ""2"": [ {""position"":1,""timestamp"":""2025-03-23T06:50:00Z""}, {""position"":2,""timestamp"":""2025-03-23T07:01:00Z""} ] },
        ""radio"": { ""1"": [ {""timestamp"":""2025-03-23T07:00:30Z"",""audioUrl"":""x""} ] },
        ""pitStop"": { ""1"": [ {""lapNumber"":1,""pitDuration"":22.4} ] },
        ""stint"": { ""1"": [ {""stintNumber"":1,""compound"":""SOFT"",""lapStart"":1,""lapEnd"":2,""tyreAgeAtStart"":0} ] },
        ""lap"": {
            ""1"": [ {""lapNumber"":1,""lapDuration"":100}, {""lapNumber"":2,""lapDuration"":90} ],
            ""2"": [ {""lapNumber"":1,""lapDuration"":101}, {""lapNumber"":2,""lapDuration"":95} ] },
        ""raceControl"": [
            {""category"":""SessionStatus"",""message"":""SESSION STARTED"",""timestamp"":""2025-03-23T07:00:00Z""},
            {""category"":""Flag"",""flag"":""CHEQUERED"",""message"":""CHEQUERED FLAG"",""timestamp"":""2025-03-23T07:03:11Z""} ],
        ""weather"": [ {""timestamp"":""2025-03-23T07:00:00Z"",""airTemperature"":20} ]
    }";

    const float Start = 25200f;

    static RaceReplay Race()
    {
        var race = ODRaceReplayReader.Read(new StringReader(RaceJson));
        race.drivers = ODChampionshipMapper.DriverLookup(new List<DriverSummaryDto>
        {
            new DriverSummaryDto { driverNumber = 1, code = "PIA", lastName = "PIASTRI" },
            new DriverSummaryDto { driverNumber = 2, code = "NOR", lastName = "NORRIS" },
        });
        return race;
    }

    [Test]
    public void Reader_BuildsTheClockAndTheLaps()
    {
        var race = Race();
        var car = race.cars[1];

        Assert.AreEqual(new DateTime(2025, 3, 23, 0, 0, 0, DateTimeKind.Utc), race.origin);
        Assert.AreEqual(Start, race.start, 1e-3f);
        Assert.AreEqual(Start + 191f, race.finish, 1e-3f);
        Assert.AreEqual(2, race.totalLaps);
        CollectionAssert.AreEqual(new[] { Start, Start + 1.5f }, car.time, "the (0, 0) sample is dropped");
        Assert.AreEqual(Start + 100f, car.lapStart[2], 1e-3f);
        Assert.AreEqual(Start + 190f, car.lapStart[3], 1e-3f);
        CollectionAssert.AreEqual(new[] { 2, 1 }, car.position, "position changes sorted by time");
        CollectionAssert.AreEqual(new[] { 1 }, car.pitLaps);
        Assert.AreEqual("SOFT", car.stints.Single().compound);
    }

    [Test]
    public void ParseUtc_ReadsTheBackendTimestamps()
    {
        Assert.AreEqual(new DateTime(2025, 3, 23, 7, 3, 54, 350, DateTimeKind.Utc), ODRaceReplayReader.ParseUtc("2025-03-23T07:03:54.35Z"));
        Assert.AreEqual(new DateTime(2025, 3, 23, 6, 20, 1, DateTimeKind.Utc), ODRaceReplayReader.ParseUtc("2025-03-23T06:20:01Z"));
        Assert.AreEqual(new DateTime(2025, 3, 23, 6, 20, 1, DateTimeKind.Utc), ODRaceReplayReader.ParseUtc("2025-03-23T08:20:01+02:00"));
    }

    [Test]
    public void Frame_IsTheRunningOrderAtThatTime()
    {
        var race = Race();
        var summary = new ReplaySummary { championshipId = "f1", sessionName = "Course" };

        var grid = ODReplayMapper.ToFrame(race, summary, Start - 10f);
        CollectionAssert.AreEqual(new[] { "NOR", "PIA" }, grid.entries.Select(e => e.code).ToArray());
        Assert.AreEqual(1, grid.currentLap);

        var frame = ODReplayMapper.ToFrame(race, summary, Start + 150f);
        CollectionAssert.AreEqual(new[] { "PIA", "NOR" }, frame.entries.Select(e => e.code).ToArray());
        Assert.AreEqual(2, frame.currentLap);
        Assert.AreEqual(2, frame.totalLaps);

        var leader = frame.entries[0];
        Assert.AreEqual("Leader", leader.gap);
        Assert.AreEqual("S", leader.tyreCompound);
        Assert.AreEqual(1, leader.tyreAgeLaps);
        Assert.AreEqual(1, leader.pitStops);
        Assert.AreEqual("1:40.000", leader.lastLap);
        Assert.IsTrue(leader.isFastestLap);
        // NOR is 1.516 laps in (lap 2 started at 101 s, 95 s long); PIA was there at 146.4 s.
        Assert.AreEqual("+3.6s", frame.entries[1].gap);
        Assert.IsFalse(frame.entries[1].isFastestLap);
    }

    [Test]
    public void Gap_CountsLapsDown()
    {
        // When the car starts lap 3 (t = 200), the leader has been on lap 4 since t = 180.
        var leader = new ReplayCar { lapStart = new[] { float.NaN, 0f, 60f, 120f, 180f, 240f } };
        var car = new ReplayCar { lapStart = new[] { float.NaN, 0f, 100f, 200f } };

        Assert.AreEqual("+1 tour", ODReplayMapper.Gap(car, leader, 205f));
        Assert.AreEqual("+40.0s", ODReplayMapper.Gap(car, leader, 100f), "on the line");
        Assert.AreEqual("+52.0s", ODReplayMapper.Gap(car, leader, 130f), "1.3 laps in: the leader was there at 78 s");
        Assert.AreEqual("", ODReplayMapper.Gap(car, leader, 50f), "no gap before the first timing line");
    }

    [Test]
    public void Gap_ChangesAtEverySectorLine()
    {
        // Leader: 90 s laps, sectors 30 / 30 / 30. Car: 100 s laps, sectors 20 / 40 / 40.
        var leader = new ReplayCar
        {
            lapStart = new[] { float.NaN, 0f, 90f, 180f },
            sector1 = new[] { float.NaN, 30f, 30f }, sector2 = new[] { float.NaN, 30f, 30f },
        };
        var car = new ReplayCar
        {
            lapStart = new[] { float.NaN, 0f, 100f, 200f },
            sector1 = new[] { float.NaN, 20f, 20f }, sector2 = new[] { float.NaN, 40f, 40f },
        };

        Assert.AreEqual("", ODReplayMapper.Gap(car, leader, 10f), "before the end of sector 1");
        Assert.AreEqual(1f / 3f, ODReplayMapper.Progress(car, 20f), 1e-4f);
        Assert.AreEqual(30f, ODReplayMapper.TimeAt(leader, 1f / 3f), 1e-3f);
        Assert.AreEqual("", ODReplayMapper.Gap(car, leader, 20f), "ahead of the leader at sector 1: no negative gap");
        Assert.AreEqual("+2.5s", ODReplayMapper.Gap(car, leader, 70f), "a quarter into the car's sector 3: the leader was there at 67.5 s");
        Assert.AreEqual("+10.0s", ODReplayMapper.Gap(car, leader, 100f), "on the line");
        Assert.AreEqual("+5.0s", ODReplayMapper.Gap(car, leader, 80f), "halfway through sector 3");
        Assert.AreEqual("+20.0s", ODReplayMapper.Gap(car, leader, 260f), "frozen at the car's last line");
    }

    [Test]
    public void CarPosition_IsInterpolatedBetweenCloseSamples()
    {
        var car = new ReplayCar { time = new[] { 0f, 1f, 10f }, x = new[] { 0f, 10f, 20f }, y = new[] { 0f, 0f, 0f } };
        int hint = -1;

        Assert.IsTrue(ODReplayMapper.TryGetPosition(car, .5f, ref hint, out var p));
        Assert.AreEqual(5f, p.x, 1e-4f);
        Assert.IsTrue(ODReplayMapper.TryGetPosition(car, 2f, ref hint, out p), "held at the last sample for a moment");
        Assert.AreEqual(10f, p.x, 1e-4f);
        Assert.IsFalse(ODReplayMapper.TryGetPosition(car, 5f, ref hint, out _), "hidden in a long data gap");
        Assert.IsFalse(ODReplayMapper.TryGetPosition(car, -1f, ref hint, out _));
    }

    [Test]
    public void Reader_FlagsCarsThatShareOnePosition()
    {
        Assert.IsFalse(Race().sharedPositions, "each car has its own samples");

        const string shared = @"{ ""telemetry"": {
            ""1"": [ {""x"":10,""y"":0,""timestamp"":""2025-03-23T07:00:00Z""}, {""x"":20,""y"":0,""timestamp"":""2025-03-23T07:00:01Z""} ],
            ""2"": [ {""x"":10,""y"":0,""timestamp"":""2025-03-23T07:00:00Z""}, {""x"":20,""y"":0,""timestamp"":""2025-03-23T07:00:01Z""} ] } }";
        Assert.IsTrue(ODRaceReplayReader.Read(new StringReader(shared)).sharedPositions);
    }

    [Test]
    public void SharedPositions_CarsAreSpreadAlongTheOutlineByLapProgress()
    {
        // Square lap of 400 units starting at (0, 0); both cars on the same (broken) samples.
        var race = new RaceReplay
        {
            start = 0f,
            sharedPositions = true,
            outline = new[] { new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 100), new Vector2(0, 100) },
            outlineDistance = new[] { 0f, 100f, 200f, 300f, 400f },
        };
        var pole = new ReplayCar { lapStart = new[] { float.NaN, 0f, 100f, 200f }, lapCount = 2, positionTime = new[] { -10f }, position = new[] { 1 } };
        var second = new ReplayCar { lapStart = new[] { float.NaN, 0f, 110f, 210f }, lapCount = 2, positionTime = new[] { -10f }, position = new[] { 2 } };
        int hint = -1;

        ODReplayMapper.TryGetPosition(race, pole, 0f, ref hint, out var p);
        Assert.AreEqual(Vector2.zero, p, "without speed data, the cars start on the line");

        ODReplayMapper.TryGetPosition(race, pole, 150f, ref hint, out p);
        Assert.AreEqual(new Vector2(100, 100), p, "half of lap 2");
        ODReplayMapper.TryGetPosition(race, second, 150f, ref hint, out p);
        Assert.AreEqual(100f, p.x, 1e-3f);
        Assert.AreEqual(60f, p.y, 1e-3f, "40 % into its lap 2, behind the leader");

        ODReplayMapper.TryGetPosition(race, pole, 250f, ref hint, out p);
        Assert.AreEqual(Vector2.zero, p, "laps done: waits on the line");
    }

    [Test]
    public void SharedPositions_FollowTheMetresDriven_FromTheGrid_AndTheTimingLine()
    {
        // Laps start at 0, 20 and 40 s; 90 m driven to the first line, then a 100-m lap.
        var car = new ReplayCar
        {
            lapStart = new[] { float.NaN, 0f, 20f, 40f },
            lapCount = 2,
            odometerTime = new[] { 0f, 10f, 20f, 30f, 40f },
            odometer = new[] { 0f, 50f, 90f, 140f, 190f },
            lapDistance = 100f,
        };
        Assert.AreEqual(.1f, ODReplayMapper.LapFraction(car, 0f), 1e-4f, "the grid slot is 10 m past the line");
        Assert.AreEqual(.6f, ODReplayMapper.LapFraction(car, 10f), 1e-4f, "50 m later");
        Assert.AreEqual(.5f, ODReplayMapper.LapFraction(car, 30f), 1e-4f, "lap 2: 50 of its 100 m");

        // Square lap of 400 units; the timing line is a quarter of the way round the outline.
        var race = new RaceReplay
        {
            sharedPositions = true,
            lineOffset = .25f,
            outline = new[] { new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 100), new Vector2(0, 100) },
            outlineDistance = new[] { 0f, 100f, 200f, 300f, 400f },
        };
        int hint = -1;
        ODReplayMapper.TryGetPosition(race, car, 20f, ref hint, out var p);
        Assert.AreEqual(new Vector2(100, 0), p, "on the timing line");
        ODReplayMapper.TryGetPosition(race, car, 30f, ref hint, out p);
        Assert.AreEqual(new Vector2(0, 100), p, "half a lap after the line");
    }
}
