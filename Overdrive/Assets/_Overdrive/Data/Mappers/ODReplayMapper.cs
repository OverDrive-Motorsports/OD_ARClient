/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODReplayMapper - A RaceReplay at a given time: the running order as the
 ## live panel shows it (LiveSessionData) and where each car is on the track.
 ## Pure functions, no state: the clock is ODReplayPlayer's.
 ##
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public static class ODReplayMapper
{
    /// <summary>Samples further apart than this are not joined: the car is shown at the last one, then hidden.</summary>
    const float MaxSampleGap = 3f;
    const float HoldLastSample = 2f;
    /// <summary>A car whose laps stop this long before the chequered flag has retired.</summary>
    const float RetiredAfter = 120f;
    /// <summary>Distance between two grid slots, as a share of the lap (about 15 m on a 5 km track).</summary>

    /// <summary>The running order at <paramref name="time"/>, P1 first.</summary>
    public static LiveSessionData ToFrame(RaceReplay replay, ReplaySummary summary, float time)
    {
        var order = replay.cars.Values
            .OrderBy(c => IsRetired(replay, c, time))
            .ThenBy(c => PositionAt(c, time))
            .ThenByDescending(c => LapAt(c, time))
            .ToList();
        var leader = order.FirstOrDefault();
        float? fastest = FastestLap(replay, time);

        var data = new LiveSessionData
        {
            championshipId = summary.championshipId,
            sessionName = summary.sessionName,
            circuitName = summary.circuit,
            currentLap = leader != null ? Mathf.Min(LapAt(leader, time), replay.totalLaps) : 0,
            totalLaps = replay.totalLaps,
        };

        for (int i = 0; i < order.Count; i++)
        {
            var car = order[i];
            replay.drivers.TryGetValue(car.number, out var driver);
            int lap = LapAt(car, time);
            int completed = Mathf.Min(lap - 1, car.lapCount);
            var stint = car.stints.LastOrDefault(s => s.lapStart <= lap);
            float lastLap = completed >= 1 ? car.lapTime[completed] : float.NaN;

            data.entries.Add(new LiveEntry
            {
                position = i + 1,
                name = ODChampionshipMapper.DriverName(driver, null, car.number),
                code = driver?.code,
                teamName = driver?.teamName ?? "",
                teamColor = ODChampionshipMapper.ParseColor(driver?.teamColor),
                gap = i == 0 ? "Leader" : IsRetired(replay, car, time) ? "Abandon" : Gap(car, leader, time),
                tyreCompound = ODChampionshipMapper.CompoundLetter(stint.compound),
                tyreAgeLaps = stint.compound != null ? stint.tyreAgeAtStart + Mathf.Max(0, lap - stint.lapStart) : -1,
                pitStops = car.pitLaps.Count(p => p < lap),
                lastLap = float.IsNaN(lastLap) ? "" : ODChampionshipMapper.FormatLapTime(lastLap),
                isFastestLap = fastest.HasValue && !float.IsNaN(lastLap) && Mathf.Approximately(lastLap, fastest.Value),
            });
        }
        return data;
    }

    /// <summary>The lap the car is on at <paramref name="time"/> (1 until its first line crossing).</summary>
    public static int LapAt(ReplayCar car, float time)
    {
        for (int n = car.lapStart.Length - 1; n > 1; n--)
            if (!float.IsNaN(car.lapStart[n]) && car.lapStart[n] <= time) return n;
        return 1;
    }

    /// <summary>The last official position given before <paramref name="time"/> (the grid before the start).</summary>
    public static int PositionAt(ReplayCar car, float time)
    {
        int i = Floor(car.positionTime, time);
        return i >= 0 ? car.position[i] : car.position.Length > 0 ? car.position[0] : int.MaxValue;
    }

    public static bool IsRetired(RaceReplay replay, ReplayCar car, float time)
    {
        float lastLine = car.lapStart.Length > 0 ? car.lapStart[car.lapStart.Length - 1] : float.NaN;
        return !float.IsNaN(lastLine) && lastLine < replay.finish - RetiredAfter && time > lastLine + RetiredAfter;
    }

    /// <summary>
    /// Gap to the leader, measured where the car is now: how long ago the leader was at the
    /// same point of the race (see <see cref="Progress"/>), so it changes all along the lap,
    /// exact at each sector line. "+n tours" when the leader is n laps or more ahead. Empty
    /// until the car's first timing line; frozen once the car has run all its laps.
    /// </summary>
    public static string Gap(ReplayCar car, ReplayCar leader, float time)
    {
        if (leader == null || time < FirstTimingLine(car)) return "";
        float progress = Progress(car, time);

        int lapsDown = Mathf.FloorToInt(Progress(leader, time) - progress + 1e-4f);
        if (lapsDown > 0) return lapsDown == 1 ? "+1 tour" : $"+{lapsDown} tours";

        float gap = Mathf.Min(time, LastLine(car)) - TimeAt(leader, progress);
        return float.IsNaN(gap) || gap < 0f ? "" : "+" + gap.ToString("0.0", CultureInfo.InvariantCulture) + "s";
    }

    /// <summary>
    /// How much of the race the car has run at <paramref name="time"/>, in laps from lights
    /// out (2.5 = halfway through lap 3). Each lap is cut in thirds at the sector lines and
    /// time runs evenly inside a sector (inside the lap when its sectors are unknown). A car
    /// that has run all its laps stays on its last line.
    /// </summary>
    public static float Progress(ReplayCar car, float time)
    {
        int last = LastLap(car);
        if (last < 1 || float.IsNaN(car.lapStart[1]) || time <= car.lapStart[1]) return 0f;
        if (time >= car.lapStart[last]) return last - 1;
        int lap = LapAt(car, time);
        Splits(car, lap, out float a, out float b, out float d);
        if (!(d > 0f)) return lap - 1;
        float t = time - car.lapStart[lap];
        float within = float.IsNaN(a) ? t / d
                     : (t < a ? t / a : t < b ? 1f + (t - a) / (b - a) : 2f + (t - b) / (d - b)) / 3f;
        return lap - 1 + Mathf.Clamp01(within);
    }

    /// <summary>When the car was at <paramref name="progress"/> (see <see cref="Progress"/>); NaN past its last line.</summary>
    public static float TimeAt(ReplayCar car, float progress)
    {
        int last = LastLap(car);
        if (last < 1 || progress < 0f) return float.NaN;
        if (progress >= last - 1) return progress <= last - 1 + 1e-4f ? car.lapStart[last] : float.NaN;
        int lap = Mathf.FloorToInt(progress) + 1;
        float f = progress - (lap - 1);
        Splits(car, lap, out float a, out float b, out float d);
        float t = float.IsNaN(a) ? f * d
                : f < 1f / 3f ? f * 3f * a
                : f < 2f / 3f ? a + (f * 3f - 1f) * (b - a)
                : b + (f * 3f - 2f) * (d - b);
        return car.lapStart[lap] + t;
    }

    /// <summary>Lap <paramref name="lap"/>'s sector lines a, b and its duration d, in seconds into the lap (a, b NaN when unusable).</summary>
    static void Splits(ReplayCar car, int lap, out float a, out float b, out float d)
    {
        d = car.lapStart[lap + 1] - car.lapStart[lap];
        a = lap < car.sector1.Length ? car.sector1[lap] : float.NaN;
        b = lap < car.sector2.Length ? a + car.sector2[lap] : float.NaN;
        if (!(a > 0f && b > a && d > b)) a = b = float.NaN;
    }

    /// <summary>The car's last known line: lapStart[LastLap] is when it finished its laps.</summary>
    static int LastLap(ReplayCar car)
    {
        for (int n = car.lapStart.Length - 1; n >= 1; n--)
            if (!float.IsNaN(car.lapStart[n])) return n;
        return 0;
    }

    static float LastLine(ReplayCar car)
    {
        int last = LastLap(car);
        return last >= 1 ? car.lapStart[last] : float.NaN;
    }

    /// <summary>The first timing line the car crosses: the end of sector 1 of lap 1, or the line.</summary>
    static float FirstTimingLine(ReplayCar car)
    {
        if (LastLap(car) < 2) return float.PositiveInfinity;
        Splits(car, 1, out float a, out _, out _);
        return car.lapStart[1] + (float.IsNaN(a) ? car.lapStart[2] - car.lapStart[1] : a);
    }

    /// <summary>Fastest lap completed by anyone before <paramref name="time"/>.</summary>
    static float? FastestLap(RaceReplay replay, float time)
    {
        float best = float.PositiveInfinity;
        foreach (var car in replay.cars.Values)
            for (int n = 1; n <= car.lapCount && n + 1 < car.lapStart.Length && car.lapStart[n + 1] <= time; n++)
                if (car.lapTime[n] < best) best = car.lapTime[n];
        return float.IsPositiveInfinity(best) ? (float?)null : best;
    }

    /// <summary>
    /// Where the car is on the map at <paramref name="time"/>: its own samples, or, when the
    /// backend gave every car the same x/y, its place in the lap along the outline.
    /// </summary>
    public static bool TryGetPosition(RaceReplay replay, ReplayCar car, float time, ref int hint, out Vector2 position)
    {
        if (!replay.sharedPositions || replay.outlineDistance.Length < 2)
            return TryGetPosition(car, time, ref hint, out position);
        position = PointOnOutline(replay, replay.lineOffset + LapFraction(car, time));
        return true;
    }

    /// <summary>
    /// How far into its lap the car is: 0 on the timing line, 1 back on it. From the metres it
    /// has driven (its own speed), so it is slow in the corners and fast on the straights; lap 1
    /// counts back from the first line crossing, so the car starts from its real grid slot
    /// (which can be past the line, as in Shanghai). Without speed data, time runs evenly over
    /// the lap. A car that has run all its laps waits on the line.
    /// </summary>
    public static float LapFraction(ReplayCar car, float time)
    {
        int lap = LapAt(car, time);
        if (lap > car.lapCount || lap + 1 >= car.lapStart.Length) return 0f;
        float start = car.lapStart[lap], end = car.lapStart[lap + 1];
        if (float.IsNaN(start) || float.IsNaN(end) || end <= start) return 0f;

        if (car.odometer.Length > 1 && car.lapDistance > 0f)
        {
            float driven = Odometer(car, time), lineAhead = Odometer(car, end);
            if (lap == 1) return Mathf.Clamp(1f - (lineAhead - driven) / car.lapDistance, -.5f, 1f);
            float lineBehind = Odometer(car, start);
            if (lineAhead - lineBehind > 1f) return Mathf.Clamp01((driven - lineBehind) / (lineAhead - lineBehind));
        }
        return Mathf.Clamp01((time - start) / (end - start));
    }

    /// <summary>Metres the car had driven at <paramref name="time"/> (see ReplayCar.odometer); NaN without speed data.</summary>
    public static float Odometer(ReplayCar car, float time)
    {
        var t = car.odometerTime;
        if (t.Length == 0 || float.IsNaN(time)) return float.NaN;
        if (time <= t[0]) return car.odometer[0];
        if (time >= t[t.Length - 1]) return car.odometer[t.Length - 1];
        int i = Floor(t, time);
        float span = t[i + 1] - t[i];
        float k = span > 0f ? (time - t[i]) / span : 0f;
        return Mathf.Lerp(car.odometer[i], car.odometer[i + 1], k);
    }

    /// <summary>The outline point <paramref name="fraction"/> of a lap after outline[0] (wraps around).</summary>
    public static Vector2 PointOnOutline(RaceReplay replay, float fraction)
    {
        var outline = replay.outline;
        var distance = replay.outlineDistance;
        float lap = distance[distance.Length - 1];
        float d = (fraction - Mathf.Floor(fraction)) * lap;
        int i = Mathf.Clamp(Floor(distance, d), 0, outline.Length - 1);
        float segment = distance[i + 1] - distance[i];
        float k = segment > 0f ? (d - distance[i]) / segment : 0f;
        return Vector2.Lerp(outline[i], outline[(i + 1) % outline.Length], k);
    }

    /// <summary>
    /// Where the car is at <paramref name="time"/>, between its two nearest samples. False when
    /// it has no sample close enough (garage, data gap, retired). <paramref name="hint"/> is the
    /// sample index found last time: playback moves forward, so the search is usually one step.
    /// </summary>
    public static bool TryGetPosition(ReplayCar car, float time, ref int hint, out Vector2 position)
    {
        position = default;
        var t = car.time;
        if (t.Length == 0 || time < t[0]) return false;

        if (hint < 0 || hint >= t.Length || t[hint] > time) hint = Floor(t, time);
        while (hint + 1 < t.Length && t[hint + 1] <= time) hint++;

        int i = hint;
        if (i + 1 < t.Length && t[i + 1] - t[i] <= MaxSampleGap)
        {
            float k = (time - t[i]) / (t[i + 1] - t[i]);
            position = new Vector2(Mathf.Lerp(car.x[i], car.x[i + 1], k), Mathf.Lerp(car.y[i], car.y[i + 1], k));
            return true;
        }
        if (time - t[i] > HoldLastSample) return false;
        position = new Vector2(car.x[i], car.y[i]);
        return true;
    }

    /// <summary>Index of the last value &lt;= <paramref name="value"/>, -1 when there is none.</summary>
    static int Floor(float[] sorted, float value)
    {
        int i = Array.BinarySearch(sorted, value);
        if (i >= 0)
        {
            while (i + 1 < sorted.Length && sorted[i + 1] == value) i++;
            return i;
        }
        return ~i - 1;
    }

    /// <summary>"Tour 12 / 56 · 0:21:45" — the race time runs from lights out.</summary>
    public static string Progress(LiveSessionData frame, float elapsed)
    {
        var t = TimeSpan.FromSeconds(Mathf.Max(0f, elapsed));
        string clock = $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
        if (frame == null || frame.currentLap <= 0) return clock;
        string lap = frame.totalLaps > 0 ? $"Tour {frame.currentLap} / {frame.totalLaps}" : $"Tour {frame.currentLap}";
        return lap + " · " + clock;
    }
}
