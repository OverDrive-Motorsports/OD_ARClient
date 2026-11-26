/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTelemetryFeed - Backend telemetry of the active session for the widgets
 ## placed in the space (ODWidgetSpace). Every widget follows Verstappen
 ## (DriverCode); the ones that compare two cars put Hamilton beside him
 ## (RivalCode). Each widget reads the part of the frame it shows.
 ## Two sources, chosen by the race page (ODSportPage):
 ##   • replay — the downloaded replay (RaceReplay), read at the replay
 ##     clock (ODReplayPlayer.RaceTime): the widgets move with the playback,
 ##     and follow it back when it is rewound or moved, since every frame is
 ##     computed from the time alone;
 ##   • live — the gateway routes of the live session (/race/laps, then
 ##     /telemetry/speed and /telemetry/engine of the running lap, the previous
 ##     one and the best one of each car), polled every livePollSeconds, played
 ##     LiveDelay behind the newest sample so it runs smoothly between polls.
 ## Publishes a TelemetryFrame 10 times a second (Frame, Version) and a status
 ## (Status, Message): loading, ready, no data (the session has none for the
 ## car, or not at this time), connection lost. Outside Ready the widgets hide
 ## their values: no stale or wrong number stays on screen.
 ##
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public sealed class ODTelemetryFeed : MonoBehaviour
{
    /// <summary>The followed car of every widget, and the one compared to it.</summary>
    public const string DriverCode = "VER", RivalCode = "HAM";
    /// <summary>Their numbers when the session's driver list doesn't give the codes.</summary>
    const int DriverFallback = 1, RivalFallback = 44;
    const float PublishSeconds = .1f;
    /// <summary>Seconds the live playback stays behind the newest sample, on top of the polling period.</summary>
    const float LiveMargin = 2f;
    /// <summary>Failed polls in a row before the values are hidden behind "connection lost".</summary>
    const int FailuresBeforeLost = 2;

    public TelemetryStatus Status { get; private set; } = TelemetryStatus.Loading;
    /// <summary>What the widgets say while not Ready.</summary>
    public string Message { get; private set; } = "Loading telemetry…";
    /// <summary>The frame, updated in place at each publish while Ready.</summary>
    public TelemetryFrame Frame { get; } = new TelemetryFrame();
    /// <summary>Goes up at every published frame.</summary>
    public int Version { get; private set; }

    TelemetryTimeline _timeline;
    Func<float> _clock;
    RaceReplay _replay;
    string _liveSession;
    int _generation;
    float _nextPublish, _liveTime;

    static float LiveDelay => Mathf.Max(1f, ODApiConfig.Instance.livePollSeconds) + LiveMargin;

    void Awake()
    {
        Frame.timing.driverCode = DriverCode;
        Frame.rival.driverCode = RivalCode;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Sources
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>The replay, at the time <paramref name="clock"/> gives (the same replay again changes nothing).</summary>
    public void PlayReplay(RaceReplay replay, Func<float> clock)
    {
        if (replay == null) { Wait("Loading the replay…"); return; }
        if (_replay == replay && _clock != null) return;
        Reset();
        _replay = replay;
        _timeline = ODTelemetryMapper.FromReplay(replay, DriverCode, DriverFallback, RivalCode, RivalFallback);
        if (_timeline.driver == null)
        {
            Unavailable($"No telemetry for {DriverCode} in this replay");
            return;
        }
        _clock = clock;
        Publish();
    }

    /// <summary>The live session <paramref name="sessionId"/>, through the gateway (the same session again changes nothing).</summary>
    public void FollowLive(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) { Wait(); return; }
        if (_liveSession == sessionId) return;
        Reset();
        _liveSession = sessionId;
        SetStatus(TelemetryStatus.Loading, "Loading live telemetry…");
        if (isActiveAndEnabled) RunLive(sessionId, _generation);
    }

    /// <summary>No source yet (the replay or the session is on its way).</summary>
    public void Wait(string message = "Loading telemetry…")
    {
        Reset();
        SetStatus(TelemetryStatus.Loading, message);
    }

    /// <summary>The source can't give data (replay that failed to load, session without telemetry).</summary>
    public void Unavailable(string message)
    {
        Reset();
        SetStatus(TelemetryStatus.NoData, message);
    }

    void Reset()
    {
        _generation++;   // ends a running live loop
        _timeline = null;
        _clock = null;
        _replay = null;
        _liveSession = null;
    }

    void OnEnable()
    {
        // Hidden with the space, the live loop stopped: it starts again where the session is now.
        if (_liveSession != null) RunLive(_liveSession, ++_generation);
    }

    void OnDisable()
    {
        if (_liveSession != null) _generation++;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Publishing
    // ═══════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (_timeline == null || _clock == null) return;
        if (_liveSession != null) AdvanceLiveClock();
        if (ODTime.Now >= _nextPublish) Publish();
    }

    void Publish()
    {
        _nextPublish = ODTime.Now + PublishSeconds;
        if (ODTelemetryMapper.Fill(_timeline, _clock(), Frame))
        {
            SetStatus(TelemetryStatus.Ready, "");
            Version++;
        }
        else SetStatus(TelemetryStatus.NoData, $"No telemetry for {DriverCode} at this moment");
    }

    void SetStatus(TelemetryStatus status, string message)
    {
        Status = status;
        Message = message ?? "";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Live
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Real time, LiveDelay behind the newest sample; never ahead of it, caught up after a gap.</summary>
    void AdvanceLiveClock()
    {
        var times = _timeline.driver.time;
        float newest = times[times.Length - 1], target = newest - LiveDelay;
        _liveTime += ODTime.Delta;
        if (Mathf.Abs(_liveTime - target) > LiveDelay) _liveTime = target;
        _liveTime = Mathf.Min(_liveTime, newest - .25f);
    }

    bool Alive(int generation) => this != null && generation == _generation && isActiveAndEnabled;

    async void RunLive(string sessionId, int generation)
    {
        var session = new LiveSession(sessionId);
        int failures = 0;
        while (Alive(generation))
        {
            try
            {
                var timeline = await session.Refresh();
                if (!Alive(generation)) return;
                failures = 0;
                if (timeline.driver == null)
                {
                    _timeline = null;
                    SetStatus(TelemetryStatus.NoData, $"No telemetry for {DriverCode} in this session yet");
                }
                else
                {
                    bool first = _timeline == null;
                    _timeline = timeline;
                    _clock = () => _liveTime;
                    if (first)
                    {
                        _liveTime = timeline.driver.time[timeline.driver.time.Length - 1] - LiveDelay;
                        Publish();
                    }
                }
            }
            catch (Exception error)
            {
                if (!Alive(generation)) return;
                Debug.LogWarning("[ODTelemetryFeed] Live telemetry refresh failed: " + error.Message);
                if (++failures >= FailuresBeforeLost)
                {
                    _timeline = null;   // nothing old stays on screen; the next answer brings it back
                    SetStatus(TelemetryStatus.ConnectionLost, "Connection lost · reconnecting…");
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(1f, ODApiConfig.Instance.livePollSeconds)));
        }
    }

    /// <summary>
    /// What is fetched of a live session: the driver list (once), every lap, and the samples of the
    /// laps the widgets need for each followed car (running, previous, best). A completed lap is
    /// fetched once; the running one at every poll.
    /// </summary>
    sealed class LiveSession
    {
        readonly string _id;
        List<DriverSummaryDto> _drivers;
        int _driver, _rival;
        DateTime? _origin;
        readonly Dictionary<int, Dictionary<int, (List<TelemetrySpeedDto>, List<TelemetryEngineDto>)>> _rows = new();
        readonly HashSet<(int car, int lap)> _final = new();

        public LiveSession(string id) => _id = id;

        public async Task<TelemetryTimeline> Refresh()
        {
            if (_drivers == null)
            {
                _drivers = await ODBackendApi.GetSessionDrivers(_id) ?? new List<DriverSummaryDto>();
                _driver = ODTelemetryMapper.Number(_drivers, DriverCode, DriverFallback);
                _rival = ODTelemetryMapper.Number(_drivers, RivalCode, RivalFallback);
            }
            var laps = await ODBackendApi.GetLaps(_id) ?? new List<RaceLapsDto>();

            var fetches = new List<Task>();
            foreach (int car in new[] { _driver, _rival })
            {
                var rows = Rows(car);
                var needed = Needed(laps.FirstOrDefault(l => l?.driverNumber == car), out int completed);
                foreach (int lap in rows.Keys.Where(lap => !needed.Contains(lap)).ToList())
                {
                    rows.Remove(lap);
                    _final.Remove((car, lap));
                }
                foreach (int lap in needed)
                    if (!_final.Contains((car, lap))) fetches.Add(Fetch(car, lap, final: lap <= completed));
            }
            await Task.WhenAll(fetches);

            var timeline = new TelemetryTimeline { driverCode = DriverCode, rivalCode = RivalCode };
            if (_origin == null) return timeline;   // no sample yet
            timeline.driver = Build(_driver, DriverCode, laps);
            timeline.rival = Build(_rival, RivalCode, laps);
            ODTelemetryMapper.SetLiveSectorBests(timeline, laps);
            return timeline;
        }

        Dictionary<int, (List<TelemetrySpeedDto>, List<TelemetryEngineDto>)> Rows(int car)
        {
            if (!_rows.TryGetValue(car, out var rows)) _rows[car] = rows = new();
            return rows;
        }

        /// <summary>Running lap, previous one, best one.</summary>
        static HashSet<int> Needed(RaceLapsDto laps, out int completed)
        {
            var done = laps?.laps?.Where(l => l?.lapDuration != null).ToList() ?? new List<RaceLapEntryDto>();
            completed = done.Select(l => l.lapNumber).DefaultIfEmpty(0).Max();
            var needed = new HashSet<int> { completed + 1 };
            if (completed > 0) needed.Add(completed);
            var best = done.Where(l => l.lapDuration > 0f).OrderBy(l => l.lapDuration).FirstOrDefault();
            if (best != null) needed.Add(best.lapNumber);
            return needed;
        }

        async Task Fetch(int car, int lap, bool final)
        {
            var speed = ODBackendApi.GetTelemetrySpeed(_id, car, lap);
            var engine = ODBackendApi.GetTelemetryEngine(_id, car, lap);
            await Task.WhenAll(speed, engine);
            var speedRows = speed.Result ?? new List<TelemetrySpeedDto>();
            Rows(car)[lap] = (speedRows, engine.Result ?? new List<TelemetryEngineDto>());
            if (speedRows.Count > 0) _origin ??= speedRows.Min(r => r.timestamp).Date;   // one clock for the whole session
            if (final) _final.Add((car, lap));
        }

        DriverTelemetry Build(int car, string code, List<RaceLapsDto> laps) =>
            ODTelemetryMapper.FromLive(car, code, _drivers.FirstOrDefault(d => d?.driverNumber == car),
                laps.FirstOrDefault(l => l?.driverNumber == car), Rows(car), _origin.Value);
    }
}
