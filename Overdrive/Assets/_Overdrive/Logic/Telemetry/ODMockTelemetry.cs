/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODMockTelemetry - Simulated telemetry and timing of the followed car
 ## until the backend feed exists. A 5.4 km lap with 8 braking zones and 3
 ## sectors (8 mini-sectors each): the car is flat out on the straights,
 ## brakes when the next corner gets within its braking distance, takes the
 ## corner on part throttle. Every lap gets its own pace (and every corner a
 ## little more or less speed), so lap, sector and mini-sector times vary and
 ## the purple / green / yellow colour code moves.
 ## Integrated every frame, published 10 times a second, like a real feed
 ## (the widgets smooth between samples), as one TelemetryFrame (Frame):
 ##   • car — CarTelemetry sample (speed, pedals, position on the lap);
 ##   • trace — the last lap along the distance (speed and pedals);
 ##   • timing — lap, sectors, mini-sectors, delta, last laps of the car;
 ##   • rival — mini-sectors of a second car, a little behind, to compare.
 ## Six laps are simulated at start-up, so traces, bests and the last laps
 ## are filled from the first frame. One shared instance (Shared), created on
 ## first use and kept across scenes.
 ##
 */

using UnityEngine;

public sealed class ODMockTelemetry : MonoBehaviour
{
    const float SampleRate = 10f;          // samples per second, as a live feed
    const float LapLength = 5400f;         // m
    const float TopSpeed = 322f;           // km/h (the long straight peaks around 315)
    const float BrakeDecel = 135f;         // km/h per second at full pedal (~3.8 g)
    const float MaxRpm = 12500f;
    const int TraceBins = 240;             // slices of track in the lap trace (22.5 m each)
    const int WarmUpLaps = 6, FirstLap = 32;
    const float RivalGap = .035f;          // share of a lap the rival is behind
    const int MiniPerSector = LapTiming.MiniSectors / LapTiming.Sectors;

    /// <summary>Upshift points in km/h: gear n is used up to GearTop[n - 1].</summary>
    static readonly float[] GearTop = { 85f, 125f, 160f, 195f, 230f, 265f, 300f, float.MaxValue };

    struct Corner
    {
        public float start, length, speed;   // m from the line, m, km/h
        public string name;
        public Corner(int turn, float start, float length, float speed)
        {
            this.start = start;
            this.length = length;
            this.speed = speed;
            name = "Turn " + turn;
        }
    }

    /// <summary>The 8 braking zones of the lap, in order.</summary>
    static readonly Corner[] Corners =
    {
        new Corner(1, 700f, 120f, 100f),
        new Corner(3, 1250f, 160f, 175f),
        new Corner(4, 1750f, 120f, 120f),
        new Corner(6, 2350f, 140f, 150f),
        new Corner(8, 2900f, 120f, 105f),
        new Corner(10, 3450f, 180f, 185f),
        new Corner(13, 4100f, 140f, 125f),
        new Corner(15, 4750f, 130f, 140f),
    };

    static readonly float[] SectorEnds = { 1900f / LapLength, 3700f / LapLength };

    /// <summary>Share of the lap where each mini-sector ends: 8 equal slices per sector.</summary>
    static readonly float[] MiniEnds = BuildMiniEnds();

    static float[] BuildMiniEnds()
    {
        var ends = new float[LapTiming.MiniSectors];
        for (int s = 0; s < LapTiming.Sectors; s++)
        {
            float from = s == 0 ? 0f : SectorEnds[s - 1], to = s < SectorEnds.Length ? SectorEnds[s] : 1f;
            for (int j = 0; j < MiniPerSector; j++)
                ends[s * MiniPerSector + j] = from + (to - from) * (j + 1) / MiniPerSector;
        }
        ends[ends.Length - 1] = 1f;
        return ends;
    }

    static ODMockTelemetry _shared;

    /// <summary>The simulated feed, created (hidden, kept across scenes) the first time it is asked for.</summary>
    public static ODMockTelemetry Shared
    {
        get
        {
            if (_shared == null)
            {
                var go = new GameObject("ODMockTelemetry") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                _shared = go.AddComponent<ODMockTelemetry>();
            }
            return _shared;
        }
    }

    /// <summary>Goes up by one at every published sample: read <see cref="Frame"/> again when it changes.</summary>
    public int Version { get; private set; }

    /// <summary>The feed; updated in place at every published sample.</summary>
    public TelemetryFrame Frame { get; } = new TelemetryFrame
    {
        trace = new LapTrace
        {
            lapLengthKm = LapLength / 1000f,
            sectorEnds = SectorEnds,
            speed = new float[TraceBins],
            throttle = new float[TraceBins],
            brake = new float[TraceBins],
        },
        timing = NewTiming("VER", "3671C6"),
        rival = NewTiming("HAM", "E8002D"),   // second car, to compare mini-sectors
    };

    LapTrace Trace => Frame.trace;
    LapTiming Timing => Frame.timing;
    LapTiming Rival => Frame.rival;

    static LapTiming NewTiming(string code, string colour)
    {
        var timing = new LapTiming { driverCode = code, teamColor = ODDesign.Hex(colour), lap = FirstLap - WarmUpLaps };
        ResetLap(timing);
        for (int s = 0; s < LapTiming.Sectors; s++) timing.lastSectorTimes[s] = -1f;
        return timing;
    }

    static void ResetLap(LapTiming timing)
    {
        for (int s = 0; s < LapTiming.Sectors; s++)
        {
            timing.sectorTimes[s] = -1f;
            timing.sectorStatus[s] = s == 0 ? SplitStatus.Running : SplitStatus.Upcoming;
        }
        for (int m = 0; m < LapTiming.MiniSectors; m++) timing.miniSectors[m] = SplitStatus.Upcoming;
    }

    // Car
    float _distance, _speed = 290f, _throttle = 1f, _brake, _sampleClock;
    string _location = "Straight";
    float _pace = 1f;
    readonly float[] _cornerFactor = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

    // Timing of the followed car
    float _lapTime, _miniStart, _sectorStart;
    int _mini, _lastBin = -1;
    readonly float[] _miniTimes = new float[LapTiming.MiniSectors];       // running lap, previous lap ahead of the car
    readonly float[] _elapsedAtBin = new float[TraceBins];                // running lap: time when each slice was entered
    readonly float[] _bestElapsedAtBin = new float[TraceBins];            // same, on the best lap
    readonly float[] _pbMini = new float[LapTiming.MiniSectors], _pbSector = new float[LapTiming.Sectors];

    // Best of the session (both cars)
    readonly float[] _sbMini = new float[LapTiming.MiniSectors], _sbSector = new float[LapTiming.Sectors];

    // Rival
    readonly float[] _rivalPbMini = new float[LapTiming.MiniSectors];
    int _rivalDone;
    bool _rivalOnPreviousLap;

    void Awake()
    {
        NewLapPace();
        // A few silent laps so the traces, the bests and the last laps are full from the start.
        const float step = 1f / 60f;
        int laps = 0;
        for (int i = 0; i < 60 * 900 && laps < WarmUpLaps; i++)
            if (Simulate(step)) laps++;
        Publish();
    }

    void Update()
    {
        Simulate(Time.deltaTime);
        _sampleClock += Time.deltaTime;
        if (_sampleClock >= 1f / SampleRate)
        {
            _sampleClock %= 1f / SampleRate;
            Publish();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Car
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Advances the car by <paramref name="dt"/>; true when it crossed the line.</summary>
    bool Simulate(float dt)
    {
        int index = CornerAhead(_distance, out bool inCorner);
        var corner = Corners[index];
        float cornerSpeed = corner.speed * _pace * _cornerFactor[index];
        float throttleTarget, brakeTarget;

        if (inCorner)
        {
            // Part throttle through the corner, picking up toward the exit.
            float t = Mathf.Clamp01((_distance - corner.start) / corner.length);
            throttleTarget = Mathf.Lerp(.3f, .65f, t);
            brakeTarget = 0f;
            _speed = Mathf.MoveTowards(_speed, cornerSpeed + t * 15f, BrakeDecel * dt);
            _location = corner.name;
        }
        else
        {
            float ahead = corner.start - _distance;
            if (ahead < 0f) ahead += LapLength;
            float v = _speed / 3.6f, vc = cornerSpeed / 3.6f, decel = BrakeDecel * .8f / 3.6f;
            float needed = (v * v - vc * vc) / (2f * decel);
            if (_speed > cornerSpeed && needed >= ahead - 2f)
            {
                // Trail braking: full pedal first, released as the corner speed gets close.
                throttleTarget = 0f;
                brakeTarget = Mathf.Lerp(.35f, 1f, Mathf.InverseLerp(cornerSpeed, cornerSpeed + 80f, _speed));
                _speed = Mathf.Max(cornerSpeed, _speed - BrakeDecel * Mathf.Max(_brake, .2f) * dt);
                _location = corner.name;
            }
            else
            {
                throttleTarget = 1f;
                brakeTarget = 0f;
                float pull = Mathf.Lerp(48f, 4f, _speed / TopSpeed) * _pace;   // strong at low speed, drag near the top
                _speed = Mathf.Min(TopSpeed * _pace, _speed + pull * _throttle * dt);
                _location = "Straight";
            }
        }

        // A real foot moves fast but not instantly.
        _throttle = Mathf.MoveTowards(_throttle, throttleTarget, dt / .12f);
        _brake = Mathf.MoveTowards(_brake, brakeTarget, dt / .08f);

        int bin = Mathf.Clamp((int)(_distance / LapLength * TraceBins), 0, TraceBins - 1);
        Trace.speed[bin] = _speed;
        Trace.throttle[bin] = _throttle;
        Trace.brake[bin] = _brake;
        if (bin != _lastBin)
        {
            _lastBin = bin;
            _elapsedAtBin[bin] = _lapTime;
        }

        _distance += _speed / 3.6f * dt;
        _lapTime += dt;

        float share = Mathf.Min(_distance / LapLength, 1f);
        while (_mini < LapTiming.MiniSectors && share >= MiniEnds[_mini]) CompleteMini();

        bool crossed = _distance >= LapLength;
        if (crossed)
        {
            float overshoot = (_distance - LapLength) / Mathf.Max(1f, _speed / 3.6f);
            CompleteLap(_lapTime - overshoot);
            _distance -= LapLength;
            _lapTime = overshoot;
        }
        UpdateRival(_distance / LapLength);
        return crossed;
    }

    void NewLapPace()
    {
        _pace = Random.Range(.993f, 1.007f);
        for (int i = 0; i < _cornerFactor.Length; i++) _cornerFactor[i] = Random.Range(.985f, 1.015f);
    }

    /// <summary>The corner the car is in, or the next one (T1 of the next lap after the last).</summary>
    static int CornerAhead(float distance, out bool inCorner)
    {
        for (int i = 0; i < Corners.Length; i++)
        {
            if (distance < Corners[i].start + Corners[i].length)
            {
                inCorner = distance >= Corners[i].start;
                return i;
            }
        }
        inCorner = false;
        return 0;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Timing
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Purple when it beats the session, green when it beats the driver's own best, else yellow (bests updated).</summary>
    static SplitStatus Judge(float time, ref float personalBest, ref float sessionBest)
    {
        bool personal = personalBest <= 0f || time < personalBest;
        if (personal) personalBest = time;
        if (sessionBest <= 0f || time < sessionBest)
        {
            sessionBest = time;
            return SplitStatus.SessionBest;
        }
        return personal ? SplitStatus.PersonalBest : SplitStatus.Slower;
    }

    void CompleteMini()
    {
        float time = _lapTime - _miniStart;
        _miniStart = _lapTime;
        _miniTimes[_mini] = time;
        Timing.miniSectors[_mini] = Judge(time, ref _pbMini[_mini], ref _sbMini[_mini]);

        if (_mini % MiniPerSector == MiniPerSector - 1)
        {
            int s = _mini / MiniPerSector;
            float sector = _lapTime - _sectorStart;
            _sectorStart = _lapTime;
            Timing.sectorTimes[s] = sector;
            Timing.sectorStatus[s] = Judge(sector, ref _pbSector[s], ref _sbSector[s]);
            if (s + 1 < LapTiming.Sectors) Timing.sectorStatus[s + 1] = SplitStatus.Running;
        }
        _mini++;
    }

    void CompleteLap(float time)
    {
        Timing.lastLaps.Add(new LapRecord { lap = Timing.lap, time = time });
        if (Timing.lastLaps.Count > 5) Timing.lastLaps.RemoveAt(0);
        if (Timing.bestLap <= 0f || time < Timing.bestLap)
        {
            Timing.bestLap = time;
            System.Array.Copy(_elapsedAtBin, _bestElapsedAtBin, TraceBins);
        }
        System.Array.Copy(Timing.sectorTimes, Timing.lastSectorTimes, LapTiming.Sectors);

        Timing.lap++;
        ResetLap(Timing);
        _mini = 0;
        _miniStart = _sectorStart = 0f;
        _lastBin = -1;
        NewLapPace();
    }

    /// <summary>The rival follows <see cref="RivalGap"/> behind; each mini-sector it finishes gets a time close to ours.</summary>
    void UpdateRival(float share)
    {
        float rivalShare = share - RivalGap;
        bool previousLap = rivalShare < 0f;
        if (_rivalOnPreviousLap && !previousLap)
        {
            Rival.lap++;
            ResetLap(Rival);
            _rivalDone = 0;
        }
        _rivalOnPreviousLap = previousLap;
        if (previousLap) rivalShare += 1f;
        Rival.lapShare = rivalShare;

        while (_rivalDone < LapTiming.MiniSectors && rivalShare >= MiniEnds[_rivalDone] && _miniTimes[_rivalDone] > 0f)
        {
            int m = _rivalDone++;
            float time = _miniTimes[m] * Random.Range(.988f, 1.014f);
            Rival.miniSectors[m] = Judge(time, ref _rivalPbMini[m], ref _sbMini[m]);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Feed
    // ═══════════════════════════════════════════════════════════════════════

    void Publish()
    {
        int gear = 1;
        while (gear < GearTop.Length && _speed > GearTop[gear - 1]) gear++;
        float low = gear > 1 ? GearTop[gear - 2] : 0f;
        float high = gear < GearTop.Length ? GearTop[gear - 1] : TopSpeed;
        float rpm = Mathf.Lerp(9200f, 12100f, Mathf.InverseLerp(low, high, _speed));
        float share = _distance / LapLength;

        Frame.car = new CarTelemetry
        {
            speedKmh = _speed,
            gear = gear,
            rpm = rpm,
            maxRpm = MaxRpm,
            throttle = _throttle,
            brake = _brake,
            lapDistance = share,
            location = _location,
        };

        Timing.lapTime = _lapTime;
        Timing.lapShare = share;
        Timing.delta = Delta(share);
        Version++;
    }

    /// <summary>Time now minus the best lap's time at the same distance (interpolated inside the slice).</summary>
    float Delta(float share)
    {
        if (Timing.bestLap <= 0f) return 0f;
        float position = share * TraceBins;
        int bin = Mathf.Clamp((int)position, 0, TraceBins - 1);
        float from = _bestElapsedAtBin[bin], to = bin + 1 < TraceBins ? _bestElapsedAtBin[bin + 1] : Timing.bestLap;
        return _lapTime - Mathf.Lerp(from, to, position - bin);
    }
}
