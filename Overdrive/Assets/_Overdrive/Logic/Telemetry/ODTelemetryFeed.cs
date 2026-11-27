/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTelemetryFeed - Backend telemetry of the active session for the widgets
 ## placed in the space (ODWidgetSpace). Each widget follows its own car (its
 ## WidgetTarget, resolved here against the running order: a position — P1
 ## by default — or a fixed car); the ones that compare two cars ask for a
 ## second one (P2 by default).
 ## Two sources, chosen by the race page (ODSportPage):
 ##   • replay — the downloaded replay (RaceReplay), read at the replay
 ##     clock (ODReplayPlayer.RaceTime): the widgets move with the playback,
 ##     and follow it back when it is rewound or moved, since every frame is
 ##     computed from the time alone;
 ##   • live — the gateway routes of the live session (/race/laps and
 ##     /race/position, then /telemetry/speed and /telemetry/engine of the
 ##     running lap, the previous one and the best one of each car a widget
 ##     follows), polled every livePollSeconds, played LiveDelay behind the
 ##     newest sample so it runs smoothly between polls. A car a widget starts
 ##     following is fetched at once, not at the next poll.
 ## Ten times a second it moves its clock on (Version) and the running order
 ## (Order, OrderVersion) the driver picker lists; each widget then asks for
 ## its frame (Fill). Status / Message say whether the source can give
 ## anything: loading, no data, connection lost. Fill says it per car (no
 ## sample at this time, not fetched yet). Outside Ready the widgets hide
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
    const float PublishSeconds = .1f;
    /// <summary>Seconds the live playback stays behind the newest sample, on top of the polling period.</summary>
    const float LiveMargin = 2f;
    /// <summary>Failed polls in a row before the values are hidden behind "connection lost".</summary>
    const int FailuresBeforeLost = 2;

    /// <summary>What the source can give: Ready once it has a session and its drivers.</summary>
    public TelemetryStatus Status { get; private set; } = TelemetryStatus.Loading;
    /// <summary>What the widgets say while not Ready.</summary>
    public string Message { get; private set; } = "Loading telemetry…";
    /// <summary>Goes up at every publish (the clock moved): widgets ask for their frame again.</summary>
    public int Version { get; private set; }
    /// <summary>Running order at the current time, leader first.</summary>
    public IReadOnlyList<DriverChoice> Order => _order;
    /// <summary>Goes up when the running order or a position changes.</summary>
    public int OrderVersion { get; private set; }

    Func<float> _clock;
    RaceReplay _replay;
    string _liveSession;
    int _generation, _orderKey;
    float _nextPublish, _liveTime, _time = float.NaN, _newest = float.NaN;
    bool _wake;

    /// <summary>Sector bests of the session (every car), shared by every target's timeline.</summary>
    TelemetryTimeline _bests = new();
    /// <summary>Cars built so far, by number; null when the source has no sample of the car.</summary>
    readonly Dictionary<int, DriverTelemetry> _cars = new();
    readonly Dictionary<(int car, int rival), TelemetryTimeline> _pairs = new();
    readonly Dictionary<int, DriverChoice> _choices = new();
    readonly List<DriverChoice> _order = new();
    /// <summary>Live: the cars the widgets asked for since the last poll (the next poll fetches them).</summary>
    readonly HashSet<int> _requested = new();

    static float PollSeconds => Mathf.Max(1f, ODApiConfig.Instance.livePollSeconds);
    static float LiveDelay => PollSeconds + LiveMargin;

    // ═══════════════════════════════════════════════════════════════════════
    // Sources
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>The replay, at the time <paramref name="clock"/> gives (the same replay again changes nothing).</summary>
    public void PlayReplay(RaceReplay replay, Func<float> clock)
    {
        if (replay == null) { Wait("Loading the replay…"); return; }
        if (_replay == replay && _clock != null) return;
        if (replay.cars.Count == 0) { Unavailable("No telemetry in this replay"); return; }
        Reset();
        _replay = replay;
        _clock = clock;
        SetDrivers(replay.drivers.Values);
        foreach (int number in replay.cars.Keys) ChoiceOf(number);
        ODTelemetryMapper.SetReplaySectorBests(_bests, replay);
        SetStatus(TelemetryStatus.Ready, "");
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
        _clock = null;
        _replay = null;
        _liveSession = null;
        _time = _newest = float.NaN;
        _bests = new TelemetryTimeline();
        _cars.Clear();
        _pairs.Clear();
        _choices.Clear();
        _order.Clear();
        _requested.Clear();
        _orderKey = 0;
        OrderVersion++;
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
    // Targets
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Car number <paramref name="target"/> follows now (0 while nobody runs at that position).</summary>
    public int Resolve(WidgetTarget target)
    {
        if (target.kind == WidgetTargetKind.Driver) return target.value;
        int index = target.value - 1;
        return index >= 0 && index < _order.Count ? _order[index].number : 0;
    }

    /// <summary>The first car of the running order that isn't <paramref name="number"/> (a compared car that would be the followed one).</summary>
    public int Other(int number)
    {
        foreach (var choice in _order)
            if (choice.number != number) return choice.number;
        return 0;
    }

    /// <summary>Code, name and team colour of car <paramref name="number"/>; null when the session doesn't know it.</summary>
    public DriverChoice Choice(int number) => _choices.TryGetValue(number, out var choice) ? choice : null;

    /// <summary>
    /// Writes into <paramref name="frame"/> what car <paramref name="number"/> shows now, compared to car
    /// <paramref name="rivalNumber"/> (0: none). Ready when it did; else the status and <paramref name="message"/> say why not.
    /// </summary>
    public TelemetryStatus Fill(int number, int rivalNumber, TelemetryFrame frame, out string message)
    {
        if (Status != TelemetryStatus.Ready) { message = Message; return Status; }
        if (number <= 0) { message = "Waiting for the running order…"; return TelemetryStatus.Loading; }

        string code = Choice(number)?.code ?? "#" + number;
        bool compare = rivalNumber > 0 && rivalNumber != number;
        Request(number);
        if (compare) Request(rivalNumber);

        if (!TryCar(number, out var driver)) { message = $"Loading telemetry for {code}…"; return TelemetryStatus.Loading; }
        if (driver == null) { message = $"No telemetry for {code} in this session"; return TelemetryStatus.NoData; }
        if (float.IsNaN(_time)) { message = "Loading telemetry…"; return TelemetryStatus.Loading; }

        DriverTelemetry rival = null;
        if (compare) TryCar(rivalNumber, out rival);
        var key = (number, rival != null ? rivalNumber : 0);
        if (!_pairs.TryGetValue(key, out var timeline)) _pairs[key] = timeline = ODTelemetryMapper.Pair(_bests, driver, rival);

        if (ODTelemetryMapper.Fill(timeline, _time, frame)) { message = ""; return TelemetryStatus.Ready; }
        message = $"No telemetry for {code} at this moment";
        return TelemetryStatus.NoData;
    }

    /// <summary>The car, built from the replay the first time; live, only once a poll fetched it.</summary>
    bool TryCar(int number, out DriverTelemetry car)
    {
        if (_cars.TryGetValue(number, out car)) return true;
        if (_replay == null) return false;
        _cars[number] = car = ODTelemetryMapper.FromReplayCar(_replay, number, Choice(number)?.code);
        return true;
    }

    void Request(int number)
    {
        if (_liveSession == null || number <= 0) return;
        if (_requested.Add(number) && !_cars.ContainsKey(number)) _wake = true;   // a new car: poll now
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Publishing
    // ═══════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (Status != TelemetryStatus.Ready) return;
        if (_liveSession != null && !float.IsNaN(_newest)) AdvanceLiveClock();
        if (ODTime.Now >= _nextPublish) Publish();
    }

    void Publish()
    {
        _nextPublish = ODTime.Now + PublishSeconds;
        _time = _clock != null ? _clock() : float.NaN;
        if (_replay != null && !float.IsNaN(_time))
        {
            foreach (var car in _replay.cars.Values)
            {
                int position = ODReplayMapper.PositionAt(car, _time);
                ChoiceOf(car.number).position = position == int.MaxValue ? 0 : position;
            }
            SortOrder();
        }
        Version++;
    }

    void SetStatus(TelemetryStatus status, string message)
    {
        Status = status;
        Message = message ?? "";
    }

    // ── Drivers and running order ───────────────────────────────────────────

    DriverChoice ChoiceOf(int number)
    {
        if (_choices.TryGetValue(number, out var choice)) return choice;
        _choices[number] = choice = new DriverChoice { number = number, code = number.ToString() };
        _order.Add(choice);
        return choice;
    }

    void SetDrivers(IEnumerable<DriverSummaryDto> drivers)
    {
        foreach (var driver in drivers ?? Enumerable.Empty<DriverSummaryDto>())
        {
            if (driver == null) continue;
            var choice = ChoiceOf(driver.driverNumber);
            if (!string.IsNullOrEmpty(driver.code)) choice.code = driver.code;
            string name = !string.IsNullOrEmpty(driver.fullName) ? driver.fullName : $"{driver.firstName} {driver.lastName}".Trim();
            choice.fullName = Titled(name);
            choice.teamColor = ODTelemetryMapper.Colour(driver.teamColor);
        }
    }

    /// <summary>"Oscar PIASTRI" (as the feeds write it) → "Oscar Piastri": words in capitals only are title-cased.</summary>
    static string Titled(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        var words = name.Split(' ');
        for (int i = 0; i < words.Length; i++)
            if (words[i].Length > 1 && words[i] == words[i].ToUpperInvariant())
                words[i] = words[i][0] + words[i].Substring(1).ToLowerInvariant();
        return string.Join(" ", words);
    }

    /// <summary>Leader first, cars without a position last; OrderVersion moves when anything changed.</summary>
    void SortOrder()
    {
        _order.Sort((a, b) =>
        {
            int pa = a.position > 0 ? a.position : int.MaxValue, pb = b.position > 0 ? b.position : int.MaxValue;
            return pa != pb ? pa.CompareTo(pb) : a.number.CompareTo(b.number);
        });
        int key = 17;
        foreach (var choice in _order) key = key * 31 + choice.number * 97 + choice.position;
        if (key == _orderKey) return;
        _orderKey = key;
        OrderVersion++;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Live
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Real time, LiveDelay behind the newest sample; never ahead of it, caught up after a gap.</summary>
    void AdvanceLiveClock()
    {
        float target = _newest - LiveDelay;
        _liveTime += ODTime.Delta;
        if (Mathf.Abs(_liveTime - target) > LiveDelay) _liveTime = target;
        _liveTime = Mathf.Min(_liveTime, _newest - .25f);
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
                // The cars a widget asked for since the last poll; one nobody follows any more is dropped.
                var wanted = new HashSet<int>(_requested);
                _requested.Clear();
                _wake = false;
                var result = await session.Refresh(wanted);
                if (!Alive(generation)) return;
                failures = 0;
                ApplyLive(result);
            }
            catch (Exception error)
            {
                if (!Alive(generation)) return;
                Debug.LogWarning("[ODTelemetryFeed] Live telemetry refresh failed: " + error.Message);
                if (++failures >= FailuresBeforeLost)
                {
                    // Nothing old stays on screen; the next answer brings it back.
                    _cars.Clear();
                    _pairs.Clear();
                    _newest = float.NaN;
                    SetStatus(TelemetryStatus.ConnectionLost, "Connection lost · reconnecting…");
                }
            }

            // Until the next poll, or right away when a widget starts following a car not fetched yet.
            float next = ODTime.Now + PollSeconds;
            while (Alive(generation) && ODTime.Now < next && !_wake)
                await Task.Delay(100);
        }
    }

    void ApplyLive(LiveResult result)
    {
        SetDrivers(result.drivers);
        foreach (var choice in _order) choice.position = 0;
        foreach (var row in result.positions) ChoiceOf(row.driverNumber).position = row.position;
        SortOrder();

        _cars.Clear();
        _pairs.Clear();
        float newest = float.NaN;
        foreach (var pair in result.cars)
        {
            _cars[pair.Key] = pair.Value;
            var times = pair.Value?.time;
            if (times != null && times.Length > 0) newest = float.IsNaN(newest) ? times[^1] : Mathf.Max(newest, times[^1]);
        }
        ODTelemetryMapper.SetLiveSectorBests(_bests, result.laps);

        if (!float.IsNaN(newest))
        {
            if (float.IsNaN(_newest)) _liveTime = newest - LiveDelay;   // first samples: start LiveDelay behind
            _newest = newest;
            _clock = () => _liveTime;
        }
        if (Status != TelemetryStatus.Ready)
        {
            SetStatus(TelemetryStatus.Ready, "");
            Publish();
        }
    }

    /// <summary>One poll of a live session: drivers, laps, latest positions, and the wanted cars (null: no sample).</summary>
    sealed class LiveResult
    {
        public List<DriverSummaryDto> drivers;
        public List<RaceLapsDto> laps;
        public List<RacePositionDto> positions;
        public readonly Dictionary<int, DriverTelemetry> cars = new();
    }

    /// <summary>
    /// What is fetched of a live session: the driver list (once), every lap and the positions, and the
    /// samples of the laps the widgets need for each followed car (running, previous, best). A completed
    /// lap is fetched once; the running one at every poll.
    /// </summary>
    sealed class LiveSession
    {
        readonly string _id;
        List<DriverSummaryDto> _drivers;
        DateTime? _origin;
        readonly Dictionary<int, Dictionary<int, (List<TelemetrySpeedDto>, List<TelemetryEngineDto>)>> _rows = new();
        readonly HashSet<(int car, int lap)> _final = new();

        public LiveSession(string id) => _id = id;

        public async Task<LiveResult> Refresh(ICollection<int> wanted)
        {
            _drivers ??= await ODBackendApi.GetSessionDrivers(_id) ?? new List<DriverSummaryDto>();
            var lapsTask = ODBackendApi.GetLaps(_id);
            var positionsTask = Positions();
            await Task.WhenAll(lapsTask, positionsTask);
            var laps = lapsTask.Result ?? new List<RaceLapsDto>();

            foreach (int car in _rows.Keys.Where(car => !wanted.Contains(car)).ToList())
            {
                _rows.Remove(car);
                _final.RemoveWhere(f => f.car == car);
            }

            var fetches = new List<Task>();
            foreach (int car in wanted)
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

            var result = new LiveResult { drivers = _drivers, laps = laps, positions = positionsTask.Result };
            foreach (int car in wanted)
                result.cars[car] = _origin == null ? null : Build(car, laps);   // no sample yet: none for anyone
            return result;
        }

        /// <summary>Latest position of each car; none when the route has nothing (the order then follows the list).</summary>
        async Task<List<RacePositionDto>> Positions()
        {
            try
            {
                var rows = await ODBackendApi.GetPositions(_id) ?? new List<RacePositionDto>();
                return rows.Where(r => r != null && r.position > 0)
                    .GroupBy(r => r.driverNumber)
                    .Select(g => g.OrderBy(r => r.timestamp).Last())
                    .ToList();
            }
            catch (Exception error)
            {
                Debug.LogWarning("[ODTelemetryFeed] Live positions unavailable: " + error.Message);
                return new List<RacePositionDto>();
            }
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

        DriverTelemetry Build(int car, List<RaceLapsDto> laps)
        {
            var info = _drivers.FirstOrDefault(d => d?.driverNumber == car);
            string code = !string.IsNullOrEmpty(info?.code) ? info.code : car.ToString();
            return ODTelemetryMapper.FromLive(car, code, info, laps.FirstOrDefault(l => l?.driverNumber == car), Rows(car), _origin.Value);
        }
    }
}
