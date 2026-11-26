/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTelemetryMapper - Backend telemetry → what the telemetry widgets show.
 ##   • FromReplay / FromLive: a TelemetryTimeline (followed car, compared
 ##     car, sector bests of every car) from a downloaded replay, or from the
 ##     rows of the live routes (/telemetry/speed, /telemetry/engine, /race/laps).
 ##   • Fill: the TelemetryFrame at one time of that timeline. Stateless: any
 ##     time can be asked, in any order (playback, rewind, seek), and only what
 ##     the car had done at that time is shown — never the rest of its lap.
 ## What is derived, the backend having no such field:
 ##   • position on the lap: metres driven since the line (odometer) over the
 ##     lap length;
 ##   • 24 mini-sectors: the lap cut in 24 equal distances, timed on the samples;
 ##     their purple compares the two followed cars only (the backend has no
 ##     mini-sector of the other cars);
 ##   • delta: time now minus the time the best lap took to the same distance;
 ##   • lap trace: speed and pedals at 240 points of the lap, the running lap
 ##     up to the car, the previous lap after it;
 ##   • location: the running sector ("Sector 2"), corners are not known.
 ## A sector or lap time shows once it is over at that time; purple / green /
 ## yellow judge it against the times set before it.
 ##
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ODTelemetryMapper
{
    /// <summary>Points of the lap trace (about 22 m each on a 5 km lap).</summary>
    public const int TraceBins = 240;
    /// <summary>Seconds without a sample around a time: the car has no data then (garage, retired, gap).</summary>
    const float MaxSampleGap = 3f;
    /// <summary>Two live samples further apart than this are from different laps: no distance between them.</summary>
    const float MaxIntegrationGap = 2f;
    const float FallbackLapLength = 5000f, MinLapLength = 1000f;
    const int ShownLastLaps = 5;
    const int MiniSectors = LapTiming.MiniSectors;
    static readonly float[] DefaultSectorEnds = { 1f / 3f, 2f / 3f };

    // ═══════════════════════════════════════════════════════════════════════
    // Building
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Timeline of a downloaded replay, on its clock (RaceReplay seconds): the car of
    /// <paramref name="driverCode"/> (else number <paramref name="driverFallback"/>) and the one of
    /// <paramref name="rivalCode"/>. Shares the replay's arrays, copies nothing big.
    /// </summary>
    public static TelemetryTimeline FromReplay(RaceReplay replay, string driverCode, int driverFallback,
        string rivalCode, int rivalFallback)
    {
        var timeline = new TelemetryTimeline { driverCode = driverCode, rivalCode = rivalCode };
        if (replay == null) return timeline;
        timeline.driver = FromReplayCar(replay, Number(replay.drivers?.Values, driverCode, driverFallback), driverCode);
        timeline.rival = FromReplayCar(replay, Number(replay.drivers?.Values, rivalCode, rivalFallback), rivalCode);

        var marks = new List<(int sector, float time, float value)>();
        foreach (var car in replay.cars.Values)
            for (int n = 1; n <= car.lapCount && n < car.lapStart.Length; n++)
            {
                float start = float.IsNaN(car.lapStart[n]) ? float.NegativeInfinity : car.lapStart[n];
                AddSectorMarks(marks, start, car.lapTime[n], car.sector1[n], car.sector2[n], float.NaN);
            }
        SetSectorBests(timeline, marks);
        return timeline;
    }

    /// <summary>Car number of <paramref name="code"/> in <paramref name="drivers"/>, else <paramref name="fallback"/>.</summary>
    public static int Number(IEnumerable<DriverSummaryDto> drivers, string code, int fallback)
    {
        var driver = drivers?.FirstOrDefault(d => d != null && string.Equals(d.code, code, StringComparison.OrdinalIgnoreCase));
        return driver != null ? driver.driverNumber : fallback;
    }

    static DriverTelemetry FromReplayCar(RaceReplay replay, int number, string code)
    {
        if (!replay.cars.TryGetValue(number, out var car) || car.odometerTime.Length == 0) return null;
        replay.drivers.TryGetValue(number, out var info);

        var sector3 = new float[car.lapTime.Length];
        for (int n = 0; n < sector3.Length; n++)
            sector3[n] = n < car.sector1.Length && n < car.sector2.Length
                ? car.lapTime[n] - car.sector1[n] - car.sector2[n] : float.NaN;

        var driver = new DriverTelemetry
        {
            number = number,
            code = string.IsNullOrEmpty(info?.code) ? code : info.code,
            teamColor = Colour(info?.teamColor),
            time = car.odometerTime,
            odometer = car.odometer,
            speed = car.speed,
            throttle = car.throttle,
            brake = car.brake,
            rpm = car.rpm,
            gear = car.gear,
            maxRpm = car.rpm.Length > 0 ? car.rpm.Max() : 0f,
            lapStart = car.lapStart,
            lapTime = car.lapTime,
            sector1 = car.sector1,
            sector2 = car.sector2,
            sector3 = sector3,
            lapCount = car.lapCount,
            lapLength = car.lapDistance,
        };
        driver.sectorEnds = SectorEnds(driver);
        return driver;
    }

    /// <summary>
    /// One followed car of a live session: its laps (<paramref name="laps"/>, every lap of the session)
    /// and the samples of the laps fetched so far (<paramref name="rows"/>, by lap number), on a clock in
    /// seconds from <paramref name="origin"/>. Null without any sample.
    /// </summary>
    public static DriverTelemetry FromLive(int number, string code, DriverSummaryDto info, RaceLapsDto laps,
        IReadOnlyDictionary<int, (List<TelemetrySpeedDto> speed, List<TelemetryEngineDto> engine)> rows, DateTime origin)
    {
        int lastLap = Math.Max(laps?.laps?.Where(l => l != null).Select(l => l.lapNumber).DefaultIfEmpty(0).Max() ?? 0,
            rows.Keys.DefaultIfEmpty(0).Max());
        var lapTime = Filled(lastLap + 2);
        var sector1 = Filled(lastLap + 2);
        var sector2 = Filled(lastLap + 2);
        var sector3 = Filled(lastLap + 2);
        var lapStart = Filled(lastLap + 2);
        int lapCount = 0;
        foreach (var lap in laps?.laps ?? new List<RaceLapEntryDto>())
        {
            if (lap == null || lap.lapNumber < 1 || lap.lapNumber > lastLap) continue;
            int n = lap.lapNumber;
            lapTime[n] = lap.lapDuration ?? float.NaN;
            sector1[n] = lap.sector1 ?? float.NaN;
            sector2[n] = lap.sector2 ?? float.NaN;
            sector3[n] = lap.sector3 ?? float.NaN;
            if (lap.lapDuration.HasValue) lapCount = Math.Max(lapCount, n);
        }

        var time = new List<float>();
        var speed = new List<float>();
        var throttle = new List<float>();
        var brake = new List<float>();
        var rpm = new List<float>();
        var gear = new List<int>();
        foreach (int n in rows.Keys.OrderBy(k => k))
        {
            var (speedRows, engineRows) = rows[n];
            var speeds = (speedRows ?? new List<TelemetrySpeedDto>()).Where(r => r != null).OrderBy(r => r.timestamp).ToList();
            var engines = (engineRows ?? new List<TelemetryEngineDto>()).Where(r => r != null).OrderBy(r => r.timestamp).ToList();
            int e = 0;
            bool first = true;
            foreach (var row in speeds)
            {
                float t = (float)(row.timestamp - origin).TotalSeconds;
                if (time.Count > 0 && t <= time[time.Count - 1]) continue;   // lap windows share their edge sample
                // Engine row of the same instant (the backend gives both from one sample).
                while (e + 1 < engines.Count && Math.Abs((engines[e + 1].timestamp - row.timestamp).TotalSeconds) <=
                       Math.Abs((engines[e].timestamp - row.timestamp).TotalSeconds)) e++;
                var engine = e < engines.Count ? engines[e] : null;
                if (first && n < lapStart.Length) lapStart[n] = t;
                first = false;
                time.Add(t);
                speed.Add(row.speed);
                gear.Add(row.gear);
                throttle.Add(engine?.throttlePercent ?? 0f);
                brake.Add(engine?.brakePercent ?? 0f);
                rpm.Add(engine?.rpm ?? 0f);
            }
        }
        if (time.Count == 0) return null;

        // A completed lap ends where the next one starts.
        for (int n = 1; n + 1 < lapStart.Length; n++)
            if (float.IsNaN(lapStart[n + 1]) && !float.IsNaN(lapStart[n]) && !float.IsNaN(lapTime[n]))
                lapStart[n + 1] = lapStart[n] + lapTime[n];

        var odometer = new float[time.Count];
        for (int i = 1; i < time.Count; i++)
        {
            float dt = time[i] - time[i - 1];
            odometer[i] = odometer[i - 1] + (dt <= MaxIntegrationGap ? (speed[i] + speed[i - 1]) * .5f / 3.6f * dt : 0f);
        }

        var driver = new DriverTelemetry
        {
            number = number,
            code = string.IsNullOrEmpty(info?.code) ? code : info.code,
            teamColor = Colour(info?.teamColor),
            time = time.ToArray(),
            speed = speed.ToArray(),
            throttle = throttle.ToArray(),
            brake = brake.ToArray(),
            rpm = rpm.ToArray(),
            gear = gear.ToArray(),
            odometer = odometer,
            maxRpm = rpm.Count > 0 ? rpm.Max() : 0f,
            lapStart = lapStart,
            lapTime = lapTime,
            sector1 = sector1,
            sector2 = sector2,
            sector3 = sector3,
            lapCount = lapCount,
        };

        // Typical lap: the median of the complete laps that have samples from line to line.
        var lengths = new List<float>();
        float last = driver.time[driver.time.Length - 1];
        for (int n = 2; n <= lapCount; n++)
        {
            float s = LapStart(driver, n), end = LapStart(driver, n + 1);
            if (float.IsNaN(s) || float.IsNaN(end) || end > last || !Covered(driver, s, end)) continue;
            float length = Odometer(driver, end) - Odometer(driver, s);
            if (length > MinLapLength) lengths.Add(length);
        }
        lengths.Sort();
        if (lengths.Count > 0) driver.lapLength = lengths[lengths.Count / 2];
        driver.sectorEnds = SectorEnds(driver);
        return driver;
    }

    /// <summary>Sector bests of a live session: every completed lap of every car, all already set.</summary>
    public static void SetLiveSectorBests(TelemetryTimeline timeline, IEnumerable<RaceLapsDto> laps)
    {
        var marks = new List<(int sector, float time, float value)>();
        foreach (var driver in laps ?? Enumerable.Empty<RaceLapsDto>())
            foreach (var lap in driver?.laps ?? new List<RaceLapEntryDto>())
                if (lap?.lapDuration != null)
                    AddSectorMarks(marks, float.NegativeInfinity, lap.lapDuration.Value,
                        lap.sector1 ?? float.NaN, lap.sector2 ?? float.NaN, lap.sector3 ?? float.NaN);
        SetSectorBests(timeline, marks);
    }

    static void AddSectorMarks(List<(int sector, float time, float value)> marks, float start, float lap,
        float s1, float s2, float s3)
    {
        if (float.IsNaN(s3) && !float.IsNaN(lap) && !float.IsNaN(s1) && !float.IsNaN(s2)) s3 = lap - s1 - s2;
        float t = start;
        float[] sectors = { s1, s2, s3 };
        for (int s = 0; s < sectors.Length; s++)
        {
            if (!(sectors[s] > 0f)) return;   // an unknown sector: the next ones have no time either
            t += sectors[s];
            marks.Add((s, t, sectors[s]));
        }
    }

    static void SetSectorBests(TelemetryTimeline timeline, List<(int sector, float time, float value)> marks)
    {
        for (int s = 0; s < LapTiming.Sectors; s++)
        {
            var sorted = marks.Where(m => m.sector == s).OrderBy(m => m.time).ToList();
            var time = new float[sorted.Count];
            var best = new float[sorted.Count];
            float running = float.PositiveInfinity;
            for (int i = 0; i < sorted.Count; i++)
            {
                running = Mathf.Min(running, sorted[i].value);
                time[i] = sorted[i].time;
                best[i] = running;
            }
            timeline.sectorBestTime[s] = time;
            timeline.sectorBest[s] = best;
        }
    }

    /// <summary>Where S1 and S2 end on the lap, from the fastest lap with samples from line to line.</summary>
    static float[] SectorEnds(DriverTelemetry d)
    {
        int best = -1;
        float last = d.time.Length > 0 ? d.time[d.time.Length - 1] : float.NegativeInfinity;
        for (int n = 2; n <= d.lapCount; n++)
        {
            float s = LapStart(d, n), e = LapStart(d, n + 1);
            if (float.IsNaN(s) || float.IsNaN(e) || e > last || !(Value(d.sector1, n) > 0f) || !(Value(d.sector2, n) > 0f)) continue;
            if (!Covered(d, s, e)) continue;
            if (best < 0 || Value(d.lapTime, n) < Value(d.lapTime, best)) best = n;
        }
        if (best < 0) return null;

        float start = LapStart(d, best), span = Odometer(d, LapStart(d, best + 1)) - Odometer(d, start);
        if (span < MinLapLength) return null;
        float s1 = start + d.sector1[best], s2 = s1 + d.sector2[best];
        return new[]
        {
            Mathf.Clamp01((Odometer(d, s1) - Odometer(d, start)) / span),
            Mathf.Clamp01((Odometer(d, s2) - Odometer(d, start)) / span),
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Reading: the frame at a time
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Writes into <paramref name="frame"/> what the widgets show at <paramref name="time"/>. False when
    /// the followed car has no sample around that time: then nothing of it is shown.
    /// </summary>
    public static bool Fill(TelemetryTimeline timeline, float time, TelemetryFrame frame)
    {
        if (timeline == null || frame == null) return false;
        PrepareTrace(frame.trace);

        var d = timeline.driver;
        bool has = HasSample(d, time);
        FillTiming(timeline, d, timeline.rival, time, has, timeline.driverCode, frame.timing);
        var rival = timeline.rival;
        FillTiming(timeline, rival, d, time, HasSample(rival, time), timeline.rivalCode, frame.rival);
        if (!has) return false;

        frame.car = Car(d, time, frame.timing);
        FillTrace(d, time, frame.timing, frame.trace);
        return true;
    }

    static void PrepareTrace(LapTrace trace)
    {
        if (trace.speed.Length == TraceBins) return;
        trace.speed = new float[TraceBins];
        trace.throttle = new float[TraceBins];
        trace.brake = new float[TraceBins];
    }

    static bool HasSample(DriverTelemetry d, float time)
    {
        if (d == null || d.time.Length == 0) return false;
        int i = Floor(d.time, time);
        float before = i >= 0 ? time - d.time[i] : float.PositiveInfinity;
        float after = i + 1 < d.time.Length ? d.time[i + 1] - time : float.PositiveInfinity;
        return Mathf.Min(before, after) <= MaxSampleGap;
    }

    static CarTelemetry Car(DriverTelemetry d, float time, LapTiming timing)
    {
        Sample(d, time, out int i, out float k);
        int running = Array.IndexOf(timing.sectorStatus, SplitStatus.Running);
        return new CarTelemetry
        {
            speedKmh = Mathf.Max(0f, Mathf.Lerp(d.speed[i], d.speed[Next(d, i)], k)),
            gear = d.gear[i],
            rpm = Mathf.Lerp(d.rpm[i], d.rpm[Next(d, i)], k),
            maxRpm = d.maxRpm > 0f ? d.maxRpm : 12500f,
            throttle = Mathf.Clamp01(Mathf.Lerp(d.throttle[i], d.throttle[Next(d, i)], k) / 100f),
            brake = Mathf.Clamp01(Mathf.Lerp(d.brake[i], d.brake[Next(d, i)], k) / 100f),
            lapDistance = timing.lapShare,
            location = running >= 0 && timing.lapTime > 0f ? "Sector " + (running + 1) : "",
        };
    }

    // ── Timing ──────────────────────────────────────────────────────────────

    static void FillTiming(TelemetryTimeline timeline, DriverTelemetry d, DriverTelemetry other, float time,
        bool has, string code, LapTiming timing)
    {
        timing.driverCode = d?.code ?? code;
        timing.teamColor = d != null ? d.teamColor : Color.white;
        timing.lastLaps.Clear();
        for (int m = 0; m < MiniSectors; m++) timing.miniSectors[m] = SplitStatus.Upcoming;
        if (d == null || !has)
        {
            timing.lap = 0;
            timing.lapTime = timing.lapShare = timing.delta = timing.bestLap = 0f;
            for (int s = 0; s < LapTiming.Sectors; s++)
            {
                timing.sectorTimes[s] = timing.lastSectorTimes[s] = -1f;
                timing.sectorStatus[s] = SplitStatus.Upcoming;
            }
            return;
        }

        int lap = LapAt(d, time);
        float start = LapStart(d, lap);
        bool started = !float.IsNaN(start) && time >= start;
        float span = LapSpan(d, lap, time);
        float driven = started ? Mathf.Max(0f, Odometer(d, time) - Odometer(d, start)) : 0f;
        timing.lap = lap;
        timing.lapTime = started ? time - start : 0f;
        timing.lapShare = Mathf.Clamp01(driven / span);

        // Sectors of the running lap: a time once the sector is over, judged against the ones set before.
        bool open = false;
        float elapsed = 0f;
        for (int s = 0; s < LapTiming.Sectors; s++)
        {
            float value = Sector(d, lap, s);
            bool done = !open && started && value > 0f && time >= start + elapsed + value;
            float previous = Sector(d, lap - 1, s);
            timing.lastSectorTimes[s] = previous > 0f ? previous : -1f;
            if (done)
            {
                elapsed += value;
                timing.sectorTimes[s] = value;
                timing.sectorStatus[s] = Judge(value, PersonalBestSector(d, lap, s), SessionBestSector(timeline, s, start + elapsed));
                continue;
            }
            timing.sectorTimes[s] = -1f;
            timing.sectorStatus[s] = open ? SplitStatus.Upcoming : SplitStatus.Running;
            open = true;
        }

        // Laps completed before this one: best and last ones.
        int best = -1;
        for (int n = 1; n < lap && n < d.lapTime.Length; n++)
        {
            float t = d.lapTime[n];
            if (!(t > 0f)) continue;
            if (best < 0 || t < d.lapTime[best]) best = n;
            timing.lastLaps.Add(new LapRecord { lap = n, time = t });
        }
        if (timing.lastLaps.Count > ShownLastLaps) timing.lastLaps.RemoveRange(0, timing.lastLaps.Count - ShownLastLaps);
        timing.bestLap = best > 0 ? d.lapTime[best] : 0f;

        float bestElapsed = best > 0 && started ? TimeToDistance(d, LapStart(d, best), LapStart(d, best + 1), driven) : float.NaN;
        timing.delta = float.IsNaN(bestElapsed) ? 0f : timing.lapTime - bestElapsed;

        if (started) FillMiniSectors(d, other, lap, start, span, driven, time, timing.miniSectors);
    }

    /// <summary>Purple when no car did better before, green when the driver did not, else yellow.</summary>
    static SplitStatus Judge(float value, float personalBest, float sessionBest)
    {
        if (value <= sessionBest + 1e-4f) return SplitStatus.SessionBest;
        return value < personalBest ? SplitStatus.PersonalBest : SplitStatus.Slower;
    }

    static float Sector(DriverTelemetry d, int lap, int s)
    {
        var sectors = s == 0 ? d.sector1 : s == 1 ? d.sector2 : d.sector3;
        return Value(sectors, lap);
    }

    static float PersonalBestSector(DriverTelemetry d, int lap, int s)
    {
        float best = float.PositiveInfinity;
        for (int n = 1; n < lap; n++)
        {
            float value = Sector(d, n, s);
            if (value > 0f) best = Mathf.Min(best, value);
        }
        return best;
    }

    /// <summary>Best sector <paramref name="s"/> of any car set before <paramref name="time"/> (the sector being judged excluded).</summary>
    static float SessionBestSector(TelemetryTimeline timeline, int s, float time)
    {
        int i = Floor(timeline.sectorBestTime[s], time - .01f);
        return i >= 0 ? timeline.sectorBest[s][i] : float.PositiveInfinity;
    }

    // ── Mini-sectors ────────────────────────────────────────────────────────

    static void FillMiniSectors(DriverTelemetry d, DriverTelemetry other, int lap, float start, float span,
        float driven, float time, SplitStatus[] statuses)
    {
        float previousEnd = 0f;
        for (int m = 0; m < MiniSectors; m++)
        {
            float distance = span * (m + 1) / MiniSectors;
            if (driven < distance) return;
            float end = TimeToDistance(d, start, time, distance);
            if (float.IsNaN(end)) return;
            float value = end - previousEnd;
            previousEnd = end;
            statuses[m] = Judge(value, PersonalBestMini(d, lap, m), SessionBestMini(d, other, m, start + end));
        }
    }

    static float PersonalBestMini(DriverTelemetry d, int lap, int m)
    {
        float best = float.PositiveInfinity;
        for (int n = 1; n < lap; n++)
        {
            var minis = Minis(d, n, out _);
            if (minis.Length > 0) best = Mathf.Min(best, minis[m]);
        }
        return best;
    }

    /// <summary>Best mini-sector <paramref name="m"/> of the two followed cars ended before <paramref name="time"/>.</summary>
    static float SessionBestMini(DriverTelemetry d, DriverTelemetry other, int m, float time)
    {
        float best = float.PositiveInfinity;
        foreach (var car in new[] { d, other })
        {
            if (car == null) continue;
            for (int n = 1; n <= car.lapCount; n++)
            {
                var minis = Minis(car, n, out var ends);
                if (minis.Length > 0 && ends[m] < time - .01f) best = Mathf.Min(best, minis[m]);
            }
        }
        return best;
    }

    /// <summary>Mini-sector times and end times of completed lap <paramref name="lap"/>, computed once (empty without samples).</summary>
    static float[] Minis(DriverTelemetry d, int lap, out float[] ends)
    {
        int count = d.lapStart.Length;
        if (d.miniTimes == null || d.miniTimes.Length != count)
        {
            d.miniTimes = new float[count][];
            d.miniEnds = new float[count][];
        }
        ends = Array.Empty<float>();
        if (lap < 1 || lap >= count) return Array.Empty<float>();
        if (d.miniTimes[lap] != null)
        {
            ends = d.miniEnds[lap];
            return d.miniTimes[lap];
        }

        float start = LapStart(d, lap), end = LapStart(d, lap + 1);
        var times = Array.Empty<float>();
        var at = Array.Empty<float>();
        float last = d.time.Length > 0 ? d.time[d.time.Length - 1] : float.NegativeInfinity;
        if (!float.IsNaN(start) && !float.IsNaN(end) && end <= last && Covered(d, start, end))
        {
            float span = Odometer(d, end) - Odometer(d, start);
            if (span > MinLapLength)
            {
                times = new float[MiniSectors];
                at = new float[MiniSectors];
                float previous = 0f;
                for (int m = 0; m < MiniSectors; m++)
                {
                    float elapsed = m == MiniSectors - 1 ? end - start : TimeToDistance(d, start, end, span * (m + 1) / MiniSectors);
                    if (float.IsNaN(elapsed)) { times = at = Array.Empty<float>(); break; }
                    times[m] = elapsed - previous;
                    at[m] = start + elapsed;
                    previous = elapsed;
                }
            }
        }
        d.miniTimes[lap] = times;
        d.miniEnds[lap] = at;
        ends = at;
        return times;
    }

    // ── Lap trace ───────────────────────────────────────────────────────────

    /// <summary>Speed and pedals along the lap: the running lap up to the car, the previous lap after it.</summary>
    static void FillTrace(DriverTelemetry d, float time, LapTiming timing, LapTrace trace)
    {
        int lap = timing.lap;
        float start = LapStart(d, lap), span = LapSpan(d, lap, time);
        float previousStart = LapStart(d, lap - 1), previousSpan = LapSpan(d, lap - 1, time);
        bool started = !float.IsNaN(start) && time >= start;
        float carAt = timing.lapShare;

        trace.lapLengthKm = (d.lapLength > MinLapLength ? d.lapLength : span) / 1000f;
        trace.sectorEnds = d.sectorEnds ?? DefaultSectorEnds;

        float speed = 0f, throttle = 0f, brake = 0f;
        for (int b = 0; b < TraceBins; b++)
        {
            float share = (b + .5f) / TraceBins;
            float at = float.NaN;
            if (started && share <= carAt) at = Offset(start, TimeToDistance(d, start, time, share * span));
            else if (!float.IsNaN(previousStart) && !float.IsNaN(start))
                at = Offset(previousStart, TimeToDistance(d, previousStart, start, share * previousSpan));
            if (!float.IsNaN(at))
            {
                Sample(d, at, out int i, out float k);
                speed = Mathf.Lerp(d.speed[i], d.speed[Next(d, i)], k);
                throttle = Mathf.Clamp01(Mathf.Lerp(d.throttle[i], d.throttle[Next(d, i)], k) / 100f);
                brake = Mathf.Clamp01(Mathf.Lerp(d.brake[i], d.brake[Next(d, i)], k) / 100f);
            }
            // A point without data keeps the one before it (no drop to zero on the chart).
            trace.speed[b] = speed;
            trace.throttle[b] = throttle;
            trace.brake[b] = brake;
        }
    }

    static float Offset(float start, float elapsed) => float.IsNaN(elapsed) ? float.NaN : start + elapsed;

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers on one car
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>The lap the car is on at <paramref name="time"/> (1 before its first known line).</summary>
    static int LapAt(DriverTelemetry d, float time)
    {
        for (int n = d.lapStart.Length - 1; n > 1; n--)
            if (!float.IsNaN(d.lapStart[n]) && d.lapStart[n] <= time) return n;
        return 1;
    }

    static float LapStart(DriverTelemetry d, int lap) => lap >= 1 && lap < d.lapStart.Length ? d.lapStart[lap] : float.NaN;

    /// <summary>Metres of lap <paramref name="lap"/>: the real ones once it is over, the typical lap before.</summary>
    static float LapSpan(DriverTelemetry d, int lap, float time)
    {
        float start = LapStart(d, lap), end = LapStart(d, lap + 1);
        if (!float.IsNaN(start) && !float.IsNaN(end) && end <= time && Covered(d, start, end))
        {
            float span = Odometer(d, end) - Odometer(d, start);
            if (span > MinLapLength) return span;
        }
        return d.lapLength > MinLapLength ? d.lapLength : FallbackLapLength;
    }

    /// <summary>Samples from <paramref name="start"/> to <paramref name="end"/>, without a hole longer than MaxSampleGap at either end.</summary>
    static bool Covered(DriverTelemetry d, float start, float end) =>
        HasSample(d, start) && HasSample(d, end) && Floor(d.time, end) > Floor(d.time, start);

    /// <summary>Metres driven at <paramref name="time"/>.</summary>
    static float Odometer(DriverTelemetry d, float time)
    {
        if (d.time.Length == 0) return 0f;
        Sample(d, time, out int i, out float k);
        return Mathf.Lerp(d.odometer[i], d.odometer[Next(d, i)], k);
    }

    /// <summary>
    /// Seconds after <paramref name="from"/> when the car had driven <paramref name="metres"/> since then,
    /// looking no later than <paramref name="to"/>; NaN when it had not.
    /// </summary>
    static float TimeToDistance(DriverTelemetry d, float from, float to, float metres)
    {
        if (float.IsNaN(from) || float.IsNaN(to) || d.time.Length < 2) return float.NaN;
        float target = Odometer(d, from) + metres;
        if (Odometer(d, to) < target - .01f) return float.NaN;

        int lo = Mathf.Max(0, Floor(d.time, from)), hi = Mathf.Min(d.time.Length - 1, Floor(d.time, to) + 1);
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (d.odometer[mid] < target) lo = mid + 1;
            else hi = mid;
        }
        if (lo == 0) return Mathf.Max(0f, d.time[0] - from);
        float a = d.odometer[lo - 1], b = d.odometer[lo];
        float k = b > a ? Mathf.Clamp01((target - a) / (b - a)) : 0f;
        return Mathf.Clamp(Mathf.Lerp(d.time[lo - 1], d.time[lo], k) - from, 0f, to - from);
    }

    /// <summary>Sample index at or before <paramref name="time"/> (clamped) and the share towards the next one.</summary>
    static void Sample(DriverTelemetry d, float time, out int index, out float k)
    {
        int i = Mathf.Clamp(Floor(d.time, time), 0, d.time.Length - 1);
        int j = Next(d, i);
        float span = d.time[j] - d.time[i];
        index = i;
        k = span > 0f ? Mathf.Clamp01((time - d.time[i]) / span) : 0f;
    }

    static int Next(DriverTelemetry d, int i) => Mathf.Min(i + 1, d.time.Length - 1);

    /// <summary>Last index whose value is at or before <paramref name="value"/> (-1 when none).</summary>
    static int Floor(float[] sorted, float value)
    {
        int lo = 0, hi = sorted.Length - 1, found = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (sorted[mid] <= value) { found = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return found;
    }

    static float Value(float[] values, int index) => index >= 0 && index < values.Length ? values[index] : float.NaN;

    static float[] Filled(int length)
    {
        var values = new float[Math.Max(0, length)];
        for (int i = 0; i < values.Length; i++) values[i] = float.NaN;
        return values;
    }

    static Color Colour(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.white;
        return ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var colour) ? colour : Color.white;
    }
}
